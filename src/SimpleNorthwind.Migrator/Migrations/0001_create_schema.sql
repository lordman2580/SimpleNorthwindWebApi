-- 0001 建表：型別、IDENTITY、BIT、datetime2(0)、欄位預設值。
-- PK / FK / CHECK / Index 見 0002。日期欄一律儲存 UTC。

CREATE TABLE dbo.categories
(
    category_id   INT IDENTITY(1,1) NOT NULL,
    category_name NVARCHAR(100)     NOT NULL,
    description   NVARCHAR(500)     NULL
);

CREATE TABLE dbo.products
(
    product_id   INT IDENTITY(1,1) NOT NULL,
    product_name NVARCHAR(150)     NOT NULL,
    category_id  INT               NOT NULL,
    quantities   INT               NOT NULL,
    unit_price   DECIMAL(18,2)     NOT NULL
);

CREATE TABLE dbo.customers
(
    customer_id        INT IDENTITY(1,1) NOT NULL,
    company_name       NVARCHAR(150)     NOT NULL,
    contact_number     NVARCHAR(50)      NULL,
    contact_title      NVARCHAR(50)      NULL,
    create_date        datetime2(0)      NOT NULL,
    create_user        NVARCHAR(100)     NOT NULL,
    is_out_contacted   BIT               NOT NULL CONSTRAINT DF_customers_is_out_contacted DEFAULT (0),
    out_contacted_date datetime2(0)      NULL
);

CREATE TABLE dbo.employees
(
    employee_id      INT IDENTITY(1,1) NOT NULL,
    password         NVARCHAR(256)     NOT NULL,
    last_name        NVARCHAR(50)      NOT NULL,
    first_name       NVARCHAR(50)      NOT NULL,
    title            NVARCHAR(50)      NULL,
    birth_date       datetime2(0)      NULL,
    hire_date        datetime2(0)      NULL,
    phone_ext_number NVARCHAR(50)      NULL,
    phone_number     NVARCHAR(50)      NULL,
    notes            NVARCHAR(MAX)     NULL,
    is_resigned      BIT               NOT NULL CONSTRAINT DF_employees_is_resigned DEFAULT (0),
    resign_date      datetime2(0)      NULL
);

CREATE TABLE dbo.orders
(
    order_id             INT IDENTITY(1,1) NOT NULL,
    customer_id          INT               NOT NULL,
    employee_id          INT               NOT NULL,
    order_date           datetime2(0)      NOT NULL,
    modified_employee_id INT               NULL,
    modified_date        datetime2(0)      NULL,
    is_canceled          BIT               NOT NULL CONSTRAINT DF_orders_is_canceled DEFAULT (0),
    is_paidoff           BIT               NOT NULL CONSTRAINT DF_orders_is_paidoff DEFAULT (0)
);

CREATE TABLE dbo.order_details
(
    order_id         INT          NOT NULL,
    product_id       INT          NOT NULL,
    order_quantities INT          NOT NULL,
    discount         DECIMAL(5,2) NOT NULL CONSTRAINT DF_order_details_discount DEFAULT (0),
    version          INT          NOT NULL CONSTRAINT DF_order_details_version DEFAULT (1)
);

CREATE TABLE dbo.api_logs
(
    guid          UNIQUEIDENTIFIER NOT NULL,
    user_id       INT              NULL,
    actions       NVARCHAR(200)    NOT NULL,
    action_detail NVARCHAR(MAX)    NULL,
    summary_date  datetime2(0)     NOT NULL
);
