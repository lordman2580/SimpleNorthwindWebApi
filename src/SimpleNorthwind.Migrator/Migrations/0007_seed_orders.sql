-- 0007 種子：orders（120 筆，>=100）+ order_details。
-- 過半訂單（order_id 1..90，共 90 > 60）含 2 個不同產品；其餘單一產品。order_date 為 UTC。

;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS rn
    FROM sys.all_objects
)
INSERT INTO dbo.orders (customer_id, employee_id, order_date, modified_employee_id, modified_date, is_canceled, is_paidoff)
SELECT
    ((rn - 1) % 25) + 1,
    ((rn - 1) % 12) + 1,
    DATEADD(DAY, rn, CAST('2024-01-01T00:00:00' AS datetime2(0))),
    NULL,
    NULL,
    CASE WHEN rn % 23 = 0 THEN 1 ELSE 0 END,
    CASE WHEN rn % 17 = 0 THEN 1 ELSE 0 END
FROM n;

-- 每張訂單第 1 筆明細（product_id = order_id 對應，數量 2）
INSERT INTO dbo.order_details (order_id, product_id, order_quantities, discount, version)
SELECT o.order_id, ((o.order_id - 1) % 120) + 1, 2, CAST(0 AS DECIMAL(5,2)), 1
FROM dbo.orders o;

-- 過半訂單（order_id <= 90）加第 2 筆「不同」產品明細（product_id = order_id 的下一個，數量 1）
INSERT INTO dbo.order_details (order_id, product_id, order_quantities, discount, version)
SELECT o.order_id, (o.order_id % 120) + 1, 1, CAST(0 AS DECIMAL(5,2)), 1
FROM dbo.orders o
WHERE o.order_id <= 90;
