using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Metric;

namespace Reshebnik.Domain.Extensions;

public static class MetricExtensions
{
    public static double GetCompletionPercent(this UserPreviewMetricItemDto metric)
    {
        decimal factValue;
        decimal? planValue;

        if (metric.Period == PeriodTypeEnum.Year)
        {
            factValue = metric.TotalFactData.Sum(x => (decimal)x);
            planValue = metric.TotalPlanData.Sum(x => (decimal)x);
            if ((planValue == null || planValue == 0) && metric.Plan.HasValue)
                planValue = metric.Plan;
        }
        else
        {
            var factArray = metric.Last12PointsFact;
            var planArray = metric.Last12PointsPlan;

            factValue = factArray.Length > 0 ? (decimal)factArray[^1] : 0m;
            planValue = planArray.Length > 0 ? planArray[^1] : metric.Plan;
        }

        return CalcPercent(factValue, planValue, metric.Min, metric.Max, metric.Type);
    }

    public static double CalcPercent(decimal fact, decimal? plan, decimal? min, decimal? max, MetricTypeEnum type)
    {
        var planFilled = plan.HasValue && plan.Value != 0;
        var minFilled = min.HasValue && min.Value != 0;
        var maxFilled = max.HasValue && max.Value != 0;

        if (type == MetricTypeEnum.FactOnly || (!planFilled && !minFilled && !maxFilled))
        {
            return fact == 0 ? 0 : 100;
        }

        if (planFilled)
        {
            var percent = fact / plan.Value * 100;
            return double.IsFinite((double)percent) ? (double)percent : 0;
        }

        if (!planFilled && maxFilled)
        {
            var percent = fact / max.Value * 100;
            return double.IsFinite((double)percent) ? (double)percent : 0;
        }

        return fact == 0 ? 0 : 100;
    }
}

