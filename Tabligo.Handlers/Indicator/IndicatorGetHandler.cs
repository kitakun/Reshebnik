using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models.Indicator;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Indicator;

public class IndicatorGetHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<IndicatorDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var indicator = await db.Indicators
            .AsNoTracking()
            .FirstAsync(i => i.Id == id && i.CreatedBy == companyId, ct);

        return new IndicatorDto
        {
            Id = indicator.Id,
            Name = indicator.Name,
            Category = indicator.Category,
            UnitType = indicator.UnitType,
            FillmentPeriod = indicator.FillmentPeriod,
            ValueType = indicator.ValueType,
            Description = indicator.Description,
            RejectionTreshold = indicator.RejectionTreshold,
            ShowToEmployees = indicator.ShowToEmployees,
            ShowOnMainScreen = indicator.ShowOnMainScreen,
            ShowOnKeyIndicators = indicator.ShowOnKeyIndicators,
            Plan = indicator.Plan,
            Min = indicator.Min,
            Max = indicator.Max,
            EmployeeId = indicator.EmployeeId,
            DepartmentId = indicator.DepartmentId,
            CreatedAt = indicator.CreatedAt,
            CreatedBy = indicator.CreatedBy
        };
    }

    public async ValueTask<List<IndicatorDto>> HandleAsync(CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId)
            .ToListAsync(ct);

        return indicators.Select(i => new IndicatorDto
        {
            Id = i.Id,
            Name = i.Name,
            Category = i.Category,
            UnitType = i.UnitType,
            FillmentPeriod = i.FillmentPeriod,
            ValueType = i.ValueType,
            Description = i.Description,
            RejectionTreshold = i.RejectionTreshold,
            ShowToEmployees = i.ShowToEmployees,
            ShowOnMainScreen = i.ShowOnMainScreen,
            ShowOnKeyIndicators = i.ShowOnKeyIndicators,
            Plan = i.Plan,
            Min = i.Min,
            Max = i.Max,
            EmployeeId = i.EmployeeId,
            DepartmentId = i.DepartmentId,
            CreatedAt = i.CreatedAt,
            CreatedBy = i.CreatedBy
        }).ToList();
    }
}
