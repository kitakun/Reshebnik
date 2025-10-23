ALTER TABLE {prefix}_company_metrics
DROP COLUMN IF EXISTS value,
    ADD COLUMN IF NOT EXISTS plan_value Int32 DEFAULT 0,
    ADD COLUMN IF NOT EXISTS fact_value Int32 DEFAULT 0;
