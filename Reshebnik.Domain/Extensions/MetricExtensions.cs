using System.Linq;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Metric;

namespace Reshebnik.Domain.Extensions;

public static class MetricExtensions
{
    public static double GetCompletionPercent(this UserPreviewMetricItemDto metric)
    {
        decimal factValue;
        decimal? planValue;
        bool hasFactData;

        if (metric.Period == PeriodTypeEnum.Year)
        {
            factValue = metric.TotalFactData.Sum(x => (decimal)x);
            planValue = metric.TotalPlanData.Sum(x => (decimal)x);
            if (planValue == 0 && metric.Plan.HasValue)
                planValue = metric.Plan;
            hasFactData = metric.TotalFactData.Any(x => x != 0);
        }
        else
        {
            var factArray = metric.Last12PointsFact;
            var planArray = metric.Last12PointsPlan;

            factValue = factArray.Length > 0 ? (decimal)factArray[^1] : 0m;
            planValue = planArray.Length > 0 ? planArray[^1] : metric.Plan;
            hasFactData = factArray.Length > 0 && factArray[^1] != 0;
        }

        if (!hasFactData)
            return 100;

        return CalcPercent(factValue, planValue, metric.Min, metric.Max, metric.Type);
    }

    public static double CalcPercent(decimal fact, decimal? plan, decimal? min, decimal? max, MetricTypeEnum type)
    {
        if (type == MetricTypeEnum.FactOnly)
        {
            return fact > 0 ? 100 : 0;
        }

        if (min.HasValue && max.HasValue && max.Value > min.Value)
        {
            var denominator = max.Value - min.Value;
            if (denominator == 0) return 0;
            var percent = (fact - min.Value) / denominator * 100;
            return double.IsFinite((double)percent) ? (double)percent : 0;
        }

        if (plan.HasValue && plan.Value > 0)
        {
            var percent = fact / plan.Value * 100;
            return double.IsFinite((double)percent) ? (double)percent : 0;
        }

        return 100;
    }
}

