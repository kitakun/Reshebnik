using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Extensions;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;
using Tabligo.Clickhouse.Handlers;
using Tabligo.Clickhouse.Models;
using System.Text.Json;

namespace Tabligo.Handlers.Integration;

public class IntegrationImportHandler(TabligoContext db, CompanyContextHandler companyContext, ExternalIdLinkHandler externalIdLinkHandler, PutIndicatorValuesHandler putIndicatorHandler)
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

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var departmentRequests = requests.Where(r => r.EntityType == "Department").ToList();
                var employeeRequests = requests.Where(r => r.EntityType == "Employee").ToList();
                var metricRequests = requests.Where(r => r.EntityType == "Metric").ToList();
                var indicatorRequests = requests.Where(r => r.EntityType == "Indicator").ToList();

                // Sort departments by dependencies: process departments without parents first
                departmentRequests = SortDepartmentsByDependencies(departmentRequests);
                
                await ProcessDepartmentsAsync(departmentRequests, currentCompany, response, ct);
                await db.SaveChangesAsync(ct);

                await ProcessEmployeesAsync(employeeRequests, currentCompany, response, ct);
                await db.SaveChangesAsync(ct);

                await ProcessMetricsAsync(metricRequests, currentCompany, response, ct);
                await ProcessIndicatorsAsync(indicatorRequests, currentCompany, response, ct);

                await CreateDepartmentEmployeeLinksAsync(employeeRequests, currentCompany, response, ct);

                await db.SaveChangesAsync(ct);
                
                await externalIdLinkHandler.ClearTempLinksAsync(currentCompany.Id, ct);
                
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

    private async Task ProcessDepartmentsAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var integrationType = ParseIntegrationType(request.SourceSystem);
                
                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                
                if (string.IsNullOrEmpty(externalId))
                {
                    var tempId = GetPropertyValue<string>(request.Properties, "DepartmentId");
                    if (!string.IsNullOrEmpty(tempId) && tempId.StartsWith("temp-"))
                    {
                        externalId = tempId;
                    }
                }
                
                DepartmentEntity? existingDepartment = null;
                
                if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                {
                    existingDepartment = await externalIdLinkHandler.FindEntityAsync<DepartmentEntity>(
                        currentCompany.Id, externalId, integrationType.Value, ct);
                }
                
                if (existingDepartment == null && !string.IsNullOrEmpty(request.Name))
                {
                    existingDepartment = await db.Departments
                        .FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == request.Name.Trim().ToLower() 
                                                  && d.CompanyId == currentCompany.Id, ct);
                }

                var parentDepartmentId = GetPropertyValue<string>(request.Properties, "ParentDepartmentId");
                var hasParent = !string.IsNullOrEmpty(parentDepartmentId);
                    
                DepartmentEntity? parentDepartment = null;
                if (hasParent && !string.IsNullOrEmpty(parentDepartmentId))
                {
                    parentDepartment = await FindDepartmentByNameOrIdAsync(parentDepartmentId, currentCompany.Id, integrationType, ct);
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
                        IsDeleted = false
                    };

                    db.Departments.Add(department);
                    await db.SaveChangesAsync(ct);
                    
                    // Create ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(DepartmentEntity),
                            department.Id,
                            ct);
                    }
                    
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
                    
                    // Update ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(DepartmentEntity),
                            existingDepartment.Id,
                            ct);
                    }
                    
                    // Get current parent from department schemas
                    var currentParentId = await db.DepartmentSchemas
                        .Where(s => s.DepartmentId == existingDepartment.Id && s.Depth == 1 && s.AncestorDepartmentId != existingDepartment.Id)
                        .Select(s => (int?)s.AncestorDepartmentId)
                        .FirstOrDefaultAsync(ct);
                    
                    var newParentId = parentDepartment?.Id;
                    
                    // Update schemas if parent changed or fundamental status changed
                    if (currentParentId != newParentId || wasFundamental != !hasParent)
                    {
                        await UpdateDepartmentSchemasAsync(existingDepartment.Id, newParentId, ct);
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
        // Check if company has only one department
        var companyDepartmentCount = await db.Departments
            .CountAsync(d => d.CompanyId == currentCompany.Id, ct);
        
        DepartmentEntity? singleDepartment = null;
        if (companyDepartmentCount == 1)
        {
            singleDepartment = await db.Departments
                .FirstOrDefaultAsync(d => d.CompanyId == currentCompany.Id, ct);
        }

        foreach (var request in requests)
        {
            try
            {
                var integrationType = ParseIntegrationType(request.SourceSystem);
                
                var departmentId = GetPropertyValue<string>(request.Properties, "DepartmentId");
                var fio = GetPropertyValue<string>(request.Properties, "FIO");
                var jobTitle = GetPropertyValue<string>(request.Properties, "JobTitle");
                var defaultRole = GetPropertyValue<string>(request.Properties, "DefaultRole");
                var email = GetPropertyValue<string>(request.Properties, "Email");

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
                    department = await FindDepartmentByNameOrIdAsync(departmentId, currentCompany.Id, integrationType, ct);
                }

                // If department was not found and company has only one department, use it
                if (department == null && singleDepartment != null)
                {
                    department = singleDepartment;
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
                
                EmployeeEntity? existingEmployee = null;
                
                if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                {
                    existingEmployee = await externalIdLinkHandler.FindEntityAsync<EmployeeEntity>(
                        currentCompany.Id, externalId, integrationType.Value, ct);
                }
                
                if (existingEmployee == null && !string.IsNullOrEmpty(fio))
                {
                    existingEmployee = await db.Employees
                        .FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == fio.Trim().ToLower() 
                                                  && e.CompanyId == currentCompany.Id, ct);
                }

                if (existingEmployee == null)
                {
                    var employee = new EmployeeEntity
                    {
                        CompanyId = currentCompany.Id,
                        FIO = fio,
                        JobTitle = jobTitle ?? "",
                        Email = !string.IsNullOrEmpty(email) ? email : $"{fio.ToLower().Replace(" ", ".")}@company.com",
                        Phone = "",
                        Comment = request.Description,
                        IsActive = true,
                        IsArchived = false,
                        Password = "temp_password",
                        Salt = "temp_salt",
                        Role = RootRolesEnum.Employee,
                        DefaultRole = ParseEmployeeType(defaultRole),
                        CreatedAt = DateTime.UtcNow,
                        WelcomeWasSeen = false
                    };

                    db.Employees.Add(employee);
                    await db.SaveChangesAsync(ct);
                    
                    // Create ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(EmployeeEntity),
                            employee.Id,
                            ct);
                    }

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
                    if (!string.IsNullOrEmpty(email))
                    {
                        existingEmployee.Email = email;
                    }
                    
                    // Update ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(EmployeeEntity),
                            existingEmployee.Id,
                            ct);
                    }

                    // Create department link if department exists and link doesn't exist
                    if (department != null)
                    {
                        var existingLink = await db.EmployeeDepartmentLinks
                            .FirstOrDefaultAsync(l => l.EmployeeId == existingEmployee.Id && l.DepartmentId == department.Id, ct);

                        if (existingLink == null)
                        {
                            var link = new EmployeeDepartmentLinkEntity
                            {
                                EmployeeId = existingEmployee.Id,
                                DepartmentId = department.Id,
                                Type = existingEmployee.DefaultRole == EmployeeTypeEnum.Supervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
                            };
                            db.EmployeeDepartmentLinks.Add(link);
                        }
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

    private async Task ProcessMetricsAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var integrationType = ParseIntegrationType(request.SourceSystem);
                
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
                
                MetricEntity? existingMetric = null;
                
                if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                {
                    existingMetric = await externalIdLinkHandler.FindEntityAsync<MetricEntity>(
                        currentCompany.Id, externalId, integrationType.Value, ct);
                }
                
                if (existingMetric == null && !string.IsNullOrEmpty(request.Name))
                {
                    existingMetric = await db.Metrics
                        .FirstOrDefaultAsync(m => m.Name.Trim().ToLower() == request.Name.Trim().ToLower() 
                                                  && m.CompanyId == currentCompany.Id, ct);
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
                        ClickHouseKey = request.Name.ToClickHouseKey()
                    };

                    db.Metrics.Add(metric);
                    await db.SaveChangesAsync(ct);
                    
                    // Create ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(MetricEntity),
                            metric.Id,
                            ct);
                    }

                    if (!string.IsNullOrEmpty(employeeId))
                    {
                        var employee = await FindEmployeeByNameOrIdAsync(employeeId, currentCompany.Id, integrationType, ct);
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
                    
                    // Update ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(MetricEntity),
                            existingMetric.Id,
                            ct);
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

    private async Task ProcessIndicatorsAsync(List<IntegrationImportRequest> requests, CompanyEntity currentCompany, IntegrationImportResponse response, CancellationToken ct)
    {
        foreach (var request in requests)
        {
            try
            {
                var integrationType = ParseIntegrationType(request.SourceSystem);

                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                var unitType = GetPropertyValue<string>(request.Properties, "UnitType") ?? GetPropertyValue<string>(request.Properties, "Unit");
                var fillmentPeriod = GetPropertyValue<string>(request.Properties, "FillmentPeriod");
                var valueType = GetPropertyValue<string>(request.Properties, "ValueType");
                var showOnMainScreen = GetPropertyValue<bool?>(request.Properties, "ShowOnMainScreen");
                var showOnKeyIndicators = GetPropertyValue<bool?>(request.Properties, "ShowOnKeyIndicators");
                var rejectionTreshold = GetPropertyValue<decimal?>(request.Properties, "RejectionTreshold");

                IndicatorEntity? existingIndicator = null;

                if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                {
                    existingIndicator = await externalIdLinkHandler.FindEntityAsync<IndicatorEntity>(
                        currentCompany.Id, externalId, integrationType.Value, ct);
                }

                if (existingIndicator == null && !string.IsNullOrEmpty(request.Name))
                {
                    existingIndicator = await db.Indicators
                        .FirstOrDefaultAsync(i => i.Name.Trim().ToLower() == request.Name.Trim().ToLower()
                                                  && i.CreatedByCompany.Id == currentCompany.Id, ct);
                }

                if (existingIndicator == null)
                {
                    var indicator = new IndicatorEntity
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Category = GetPropertyValue<string>(request.Properties, "Category") ?? "Интеграции",
                        UnitType = ParseIndicatorUnitType(unitType),
                        FillmentPeriod = ParseFillmentPeriod(fillmentPeriod),
                        ValueType = ParseIndicatorValueType(valueType),
                        RejectionTreshold = rejectionTreshold.HasValue ? rejectionTreshold.Value : 0,
                        ShowToEmployees = true,
                        ShowOnMainScreen = showOnMainScreen.HasValue ? showOnMainScreen.Value : true,
                        ShowOnKeyIndicators = showOnKeyIndicators.HasValue ? showOnKeyIndicators.Value : true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentCompany.Id,
                        IsArchived = false
                    };

                    db.Indicators.Add(indicator);
                    await db.SaveChangesAsync(ct);

                    // Create ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(IndicatorEntity),
                            indicator.Id,
                            ct);
                    }

                    // Save indicator values to ClickHouse
                    await SaveIndicatorValuesAsync(indicator.Id, currentCompany.Id, indicator.FillmentPeriod, request, ct);

                    response.CreatedCount++;
                }
                else
                {
                    existingIndicator.Name = request.Name;
                    existingIndicator.Description = request.Description;

                    // Update ExternalIdLink if externalId exists
                    if (!string.IsNullOrEmpty(externalId) && integrationType.HasValue)
                    {
                        await externalIdLinkHandler.LinkAsync(
                            currentCompany.Id,
                            externalId,
                            integrationType.Value,
                            nameof(IndicatorEntity),
                            existingIndicator.Id,
                            ct);
                    }

                    // Save indicator values to ClickHouse
                    await SaveIndicatorValuesAsync(existingIndicator.Id, currentCompany.Id, existingIndicator.FillmentPeriod, request, ct);

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

    private async Task<DepartmentEntity?> FindDepartmentByNameOrIdAsync(string departmentId, int companyId, IntegrationTypeEnum? integrationType, CancellationToken ct)
    {
        if (departmentId.StartsWith("temp-") && integrationType.HasValue)
        {
            return await externalIdLinkHandler.FindEntityAsync<DepartmentEntity>(
                companyId, departmentId, integrationType.Value, ct);
        }

        if (!string.IsNullOrEmpty(departmentId) && !departmentId.StartsWith("temp-") && integrationType.HasValue)
        {
            var department = await externalIdLinkHandler.FindEntityAsync<DepartmentEntity>(
                companyId, departmentId, integrationType.Value, ct);
            
            if (department != null)
                return department;
        }
        
        if (int.TryParse(departmentId, out int id))
        {
            return await db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, ct);
        }

        return await db.Departments
            .FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == departmentId.Trim().ToLower() && d.CompanyId == companyId, ct);
    }

    private async Task<EmployeeEntity?> FindEmployeeByNameOrIdAsync(string employeeId, int companyId, IntegrationTypeEnum? integrationType, CancellationToken ct)
    {
        if (employeeId.StartsWith("temp-") && integrationType.HasValue)
        {
            return await externalIdLinkHandler.FindEntityAsync<EmployeeEntity>(
                companyId, employeeId, integrationType.Value, ct);
        }

        if (!string.IsNullOrEmpty(employeeId) && !employeeId.StartsWith("temp-") && integrationType.HasValue)
        {
            var employee = await externalIdLinkHandler.FindEntityAsync<EmployeeEntity>(
                companyId, employeeId, integrationType.Value, ct);
            
            if (employee != null)
                return employee;
        }
        
        if (int.TryParse(employeeId, out int id))
        {
            return await db.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, ct);
        }

        return await db.Employees
            .FirstOrDefaultAsync(e => e.FIO.Trim().ToLower() == employeeId.Trim().ToLower() && e.CompanyId == companyId, ct);
    }
    
    private IntegrationTypeEnum? ParseIntegrationType(string? sourceSystem)
    {
        if (string.IsNullOrEmpty(sourceSystem))
            return null;
        
        // Handle neural-file-process special case
        if (sourceSystem == "neural-file-process" || sourceSystem == JobOperationTypes.NeuralFileProcess)
        {
            return IntegrationTypeEnum.NeuralFileProcess;
        }
            
        return Enum.TryParse<IntegrationTypeEnum>(sourceSystem, true, out var type) ? type : null;
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

    private IndicatorUnitTypeEnum ParseIndicatorUnitType(string? unitType)
    {
        if (string.IsNullOrEmpty(unitType))
            return IndicatorUnitTypeEnum.Pieces;

        return unitType.ToLower() switch
        {
            "pieces" or "штуки" => IndicatorUnitTypeEnum.Pieces,
            "rubles" or "рубли" => IndicatorUnitTypeEnum.Rubles,
            "items" or "элементы" => IndicatorUnitTypeEnum.Items,
            "cr" => IndicatorUnitTypeEnum.CR,
            "percent" or "проценты" or "%" => IndicatorUnitTypeEnum.Percent,
            _ => IndicatorUnitTypeEnum.Pieces
        };
    }

    private FillmentPeriodEnum ParseFillmentPeriod(string? fillmentPeriod)
    {
        if (string.IsNullOrEmpty(fillmentPeriod))
            return FillmentPeriodEnum.Monthly;

        return fillmentPeriod.ToLower() switch
        {
            "daily" or "день" => FillmentPeriodEnum.Daily,
            "weekly" or "неделя" => FillmentPeriodEnum.Weekly,
            "monthly" or "месяц" => FillmentPeriodEnum.Monthly,
            _ => FillmentPeriodEnum.Monthly
        };
    }

    private IndicatorValueTypeEnum ParseIndicatorValueType(string? valueType)
    {
        if (string.IsNullOrEmpty(valueType))
            return IndicatorValueTypeEnum.Integer;

        return valueType.ToLower() switch
        {
            "fraction" or "дробное" => IndicatorValueTypeEnum.Fraction,
            "integer" or "целое" => IndicatorValueTypeEnum.Integer,
            "percent" or "проценты" => IndicatorValueTypeEnum.Percent,
            _ => IndicatorValueTypeEnum.Integer
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

    private async Task SaveIndicatorValuesAsync(int indicatorId, int companyId, FillmentPeriodEnum fillmentPeriod, IntegrationImportRequest request, CancellationToken ct)
    {
        try
        {
            var values = new List<IndicatorValue>();

            // Try to get Orders array
            if (request.Properties.TryGetValue("Orders", out var ordersObj))
            {
                if (ordersObj is JsonElement ordersElement && ordersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var orderElement in ordersElement.EnumerateArray())
                    {
                        if (orderElement.ValueKind == JsonValueKind.Object)
                        {
                            var dateStr = orderElement.GetProperty("Date").GetString();
                            var status = orderElement.GetProperty("Status").GetString() ?? "";
                            var paid = orderElement.GetProperty("Paid").GetDecimal();
                            var totalCost = orderElement.GetProperty("TotalCost").GetDecimal();
                            var orderExternalId = orderElement.GetProperty("ExternalId").GetString() ?? "";

                            if (DateTime.TryParse(dateStr, out var date))
                            {
                                // Normalize date based on fillment period
                                var normalizedDate = NormalizeDateByPeriod(date, fillmentPeriod);

                                values.Add(new IndicatorValue
                                {
                                    Date = normalizedDate,
                                    Status = status,
                                    PaidAmount = Math.Round(paid, 2),
                                    TotalAmount = Math.Round(totalCost, 2),
                                    ExternalId = orderExternalId
                                });
                            }
                        }
                    }
                }
            }

            // Try to get Payments array
            if (request.Properties.TryGetValue("Payments", out var paymentsObj))
            {
                if (paymentsObj is JsonElement paymentsElement && paymentsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var paymentElement in paymentsElement.EnumerateArray())
                    {
                        if (paymentElement.ValueKind == JsonValueKind.Object)
                        {
                            var dateStr = paymentElement.GetProperty("Date").GetString();
                            var status = paymentElement.GetProperty("Status").GetString() ?? "";
                            var amount = paymentElement.GetProperty("Amount").GetDecimal();

                            if (DateTime.TryParse(dateStr, out var date))
                            {
                                // Normalize date based on fillment period
                                var normalizedDate = NormalizeDateByPeriod(date, fillmentPeriod);

                                values.Add(new IndicatorValue
                                {
                                    Date = normalizedDate,
                                    Status = status,
                                    PaidAmount = Math.Round(amount, 2),
                                    TotalAmount = Math.Round(amount, 2)
                                });
                            }
                        }
                    }
                }
            }

            // Group values by period, date, and status, sum PaidAmount and TotalAmount
            var groupedValues = values
                .GroupBy(v => new { v.Date, v.Status })
                .Select(g => new IndicatorValue
                {
                    Date = g.Key.Date,
                    Status = g.Key.Status,
                    PaidAmount = Math.Round(g.Sum(v => v.PaidAmount), 2),
                    TotalAmount = Math.Round(g.Sum(v => v.TotalAmount), 2),
                    ExternalId = g.First().ExternalId // Keep first external ID for reference
                })
                .ToList();

            if (groupedValues.Any())
            {
                await putIndicatorHandler.PutIndicatorValuesAsync(indicatorId, companyId, fillmentPeriod, groupedValues, ct);
            }
        }
        catch
        {
            // Log error but don't fail the entire import
            // You might want to add logging here
        }
    }

    private List<IntegrationImportRequest> SortDepartmentsByDependencies(List<IntegrationImportRequest> departmentRequests)
    {
        var sorted = new List<IntegrationImportRequest>();
        var remaining = new List<IntegrationImportRequest>(departmentRequests);
        var processedExternalIds = new HashSet<string>();
        var maxIterations = remaining.Count; // Prevent infinite loops
        var iterations = 0;

        while (remaining.Any() && iterations < maxIterations)
        {
            iterations++;
            var addedInThisIteration = false;

            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                var request = remaining[i];
                var parentDepartmentId = GetPropertyValue<string>(request.Properties, "ParentDepartmentId");
                var externalId = GetPropertyValue<string>(request.Properties, "ExternalId");
                
                // If no parent, or parent is already processed
                if (string.IsNullOrEmpty(parentDepartmentId) || processedExternalIds.Contains(parentDepartmentId))
                {
                    sorted.Add(request);
                    remaining.RemoveAt(i);
                    
                    if (!string.IsNullOrEmpty(externalId))
                    {
                        processedExternalIds.Add(externalId);
                    }
                    
                    addedInThisIteration = true;
                }
            }

            // If no departments were added in this iteration, break to avoid infinite loop
            if (!addedInThisIteration)
            {
                // Add remaining departments anyway (they might have circular dependencies)
                sorted.AddRange(remaining);
                break;
            }
        }

        return sorted;
    }

    private DateTime NormalizeDateByPeriod(DateTime date, FillmentPeriodEnum period)
    {
        return period switch
        {
            FillmentPeriodEnum.Daily => date.Date,
            FillmentPeriodEnum.Weekly => date.Date.AddDays(-(int)date.DayOfWeek), // Start of week (Sunday = 0)
            FillmentPeriodEnum.Monthly => new DateTime(date.Year, date.Month, 1),
            _ => date.Date
        };
    }
}
