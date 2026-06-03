-- 0011 customers 加 contact_name / email（前端設計稿 UD12）。
-- 客戶詳情頁顯示聯絡人姓名與 email。COL_LENGTH 守衛 → 手動重跑亦 idempotent。

IF COL_LENGTH('dbo.customers', 'contact_name') IS NULL
    ALTER TABLE dbo.customers ADD contact_name NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.customers', 'email') IS NULL
    ALTER TABLE dbo.customers ADD email NVARCHAR(200) NULL;
