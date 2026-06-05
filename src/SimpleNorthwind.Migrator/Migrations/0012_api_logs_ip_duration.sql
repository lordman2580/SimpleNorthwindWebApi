-- 0012 api_logs 加 client_ip / duration_ms（前端設計稿 UD12）。
-- 稽核檢視顯示來源 IP 與處理耗時；由 ApiLogActionFilter 擷取寫入。COL_LENGTH 守衛 idempotent。

IF COL_LENGTH('dbo.api_logs', 'client_ip') IS NULL
    ALTER TABLE dbo.api_logs ADD client_ip NVARCHAR(45) NULL;   -- IPv4 / IPv6

IF COL_LENGTH('dbo.api_logs', 'duration_ms') IS NULL
    ALTER TABLE dbo.api_logs ADD duration_ms INT NULL;
