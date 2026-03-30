-- Thêm cột theo dõi trạng thái & địa chỉ giao hàng cho hóa đơn bán mỹ phẩm
IF COL_LENGTH('dbo.Invoices', 'FulfillmentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD FulfillmentStatus NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Invoices', 'ShippingAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD ShippingAddress NVARCHAR(255) NULL;
END
GO

-- Đặt mặc định trạng thái cho các hóa đơn có sản phẩm
UPDATE i
SET FulfillmentStatus = 'Pending'
FROM dbo.Invoices i
WHERE i.FulfillmentStatus IS NULL
  AND EXISTS (
      SELECT 1
      FROM dbo.InvoiceDetails d
      WHERE d.InvoiceID = i.InvoiceID
        AND d.ProductID IS NOT NULL
  );
GO
