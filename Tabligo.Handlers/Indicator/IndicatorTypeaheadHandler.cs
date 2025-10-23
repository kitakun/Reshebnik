using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Indicator;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Indicator;

public class IndicatorTypeaheadHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<IndicatorDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var query = db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(q) || i.Category.ToLower().Contains(q));
        }

        var page = Math.Max(request.Page, 1);

        var items = await query
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
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
            Plan = i.Plan,
            Min = i.Min,
            Max = i.Max,
            EmployeeId = i.EmployeeId,
            DepartmentId = i.DepartmentId,
            CreatedAt = i.CreatedAt,
            CreatedBy = i.CreatedBy
        }).ToList();

        return new PaginationDto<IndicatorDto>(list, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
