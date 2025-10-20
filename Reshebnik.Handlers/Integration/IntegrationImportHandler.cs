using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Extensions;
using Reshebnik.Domain.Models.Integration;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using System.Text.Json;

namespace Reshebnik.Handlers.Integration;

public class IntegrationImportHandler(ReshebnikContext db, CompanyContextHandler companyContext)
{
    public async Task<IntegrationImportResponse> HandleAsync(List<IntegrationImportRequest> requests, CancellationToken ct = default)
    {
        var response = new IntegrationImportResponse
        {
            Success = true,
            CreatedCount = 0,
            UpdatedCount = 0,
            ErrorCount = 0,
            Errors = new List<ImportError>()
        };

        try
        {
            var currentCompany = await companyContext.CurrentCompanyAsync;
            if (currentCompany == null)
            {
                response.Success = false;
                response.Message = "No company context found. Please ensure you are logged in and have access to a company.";
                return response;
            }

            using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var departmentRequests = requests.Where(r => r.EntityType == "Department").ToList();
                var employeeRequests = requests.Where(r => r.EntityType == "Employee").ToList();
                var metricRequests = requests.Where(r => r.EntityType == "Metric").ToList();

                await ProcessDepartmentsAsync(departmentRequests, currentCompany, response, ct);
                await db.SaveChangesAsync(ct);

                await ProcessEmployeesAsync(employeeRequests, currentCompany, response, ct);
                await db.SaveChangesAsync(ct);

                await ProcessMetricsAsync(metricRequests, currentCompany, response, ct);

                await CreateDepartmentEmployeeLinksAsync(employeeRequests, currentCompany, response, ct);

                await db.SaveChangesAsync(ct);
                
                await ClearTempExternalIdsAsync(currentCompany.Id, ct);
                
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Import failed: {ex.Message}";
        }

        return response;
    }

