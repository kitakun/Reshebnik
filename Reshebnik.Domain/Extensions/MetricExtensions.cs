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

        return CalcPercent(factValue, planValue, metric.Plan, metric.Max, metric.Type);
    }

    public static double CalcPercent(decimal fact, decimal? plan, decimal? metricPlan, decimal? max, MetricTypeEnum type)
    {
        var maxFilled = max.HasValue && max.Value != 0m;
        var planVal = plan is > 0 ? plan.Value : metricPlan ?? 0m;

        if (type == MetricTypeEnum.FactOnly)
        {
            if (maxFilled)
            {
                return (double)(fact / max!.Value * 100m);
            }

            if (planVal > 0)
            {
                return (double)(fact / planVal) * 100;
            }

            return fact > 0 ? 100 : 0;
        }

        if (maxFilled)
        {
            return (double)(fact / max!.Value * 100m);
        }

        return (double)(fact / Math.Max(1, planVal) * 100m);
    }
}