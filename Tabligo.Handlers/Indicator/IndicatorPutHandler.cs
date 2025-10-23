using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.Indicator;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Indicator;

public class IndicatorPutHandler(
    TabligoContext db,
    UserContextHandler userContextHandler,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(IndicatorPutDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var userId = userContextHandler.CurrentUserId;

        IndicatorEntity entity;
        if (dto.Id != 0)
        {
            entity = await db.Indicators
                         .FirstOrDefaultAsync(
                             i => i.Id == dto.Id
                                  && i.CreatedBy == companyId, ct)
                     ?? new IndicatorEntity
                     {
                         CreatedBy = companyId,
                         CreatedAt = DateTime.UtcNow
                     };
            if (entity.Id == 0)
            {
                db.Indicators.Add(entity);
            }
        }
        else
        {
            entity = new IndicatorEntity
            {
                CreatedBy = companyId,
                CreatedAt = DateTime.UtcNow
            };
            db.Indicators.Add(entity);
        }

        entity.Name = dto.Name;
        entity.Category = dto.Category;
        entity.UnitType = dto.UnitType;
        entity.FillmentPeriod = dto.FillmentPeriod;
        entity.ValueType = dto.ValueType;
        entity.Description = dto.Description;
        entity.RejectionTreshold = dto.RejectionTreshold;
        entity.ShowToEmployees = dto.ShowToEmployees;
        entity.ShowOnMainScreen = dto.ShowOnMainScreen;
        entity.ShowOnKeyIndicators = dto.ShowOnKeyIndicators;
        entity.Plan = dto.Plan;
        entity.Min = dto.Min;
        entity.Max = dto.Max;
        entity.EmployeeId = dto.EmployeeId;
        entity.DepartmentId = dto.DepartmentId;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}