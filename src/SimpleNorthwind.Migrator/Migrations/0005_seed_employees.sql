-- 0005 種子：employees（12 筆，>=10）。
-- password = PasswordHasher<T> 產生的 PBKDF2-HMAC-SHA256 雜湊，明文密碼為 P@ssw0rd!（供登入測試）。
;WITH n AS
(
    SELECT TOP (12) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS rn
    FROM sys.all_objects
)
INSERT INTO dbo.employees (password, last_name, first_name, title, birth_date, hire_date, phone_ext_number, phone_number, is_resigned)
SELECT
    N'AQAAAAIAAYagAAAAED/lAVxQtYz3dvgGvHhF77wzNluAgdDet4YNNJlX5TCsjH9fdK2FpZ9t1/yUdBbatw==',
    N'Last' + CAST(rn AS varchar(10)),
    N'First' + CAST(rn AS varchar(10)),
    CASE WHEN rn = 1 THEN N'Sales Manager' ELSE N'Sales Representative' END,
    NULL,
    CAST('2020-01-01T00:00:00' AS datetime2(0)),
    NULL,
    N'02-2700-0000',
    0
FROM n;
