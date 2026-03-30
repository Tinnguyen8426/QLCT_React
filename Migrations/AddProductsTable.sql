-- Migration Script: Thêm tính năng bán mỹ phẩm
-- Tạo bảng Products để lưu thông tin mỹ phẩm

-- Tạo bảng Products
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Products] (
        [ProductID] INT IDENTITY(1,1) NOT NULL,
        [ProductName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        [Price] DECIMAL(12, 2) NOT NULL,
        [Category] NVARCHAR(50) NULL,
        [StockQuantity] INT NOT NULL DEFAULT 0,
        [Unit] NVARCHAR(50) NULL,
        [ImageUrl] NVARCHAR(255) NULL,
        [Brand] NVARCHAR(100) NULL,
        [CostPrice] DECIMAL(12, 2) NULL,
        [IsActive] BIT DEFAULT 1,
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK__Products__B40CC6CD12345678] PRIMARY KEY ([ProductID])
    );
    PRINT 'Bảng Products đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng Products đã tồn tại.';
END
GO

-- Sửa bảng InvoiceDetails để hỗ trợ cả Service và Product
-- Làm ServiceID nullable
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceDetails]') AND name = 'ServiceID' AND is_nullable = 0)
BEGIN
    ALTER TABLE [InvoiceDetails]
        ALTER COLUMN [ServiceID] INT NULL;
    PRINT 'Đã sửa ServiceID thành nullable.';
END
ELSE
BEGIN
    PRINT 'ServiceID đã là nullable hoặc không tồn tại.';
END
GO

-- Thêm cột ProductID
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceDetails]') AND name = 'ProductID')
BEGIN
    ALTER TABLE [InvoiceDetails]
        ADD [ProductID] INT NULL;
    PRINT 'Đã thêm cột ProductID.';
END
ELSE
BEGIN
    PRINT 'Cột ProductID đã tồn tại.';
END
GO

-- Thêm cột ItemType
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceDetails]') AND name = 'ItemType')
BEGIN
    ALTER TABLE [InvoiceDetails]
        ADD [ItemType] NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột ItemType.';
END
ELSE
BEGIN
    PRINT 'Cột ItemType đã tồn tại.';
END
GO

-- Thêm Foreign Key cho ProductID
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_InvDet_Products')
BEGIN
    ALTER TABLE [InvoiceDetails]
        ADD CONSTRAINT [FK_InvDet_Products]
        FOREIGN KEY ([ProductID]) REFERENCES [Products]([ProductID])
        ON DELETE SET NULL;
    PRINT 'Đã thêm Foreign Key FK_InvDet_Products.';
END
ELSE
BEGIN
    PRINT 'Foreign Key FK_InvDet_Products đã tồn tại.';
END
GO

-- Thêm Check Constraint: phải có ServiceID hoặc ProductID (không được cả hai hoặc không có)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_InvoiceDetail_ServiceOrProduct')
BEGIN
    ALTER TABLE [InvoiceDetails]
        ADD CONSTRAINT [CK_InvoiceDetail_ServiceOrProduct]
        CHECK (([ServiceID] IS NOT NULL AND [ProductID] IS NULL) 
               OR ([ServiceID] IS NULL AND [ProductID] IS NOT NULL));
    PRINT 'Đã thêm Check Constraint CK_InvoiceDetail_ServiceOrProduct.';
END
ELSE
BEGIN
    PRINT 'Check Constraint CK_InvoiceDetail_ServiceOrProduct đã tồn tại.';
END
GO

-- Tạo index cho ProductID để tối ưu hiệu suất
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InvoiceDetails_ProductID' AND object_id = OBJECT_ID(N'[dbo].[InvoiceDetails]'))
BEGIN
    CREATE INDEX [IX_InvoiceDetails_ProductID] ON [InvoiceDetails]([ProductID]);
    PRINT 'Đã tạo index IX_InvoiceDetails_ProductID.';
END
ELSE
BEGIN
    PRINT 'Index IX_InvoiceDetails_ProductID đã tồn tại.';
END
GO

-- Cập nhật dữ liệu hiện có: Set ItemType = 'Service' cho các InvoiceDetail hiện tại có ServiceID
UPDATE [InvoiceDetails]
SET [ItemType] = 'Service'
WHERE [ServiceID] IS NOT NULL AND [ItemType] IS NULL;
GO

PRINT 'Migration hoàn tất!';
PRINT 'Lưu ý: Vui lòng kiểm tra lại dữ liệu và đảm bảo tất cả InvoiceDetail có ServiceID hoặc ProductID.';