    private async Task ClearTempExternalIdsAsync(int companyId, CancellationToken ct)
    {
        await db.Departments
            .Where(d => d.CompanyId == companyId && d.ExternalId != null && d.ExternalId.StartsWith("temp-"))
            .ExecuteUpdateAsync(d => d.SetProperty(x => x.ExternalId, (string?)null), ct);

        await db.Employees
            .Where(e => e.CompanyId == companyId && e.ExternalId != null && e.ExternalId.StartsWith("temp-"))
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.ExternalId, (string?)null), ct);

        await db.Metrics
            .Where(m => m.CompanyId == companyId && m.ExternalId != null && m.ExternalId.StartsWith("temp-"))
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.ExternalId, (string?)null), ct);

        await db.Indicators
            .Where(i => i.CreatedBy == companyId && i.ExternalId != null && i.ExternalId.StartsWith("temp-"))
            .ExecuteUpdateAsync(i => i.SetProperty(x => x.ExternalId, (string?)null), ct);
    }

    private async Task ProcessDepartmentsAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                
                if (string.IsNullOrEmpty(externalId))
                {
                    var tempId = GetPropertyValue<string>(request.Properties, "DepartmentId");
                    if (!string.IsNullOrEmpty(tempId) && tempId.StartsWith("temp-"))
                    {
                        externalId = tempId;
                    }
                }
                
                var existingDepartment = !string.IsNullOrEmpty(externalId)
                    ? await db.Departments.FirstOrDefaultAsync(d => d.ExternalId == externalId && d.CompanyId == currentCompany.Id, ct)
                    : await db.Departments.FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == request.Name.Trim().ToLower() && d.CompanyId == currentCompany.Id, ct);
                if (existingDepartment == null)
                {
                    existingDepartment = await db.Departments.FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == request.Name.Trim().ToLower() && d.CompanyId == currentCompany.Id, ct);
                }

                var parentDepartmentId = GetPropertyValue<string>(request.Properties, "ParentDepartmentId");
                var hasParent = !string.IsNullOrEmpty(parentDepartmentId);
                    
                DepartmentEntity? parentDepartment = null;
                if (hasParent && !string.IsNullOrEmpty(parentDepartmentId))
                {
                    parentDepartment = await FindDepartmentByNameOrIdAsync(parentDepartmentId, currentCompany.Id, ct);
                }
                
                if (existingDepartment == null)
                {
                    var department = new DepartmentEntity
                    {
                        Name = request.Name,
                        CompanyId = currentCompany.Id,
                        Comment = request.Description,
                        IsActive = true,
                        IsFundamental = !hasParent,
                        IsDeleted = false,
                        ExternalId = externalId
                    };

                    db.Departments.Add(department);
                    await db.SaveChangesAsync(ct);
                    
                    if (hasParent && parentDepartment != null)
                    {
                        await AddDepartmentSchemeAsync(department.Id, parentDepartment.Id, ct);
                    }
                    else
                    {
                        db.DepartmentSchemas.Add(new DepartmentSchemeEntity
                        {
                            FundamentalDepartmentId = department.Id,
                            AncestorDepartmentId = department.Id,
                            DepartmentId = department.Id,
                            Depth = 0
                        });
                    }
                    
                    response.CreatedCount++;
                }
                else
                {
                    var wasFundamental = existingDepartment.IsFundamental;
                    existingDepartment.Name = request.Name;
                    existingDepartment.Comment = request.Description;
                    existingDepartment.IsFundamental = !hasParent;
                    existingDepartment.ExternalId = externalId;
                    
                    if (wasFundamental != !hasParent)
                    {
                        await UpdateDepartmentSchemasAsync(existingDepartment.Id, parentDepartment?.Id, ct);
                    }
                    
                    response.UpdatedCount++;
                }
            }
            catch (Exception ex)
            {
                response.ErrorCount++;
                response.Errors.Add(new ImportError
                {
                    EntityType = request.EntityType,
                    Name = request.Name,
                    Error = ex.Message
                });
            }
        }
    }

    private async Task ProcessEmployeesAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var departmentId = GetPropertyValue<string>(request.Properties, "DepartmentId");
                var fio = GetPropertyValue<string>(request.Properties, "FIO");
                var jobTitle = GetPropertyValue<string>(request.Properties, "JobTitle");
                var defaultRole = GetPropertyValue<string>(request.Properties, "DefaultRole");

                if (string.IsNullOrEmpty(fio))
                {
                    fio = request.Name;
                }
                
                if (string.IsNullOrEmpty(fio))
                {
                    response.ErrorCount++;
                    response.Errors.Add(new ImportError
                    {
                        EntityType = request.EntityType,
                        Name = request.Name,
                        Error = "FIO is required for Employee"
                    });
                    continue;
                }

                DepartmentEntity? department = null;
                if (!string.IsNullOrEmpty(departmentId))
                {
                    department = await FindDepartmentByNameOrIdAsync(departmentId, currentCompany.Id, ct);
                    if (department == null && departmentId.StartsWith("temp-"))
                    {
                        department = null;
                    }
                }

                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                
                if (string.IsNullOrEmpty(externalId))
                {
                    var tempId = GetPropertyValue<string>(request.Properties, "EmployeeId");
                    if (!string.IsNullOrEmpty(tempId) && tempId.StartsWith("temp-"))
                    {
                        externalId = tempId;
                    }
                }
                
                var existingEmployee = !string.IsNullOrEmpty(externalId)
                    ? await db.Employees.FirstOrDefaultAsync(e => e.ExternalId == externalId && e.CompanyId == currentCompany.Id, ct)
                    : await db.Employees.FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == fio.Trim().ToLower() && e.CompanyId == currentCompany.Id, ct);
                if (existingEmployee == null)
                {
                    existingEmployee = await db.Employees.FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == fio.Trim().ToLower() && e.CompanyId == currentCompany.Id, ct);
                }

                if (existingEmployee == null)
                {
                    var employee = new EmployeeEntity
                    {
                        CompanyId = currentCompany.Id,
                        FIO = fio,
                        JobTitle = jobTitle ?? "",
                        Email = $"{fio.ToLower().Replace(" ", ".")}@company.com",
                        Phone = "",
                        Comment = request.Description,
                        IsActive = true,
                        IsArchived = false,
                        Password = "temp_password",
                        Salt = "temp_salt",
                        Role = RootRolesEnum.Employee,
                        DefaultRole = ParseEmployeeType(defaultRole),
                        CreatedAt = DateTime.UtcNow,
                        WelcomeWasSeen = false,
                        ExternalId = externalId
                    };

                    db.Employees.Add(employee);
                    await db.SaveChangesAsync(ct);

                    if (department != null)
                    {
                        var link = new EmployeeDepartmentLinkEntity
                        {
                            EmployeeId = employee.Id,
                            DepartmentId = department.Id,
                            Type = employee.DefaultRole == EmployeeTypeEnum.Supervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
                        };
                        db.EmployeeDepartmentLinks.Add(link);
                    }

                    response.CreatedCount++;
                }
                else
                {
                    existingEmployee.FIO = fio;
                    existingEmployee.JobTitle = jobTitle ?? existingEmployee.JobTitle;
                    existingEmployee.DefaultRole = ParseEmployeeType(defaultRole) ?? existingEmployee.DefaultRole;
                    response.UpdatedCount++;
                }
            }
            catch (Exception ex)
            {
                response.ErrorCount++;
                response.Errors.Add(new ImportError
                {
                    EntityType = request.EntityType,
                    Name = request.Name,
                    Error = ex.Message
                });
            }
        }
    }

    private async Task ProcessMetricsAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var employeeId = GetPropertyValue<string>(request.Properties, "EmployeeId");
                var unit = GetPropertyValue<string>(request.Properties, "Unit");
                var type = GetPropertyValue<string>(request.Properties, "Type");
                var periodType = GetPropertyValue<string>(request.Properties, "PeriodType");

                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                
                if (string.IsNullOrEmpty(externalId))
                {
                    var tempId = GetPropertyValue<string>(request.Properties, "MetricId");
                    if (!string.IsNullOrEmpty(tempId) && tempId.StartsWith("temp-"))
                    {
                        externalId = tempId;
                    }
                }
                
                var existingMetric = !string.IsNullOrEmpty(externalId)
                    ? await db.Metrics.FirstOrDefaultAsync(m => m.ExternalId == externalId && m.CompanyId == currentCompany.Id, ct)
                    : await db.Metrics.FirstOrDefaultAsync(m => m.Name.Trim().ToLower() == request.Name.Trim().ToLower() && m.CompanyId == currentCompany.Id, ct);
                if (existingMetric == null)
                {
                    existingMetric = await db.Metrics.FirstOrDefaultAsync(m => m.Name.Trim().ToLower() == request.Name.Trim().ToLower() && m.CompanyId == currentCompany.Id, ct);
                }

                if (existingMetric == null)
                {
                    var metric = new MetricEntity
                    {
                        CompanyId = currentCompany.Id,
                        Name = request.Name,
                        Description = request.Description,
                        Unit = ParseMetricUnit(unit),
                        Type = ParseMetricType(type),
                        PeriodType = ParsePeriodType(periodType),
                        WeekType = WeekTypeEnum.Calendar,
                        ShowGrowthPercent = false,
                        Visible = true,
                        IsArchived = false,
                        ClickHouseKey = request.Name.ToClickHouseKey(),
                        ExternalId = externalId
                    };

                    db.Metrics.Add(metric);
                    await db.SaveChangesAsync(ct);

                    if (!string.IsNullOrEmpty(employeeId))
                    {
                        var employee = await FindEmployeeByNameOrIdAsync(employeeId, currentCompany.Id, ct);
                        if (employee != null)
                        {
                            var link = new MetricEmployeeLinkEntity
                            {
                                MetricId = metric.Id,
                                EmployeeId = employee.Id
                            };
                            db.MetricEmployeeLinks.Add(link);
                        }
                    }

                    response.CreatedCount++;
                }
                else
                {
                    existingMetric.Name = request.Name;
                    existingMetric.Description = request.Description;
                    if (!string.IsNullOrEmpty(unit))
                        existingMetric.Unit = ParseMetricUnit(unit);
                    if (!string.IsNullOrEmpty(type))
                        existingMetric.Type = ParseMetricType(type);
                    if (!string.IsNullOrEmpty(periodType))
                        existingMetric.PeriodType = ParsePeriodType(periodType);

                    response.UpdatedCount++;
                }
            }
            catch (Exception ex)
            {
                response.ErrorCount++;
                response.Errors.Add(new ImportError
                {
                    EntityType = request.EntityType,
                    Name = request.Name,
                    Error = ex.Message
                });
            }
        }
    }

    private async Task CreateDepartmentEmployeeLinksAsync(List<IntegrationImportRequest> employeeRequests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in employeeRequests)
        {
            try
            {
                var departmentId = GetPropertyValue<string>(request.Properties, "DepartmentId");
                var fio = GetPropertyValue<string>(request.Properties, "FIO");

                if (string.IsNullOrEmpty(departmentId) || string.IsNullOrEmpty(fio))
                    continue;

                var employee = await db.Employees
                    .FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == fio.Trim().ToLower() && e.CompanyId == currentCompany.Id, ct);

                if (employee == null)
                    continue;

                var departmentName = request.Properties.GetValueOrDefault("DepartmentName", "")?.ToString();
                if (string.IsNullOrEmpty(departmentName))
                {
                    if (departmentId.Contains("dept-"))
                    {
                        departmentName = "Разработка (Development Department)";
                    }
                }

                if (!string.IsNullOrEmpty(departmentName))
                {
                    var department = await db.Departments
                        .FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == departmentName.Trim().ToLower() && d.CompanyId == currentCompany.Id, ct);

                    if (department != null)
                    {
                        var existingLink = await db.EmployeeDepartmentLinks
                            .FirstOrDefaultAsync(l => l.EmployeeId == employee.Id && l.DepartmentId == department.Id, ct);

                        if (existingLink == null)
                        {
                            var link = new EmployeeDepartmentLinkEntity
                            {
                                EmployeeId = employee.Id,
                                DepartmentId = department.Id,
                                Type = employee.DefaultRole == EmployeeTypeEnum.Supervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
                            };
                            db.EmployeeDepartmentLinks.Add(link);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.ErrorCount++;
                response.Errors.Add(new ImportError
                {
                    EntityType = request.EntityType,
                    Name = request.Name,
                    Error = $"Failed to create department link: {ex.Message}"
                });
            }
        }
    }

    private async Task<DepartmentEntity?> FindDepartmentByNameOrIdAsync(string departmentId, int companyId, CancellationToken ct)
    {
        if (departmentId.StartsWith("temp-"))
        {
            return await db.Departments
                .FirstOrDefaultAsync(d => d.ExternalId == departmentId && d.CompanyId == companyId, ct);
        }

        var department = await db.Departments
            .FirstOrDefaultAsync(d => d.ExternalId == departmentId && d.CompanyId == companyId, ct);
        
        if (department != null)
            return department;
        if (int.TryParse(departmentId, out int id))
        {
            return await db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, ct);
        }

        return await db.Departments
            .FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == departmentId.Trim().ToLower() && d.CompanyId == companyId, ct);
    }

    private async Task<EmployeeEntity?> FindEmployeeByNameOrIdAsync(string employeeId, int companyId, CancellationToken ct)
    {
        if (employeeId.StartsWith("temp-"))
        {
            return await db.Employees
                .FirstOrDefaultAsync(e => e.ExternalId == employeeId && e.CompanyId == companyId, ct);
        }

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.ExternalId == employeeId && e.CompanyId == companyId, ct);
        
        if (employee != null)
            return employee;
        if (int.TryParse(employeeId, out int id))
        {
            return await db.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, ct);
        }

        return await db.Employees
            .FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == employeeId.Trim().ToLower() && e.CompanyId == companyId, ct);
    }

    private T? GetPropertyValue<T>(Dictionary<string, object> properties, string key)
    {
        if (!properties.ContainsKey(key))
            return default;

        var value = properties[key];
        if (value == null)
            return default;

        try
        {
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    private EmployeeTypeEnum? ParseEmployeeType(string? defaultRole)
    {
        if (string.IsNullOrEmpty(defaultRole))
            return null;

        return defaultRole.ToLower() switch
        {
            "teamlead" or "supervisor" => EmployeeTypeEnum.Supervisor,
            "developer" or "analyst" or "qaengineer" or "engineer" or "bideveloper" or "manager" => EmployeeTypeEnum.Employee,
            _ => EmployeeTypeEnum.Employee
        };
    }

    private MetricUnitEnum ParseMetricUnit(string? unit)
    {
        if (string.IsNullOrEmpty(unit))
            return MetricUnitEnum.Count;

        return unit.ToLower() switch
        {
            "%" => MetricUnitEnum.Percent,
            "часы" or "час" => MetricUnitEnum.Count,
            "дни" or "день" => MetricUnitEnum.Count,
            "минуты" or "минута" => MetricUnitEnum.Count,
            "количество ошибок" or "дефекты" => MetricUnitEnum.Count,
            "story points" => MetricUnitEnum.Count,
            "/5" => MetricUnitEnum.Count,
            _ => MetricUnitEnum.Count
        };
    }

    private MetricTypeEnum ParseMetricType(string? type)
    {
        if (string.IsNullOrEmpty(type))
            return MetricTypeEnum.PlanFact;

        return type.ToLower() switch
        {
            "planfact" => MetricTypeEnum.PlanFact,
            "factonly" => MetricTypeEnum.FactOnly,
            "cumulative" => MetricTypeEnum.Cumulative,
            _ => MetricTypeEnum.PlanFact
        };
    }

    private PeriodTypeEnum ParsePeriodType(string? periodType)
    {
        if (string.IsNullOrEmpty(periodType))
            return PeriodTypeEnum.Month;

        return periodType.ToLower() switch
        {
            "daily" or "day" => PeriodTypeEnum.Day,
            "weekly" or "week" => PeriodTypeEnum.Week,
            "monthly" or "month" => PeriodTypeEnum.Month,
            "quarterly" or "quartal" => PeriodTypeEnum.Quartal,
            "yearly" or "year" => PeriodTypeEnum.Year,
            "sprint" => PeriodTypeEnum.Week,
            _ => PeriodTypeEnum.Month
        };
    }

    private async Task AddDepartmentSchemeAsync(int departmentId, int parentId, CancellationToken ct)
    {
        var parentSchemes = await db.DepartmentSchemas
            .Where(s => s.DepartmentId == parentId)
            .ToListAsync(ct);

        foreach (var scheme in parentSchemes)
        {
            db.DepartmentSchemas.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = scheme.FundamentalDepartmentId,
                AncestorDepartmentId = scheme.AncestorDepartmentId,
                DepartmentId = departmentId,
                Depth = scheme.Depth + 1
            });
        }

        db.DepartmentSchemas.Add(new DepartmentSchemeEntity
        {
            FundamentalDepartmentId = parentSchemes.First().FundamentalDepartmentId,
            AncestorDepartmentId = departmentId,
            DepartmentId = departmentId,
            Depth = 0
        });
    }

    private async Task UpdateDepartmentSchemasAsync(int departmentId, int? newParentId, CancellationToken ct)
    {
        var existingSchemes = await db.DepartmentSchemas
            .Where(s => s.DepartmentId == departmentId)
            .ToListAsync(ct);
        
        db.DepartmentSchemas.RemoveRange(existingSchemes);
        await db.SaveChangesAsync(ct);

        if (newParentId.HasValue)
        {
            await AddDepartmentSchemeAsync(departmentId, newParentId.Value, ct);
        }
        else
        {
            db.DepartmentSchemas.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = departmentId,
                AncestorDepartmentId = departmentId,
                DepartmentId = departmentId,
                Depth = 0
            });
        }
    }

}
