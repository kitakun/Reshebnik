namespace Tabligo.Domain.Enums;

public readonly struct FillmentPeriodWrapper(FillmentPeriodEnum value)
{
    public FillmentPeriodEnum Value { get; } = value;

    public static implicit operator PeriodTypeEnum(FillmentPeriodWrapper wrapper)
    {
        return wrapper.Value switch
        {
            FillmentPeriodEnum.Daily => PeriodTypeEnum.Day,
            FillmentPeriodEnum.Weekly => PeriodTypeEnum.Week,
            FillmentPeriodEnum.Monthly => PeriodTypeEnum.Month,
            _ => throw new InvalidCastException($"Unsupported FillmentPeriodEnum value: {wrapper.Value}")
        };
    }

    public static explicit operator FillmentPeriodWrapper(PeriodTypeEnum period)
    {
        return period switch
        {
            PeriodTypeEnum.Day => new FillmentPeriodWrapper(FillmentPeriodEnum.Daily),
            PeriodTypeEnum.Week => new FillmentPeriodWrapper(FillmentPeriodEnum.Weekly),
            PeriodTypeEnum.Month => new FillmentPeriodWrapper(FillmentPeriodEnum.Monthly),
            _ => throw new InvalidCastException($"Cannot cast PeriodTypeEnum {period} to FillmentPeriodEnum")
        };
    }

    public static implicit operator FillmentPeriodEnum(FillmentPeriodWrapper wrapper) => wrapper.Value;
    public static implicit operator FillmentPeriodWrapper(FillmentPeriodEnum value) => new(value);
}