-- 0010 discount 語意由「分數 0~1」改為「百分比 0~100」（15.00 = 15%）。
-- 種子 discount 皆為 0，於兩種語意下皆合法 → 無需資料轉換。
-- 於 DB 層加 CHECK 固化 0~100 不變式；以 sys.check_constraints 守衛 → 手動重跑亦 idempotent
-- （runner 另以 schema_versions 控管）。

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_order_details_discount')
    ALTER TABLE dbo.order_details
        ADD CONSTRAINT CK_order_details_discount CHECK (discount >= 0 AND discount <= 100);
