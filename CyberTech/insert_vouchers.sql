
USE cybertech;
GO


INSERT INTO Vouchers (Code, Description, DiscountType, DiscountValue, QuantityAvailable, ValidFrom, ValidTo, IsActive, AppliesTo)
VALUES 
    -- Voucher cho đơn hàng đầu tiên
    ('USERPROMO50', N'Giảm 5% cho đơn hàng đầu tiên của khách hàng mới', 'PERCENT', 5.00, 1000, '2025-01-01', '2025-12-31', 1, 'Order'),
    
    -- Voucher premium
    ('PREMIUM10', N'Giảm 10% cho đơn hàng', 'PERCENT', 10.00, 500, '2025-01-01', '2025-12-31', 1, 'Order'),
    
    -- Voucher theo mùa và sự kiện
    ('SUMMER2025', N'Giảm 15% cho đơn hàng mùa hè', 'PERCENT', 15.00, 200, '2025-06-01', '2025-08-31', 1, 'Order'),
    
    -- Voucher cho sản phẩm cụ thể
    ('LAPTOP10', N'Giảm 10% cho Laptop Gaming', 'PERCENT', 10.00, 100, '2025-01-01', '2025-12-31', 1, 'Product'),
    ('MONITOR15', N'Giảm 15% cho Màn hình', 'PERCENT', 15.00, 150, '2025-01-01', '2025-12-31', 1, 'Product');

-- Thêm mapping giữa voucher và sản phẩm
INSERT INTO VoucherProducts (VoucherID, ProductID)
SELECT v.VoucherID, p.ProductID
FROM Vouchers v
CROSS JOIN Products p
WHERE 
    -- Mapping cho laptop gaming
    ((v.Code = 'LAPTOP10' AND p.SubSubcategoryID IN (
        SELECT SubSubcategoryID 
        FROM SubSubcategory ss
        JOIN Subcategory s ON ss.SubcategoryID = s.SubcategoryID
        WHERE s.CategoryID = 2  -- CategoryID 2 là Laptop Gaming
    ))
    -- Mapping cho màn hình
    OR (v.Code = 'MONITOR15' AND p.SubSubcategoryID IN (
        SELECT SubSubcategoryID 
        FROM SubSubcategory ss
        JOIN Subcategory s ON ss.SubcategoryID = s.SubcategoryID
        WHERE s.CategoryID = 8  -- CategoryID 8 là Màn hình
    ))
    -- Mapping cho phụ kiện gaming
    OR (v.Code = 'GAMING10' AND p.SubSubcategoryID IN (
        SELECT SubSubcategoryID 
        FROM SubSubcategory ss
        JOIN Subcategory s ON ss.SubcategoryID = s.SubcategoryID
        WHERE s.CategoryID IN (9, 10, 11)  -- CategoryID 9,10,11 là Bàn phím, Chuột, Tai nghe
    ))
    -- Mapping cho phụ kiện
    OR (v.Code = 'ACCESSORY20' AND p.SubSubcategoryID IN (
        SELECT SubSubcategoryID 
        FROM SubSubcategory ss
        JOIN Subcategory s ON ss.SubcategoryID = s.SubcategoryID
        WHERE s.CategoryID = 15  -- CategoryID 15 là Phụ kiện
    )))
    AND p.Status = 'Active';  -- Chỉ áp dụng cho sản phẩm đang active