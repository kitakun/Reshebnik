CREATE TABLE IF NOT EXISTS {prefix}_company_metrics (
    company_id Int32,
    metric_key String,
    period_type String,
    upsert_date Date,
    value Int32
) ENGINE = ReplacingMergeTree()
ORDER BY (company_id, metric_key, period_type, upsert_date);
