using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Indicator;

public class IndicatorTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<IndicatorDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 50;

        var query = db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId)
            .Take(COUNT);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(q) || i.Category.ToLower().Contains(q));
        }

        var items = await query
            .Skip((Math.Max(request.Page ?? 1, 1) - 1) * COUNT)
            .ToListAsync(ct);

        var count = await query.CountAsync(ct);

        var list = items.Select(i => new IndicatorDto
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
            EmployeeId = i.EmployeeId,
            DepartmentId = i.DepartmentId,
            CreatedAt = i.CreatedAt,
            CreatedBy = i.CreatedBy
        }).ToList();

        return new PaginationDto<IndicatorDto>(list, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
