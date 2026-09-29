USE [PCAccess_DB]
GO

-- ==========================================================
-- 1. Ensure Default Admin User Type exists with Dashboard URL
-- ==========================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_user_type] WHERE [user_type_id] = 1)
BEGIN
    INSERT INTO [dbo].[tbl_user_type] ([user_type_id], [user_type], [user_level], [module_url])
    VALUES (1, 'Admin', 1, '/Admin/DashboardOverview');
    PRINT 'User type Admin (ID: 1) created with module_url: /Admin/DashboardOverview';
END
ELSE
BEGIN
    UPDATE [dbo].[tbl_user_type]
    SET [module_url] = '/Admin/DashboardOverview'
    WHERE [user_type_id] = 1;
    PRINT 'User type Admin (ID: 1) updated with module_url: /Admin/DashboardOverview';
END
GO

-- ==========================================================
-- 2. Ensure Default Admin User exists (admin / 123)
-- ==========================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_user] WHERE [user_name] = 'admin')
BEGIN
    INSERT INTO [dbo].[tbl_user] 
    (
        [user_type_id],
        [user_role_id],
        [user_name],
        [password],
        [name],
        [email],
        [phone_no],
        [status],
        [created_date],
        [is_deleted]
    )
    VALUES 
    (
        1,
        1,
        'admin',
        '123',
        'System Administrator',
        'admin@pcaccess.com',
        '9999999999',
        'Active',
        GETDATE(),
        0
    );
    PRINT 'Default Admin user created successfully (username: admin, password: 123)';
END
ELSE
BEGIN
    UPDATE [dbo].[tbl_user]
    SET [password] = '123', [status] = 'Active', [user_type_id] = 1
    WHERE [user_name] = 'admin';
    PRINT 'Default Admin user updated successfully (username: admin, password: 123)';
END
GO
