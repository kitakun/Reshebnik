INSERT INTO {prefix}_user_metrics (employee_ids, company_ids, department_id, metric_key, value_type, period_type, upsert_date, value)
SELECT [employee_id], [company_id], department_id, metric_key, value_type, period_type, upsert_date, value
FROM {prefix}_user_metrics
ARRAY JOIN employee_ids AS employee_id, company_ids AS company_id
WHERE length(employee_ids) > 1;
