-- 0002 PK / FK / CHECK / Index

-- Primary keys
ALTER TABLE dbo.categories    ADD CONSTRAINT PK_categories    PRIMARY KEY (category_id);
ALTER TABLE dbo.products      ADD CONSTRAINT PK_products      PRIMARY KEY (product_id);
ALTER TABLE dbo.customers     ADD CONSTRAINT PK_customers     PRIMARY KEY (customer_id);
ALTER TABLE dbo.employees     ADD CONSTRAINT PK_employees     PRIMARY KEY (employee_id);
ALTER TABLE dbo.orders        ADD CONSTRAINT PK_orders        PRIMARY KEY (order_id);
ALTER TABLE dbo.order_details ADD CONSTRAINT PK_order_details PRIMARY KEY (order_id, product_id);
ALTER TABLE dbo.api_logs      ADD CONSTRAINT PK_api_logs      PRIMARY KEY (guid);

-- Foreign keys
ALTER TABLE dbo.products      ADD CONSTRAINT FK_products_categories      FOREIGN KEY (category_id)          REFERENCES dbo.categories (category_id);
ALTER TABLE dbo.orders        ADD CONSTRAINT FK_orders_customers         FOREIGN KEY (customer_id)          REFERENCES dbo.customers (customer_id);
ALTER TABLE dbo.orders        ADD CONSTRAINT FK_orders_employees         FOREIGN KEY (employee_id)          REFERENCES dbo.employees (employee_id);
ALTER TABLE dbo.orders        ADD CONSTRAINT FK_orders_modified_employee FOREIGN KEY (modified_employee_id) REFERENCES dbo.employees (employee_id);
ALTER TABLE dbo.order_details ADD CONSTRAINT FK_order_details_orders     FOREIGN KEY (order_id)             REFERENCES dbo.orders (order_id);
ALTER TABLE dbo.order_details ADD CONSTRAINT FK_order_details_products   FOREIGN KEY (product_id)           REFERENCES dbo.products (product_id);

-- Check constraints（庫存不可為負；訂購數量需為正）
ALTER TABLE dbo.products      ADD CONSTRAINT CK_products_quantities      CHECK (quantities >= 0);
ALTER TABLE dbo.order_details ADD CONSTRAINT CK_order_details_quantities CHECK (order_quantities > 0);

-- Non-clustered indexes
CREATE INDEX IX_products_category_id     ON dbo.products (category_id);
CREATE INDEX IX_customers_company_name   ON dbo.customers (company_name);
CREATE INDEX IX_orders_customer_id       ON dbo.orders (customer_id);
CREATE INDEX IX_orders_employee_id       ON dbo.orders (employee_id);
CREATE INDEX IX_orders_order_date        ON dbo.orders (order_date);
CREATE INDEX IX_order_details_product_id ON dbo.order_details (product_id);
CREATE INDEX IX_api_logs_user_id         ON dbo.api_logs (user_id);
CREATE INDEX IX_api_logs_summary_date    ON dbo.api_logs (summary_date);
