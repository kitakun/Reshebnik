using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.IndicatorCategory;

public class IndicatorCategoryCommentUpdateHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(string categoryName, string comment, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var record = await db.CategoryRecords
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Name == categoryName, ct);
        if (record == null)
        {
            record = new CategoryRecordEntity
            {
                CompanyId = companyId,
                Name = categoryName,
                Comment = comment
            };
            db.CategoryRecords.Add(record);
        }
        else
        {
            record.Comment = comment;
        }

        await db.SaveChangesAsync(ct);
    }
}
