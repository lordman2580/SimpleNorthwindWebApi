-- 0009 api_logs 增加回應稽核欄位：HTTP 狀態碼與回應結果（response_status / response_result）。
-- 以 COL_LENGTH 守衛 → 手動重跑也 idempotent（runner 另以 schema_versions 控管）。

IF COL_LENGTH('dbo.api_logs', 'response_status') IS NULL
    ALTER TABLE dbo.api_logs ADD response_status INT NULL;

IF COL_LENGTH('dbo.api_logs', 'response_result') IS NULL
    ALTER TABLE dbo.api_logs ADD response_result NVARCHAR(MAX) NULL;
