CREATE TABLE IF NOT EXISTS {prefix}_user_metrics (
    employee_ids Array(Int32),
    company_ids Array(Int32),
    department_id Nullable(Int32),
    metric_key String,
    value_type String,
    period_type String,
    upsert_date Date,
    value Int32
) ENGINE = ReplacingMergeTree()
ORDER BY (employee_ids, company_ids, metric_key, value_type, period_type, upsert_date);
