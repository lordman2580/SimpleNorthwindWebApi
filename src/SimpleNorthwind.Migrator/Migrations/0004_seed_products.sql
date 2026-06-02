-- 0004 種子：products（120 筆，>=100）。初始庫存高（800），確定性序列。
;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS rn
    FROM sys.all_objects
)
INSERT INTO dbo.products (product_name, category_id, quantities, unit_price)
SELECT
    N'Product ' + RIGHT('000' + CAST(rn AS varchar(10)), 3),
    ((rn - 1) % (SELECT COUNT(*) FROM dbo.categories)) + 1,
    800,
    CAST(9 + (rn % 90) AS DECIMAL(18,2)) + CAST(0.99 AS DECIMAL(18,2))
FROM n;
