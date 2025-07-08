CREATE TABLE IF NOT EXISTS {prefix}_user_metrics (
    employee_id Int32,
    company_id Int32,
    department_id Nullable(Int32),
    metric_key String,
    value_type String,
    period_type String,
    upsert_date Date,
    value Int32
) ENGINE = ReplacingMergeTree()
ORDER BY (employee_id, company_id, metric_key, value_type, period_type, upsert_date);
