-- 0008 依種子訂單明細回扣庫存，使起始即符合庫存不變式（quantities >= 0）。
-- 僅計入未取消訂單（取消訂單視同庫存還原）；種子銷量 << 初始 800，必然 >= 0。
UPDATE p
SET p.quantities = p.quantities - s.sold
FROM dbo.products p
INNER JOIN
(
    SELECT od.product_id, SUM(od.order_quantities) AS sold
    FROM dbo.order_details od
    INNER JOIN dbo.orders o ON o.order_id = od.order_id
    WHERE o.is_canceled = 0
    GROUP BY od.product_id
) s ON s.product_id = p.product_id;
