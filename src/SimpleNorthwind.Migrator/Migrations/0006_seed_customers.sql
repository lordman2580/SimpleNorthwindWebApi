-- 0006 種子：customers（25 筆，>=20）。create_date 為 UTC。
;WITH n AS
(
    SELECT TOP (25) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS rn
    FROM sys.all_objects
)
INSERT INTO dbo.customers (company_name, contact_number, contact_title, create_date, create_user, is_out_contacted, out_contacted_date)
SELECT
    N'Company ' + RIGHT('000' + CAST(rn AS varchar(10)), 3),
    N'02-1234-' + RIGHT('0000' + CAST(rn AS varchar(10)), 4),
    N'Purchasing Manager',
    CAST('2024-01-01T00:00:00' AS datetime2(0)),
    N'seed',
    0,
    NULL
FROM n;
