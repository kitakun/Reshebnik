INSERT INTO {prefix}_user_metrics (employee_ids, company_ids, department_id, metric_key, value_type, period_type, upsert_date, value)
SELECT [mel.employee_id], um.company_ids, um.department_id, um.metric_key, um.value_type, um.period_type, um.upsert_date, um.value
FROM {prefix}_user_metrics AS um
INNER JOIN {prefix}_metric_employee_links AS mel
    ON um.metric_key = concat('user-metric-', toString(mel.metric_id))
WHERE length(um.employee_ids) = 1 AND has(um.employee_ids, mel.employee_id) = 0;
