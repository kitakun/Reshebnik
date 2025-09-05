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
        var planFilled = plan.HasValue && plan.Value != 0m;
        var minFilled  = min.HasValue  && min.Value  != 0m;
        var maxFilled  = max.HasValue  && max.Value  != 0m;

        // if type is FactOnly OR plan/min/max all empty -> 0% when fact==0, else 100%
        if (type == MetricTypeEnum.FactOnly || (!planFilled && !minFilled && !maxFilled))
            return fact == 0m ? 0d : 100d;

        // plan wins when present and non-zero
        if (planFilled && plan.HasValue)
            return (double)(fact / plan.Value * 100m);

        // otherwise, if max present -> fact/max * 100
        if (!planFilled && maxFilled && max.HasValue)
            return (double)(fact / max.Value * 100m);

        // fallback
        return fact == 0m ? 0d : 100d;
    }
}

