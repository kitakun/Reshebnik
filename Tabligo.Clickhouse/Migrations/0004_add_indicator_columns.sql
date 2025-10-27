ALTER TABLE {prefix}_company_metrics
    ADD COLUMN IF NOT EXISTS status String DEFAULT '',
    ADD COLUMN IF NOT EXISTS paid_amount Decimal(18, 2) DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_amount Decimal(18, 2) DEFAULT 0,
    ADD COLUMN IF NOT EXISTS external_id String DEFAULT '';
