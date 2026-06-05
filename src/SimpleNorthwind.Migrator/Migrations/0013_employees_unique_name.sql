-- 0013 員工種子改實名（唯一）+ 加 UNIQUE(first_name, last_name)（登入改用姓名，UD13）。
-- 先把既有 12 筆（0005 之 First/Last N）依 employee_id 實名化為唯一的 Northwind 風格姓名，
-- 再加唯一約束。對 fresh 與既有 DB 皆 idempotent（rename 為確定性 UPDATE、約束以 sys.indexes 守衛）。

;WITH names AS (
    SELECT * FROM (VALUES
        (1,  N'Nancy',    N'Davolio'),
        (2,  N'Andrew',   N'Fuller'),
        (3,  N'Janet',    N'Leverling'),
        (4,  N'Margaret', N'Peacock'),
        (5,  N'Steven',   N'Buchanan'),
        (6,  N'Michael',  N'Suyama'),
        (7,  N'Robert',   N'King'),
        (8,  N'Laura',    N'Callahan'),
        (9,  N'Anne',     N'Dodsworth'),
        (10, N'Sandra',   N'Mills'),
        (11, N'David',    N'Park'),
        (12, N'Emily',    N'Stone')
    ) AS v(employee_id, first_name, last_name)
)
UPDATE e
    SET e.first_name = n.first_name,
        e.last_name  = n.last_name
FROM dbo.employees e
JOIN names n ON n.employee_id = e.employee_id;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_employees_name' AND object_id = OBJECT_ID('dbo.employees'))
    ALTER TABLE dbo.employees ADD CONSTRAINT UQ_employees_name UNIQUE (first_name, last_name);
