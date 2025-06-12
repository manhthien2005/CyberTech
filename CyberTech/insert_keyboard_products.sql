-- ========================================================
-- FILE: INSERT PRODUCTS - BÀN PHÍM CƠ BÁN CHẠY
-- Mô tả: Thêm 5 sản phẩm bàn phím cơ vào database CyberTech từ GearVN
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY CHO BÀN PHÍM
-- ========================================================

-- Tạo attributes cho bàn phím (kiểm tra tồn tại trước)
INSERT INTO ProductAttribute (AttributeName, AttributeType) 
SELECT * FROM (VALUES
  (N'Thương hiệu', 'Text'), (N'Kết nối', 'Text'), (N'Switch', 'Text'),
  (N'Layout', 'Text'), (N'Màu sắc', 'Text'), (N'Kiểu bàn phím', 'Text'), (N'Pin', 'Text')
) AS NewAttrs(AttributeName, AttributeType)
WHERE NOT EXISTS (
  SELECT 1 FROM ProductAttribute pa 
  WHERE pa.AttributeName = NewAttrs.AttributeName
);

-- CategoryID cho Bàn phím = 9
DECLARE @KeyboardCategoryID INT = 9;

INSERT INTO CategoryAttributes (CategoryID, AttributeName)
SELECT * FROM (VALUES
  (@KeyboardCategoryID, N'Thương hiệu'), (@KeyboardCategoryID, N'Kết nối'), (@KeyboardCategoryID, N'Switch'), (@KeyboardCategoryID, N'Layout'), 
  (@KeyboardCategoryID, N'Màu sắc'), (@KeyboardCategoryID, N'Kiểu bàn phím'), (@KeyboardCategoryID, N'Pin')
) AS NewCatAttrs(CategoryID, AttributeName)
WHERE NOT EXISTS (
  SELECT 1 FROM CategoryAttributes ca 
  WHERE ca.CategoryID = NewCatAttrs.CategoryID AND ca.AttributeName = NewCatAttrs.AttributeName
);

-- Variables cho AttributeID
DECLARE @ThuongHieuID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Thương hiệu');
DECLARE @KetNoiID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Kết nối');
DECLARE @SwitchID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Switch');
DECLARE @LayoutID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Layout');
DECLARE @MauSacID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Màu sắc');
DECLARE @KieuBanPhimID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Kiểu bàn phím');
DECLARE @PinID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Pin');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM BÀN PHÍM BÁN CHẠY
-- ========================================================

-- Sử dụng SubSubcategoryID hợp lệ cho CategoryID = 9 (Bàn phím)
DECLARE @ValidSubSubcategoryID INT = (
  SELECT TOP 1 ssc.SubSubcategoryID 
  FROM SubSubcategory ssc 
  INNER JOIN Subcategory sc ON ssc.SubcategoryID = sc.SubcategoryID 
  WHERE sc.CategoryID = 9 AND ssc.Name LIKE N'%AULA%'
);

-- Fallback nếu không tìm thấy, lấy SubSubcategoryID đầu tiên của CategoryID = 9
IF @ValidSubSubcategoryID IS NULL
BEGIN
  SET @ValidSubSubcategoryID = (
    SELECT TOP 1 ssc.SubSubcategoryID 
    FROM SubSubcategory ssc 
    INNER JOIN Subcategory sc ON ssc.SubcategoryID = sc.SubcategoryID 
    WHERE sc.CategoryID = 9
  );
END

-- Insert all keyboards at once với SubSubcategoryID hợp lệ
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
VALUES 
-- 1. Bàn phím AULA F68 - Tím - Ice Crystal Switch (giá tham khảo: 1.990k → 1.690k, -15%)
(N'Bàn phím AULA F68 - Tím - Ice Crystal Switch',
 N'Bàn phím cơ AULA F68 layout 65%, kết nối 3 chế độ (Bluetooth/2.4G/USB-C), vỏ trong suốt, Ice Crystal Switch, RGB đầy màu sắc',
 1990000, 15, 1690000, 25, @ValidSubSubcategoryID, N'AULA', 'Active'),

-- 2. Bàn phím Leobog AMG65 - Tím Đen - Jadeite Switch (giá tham khảo: 2.490k → 2.190k, -12%)
(N'Bàn phím Leobog AMG65 - Tím Đen - Jadeite Switch',
 N'Bàn phím cơ Leobog AMG65 layout 65%, gasket mount, hot-swap switch, Jadeite Switch tactile, màu tím đen độc đáo, RGB per-key',
 2490000, 12, 2190000, 20, @ValidSubSubcategoryID, N'Leobog', 'Active'),

-- 3. Bàn phím cơ Dareu EK75 Pro Triple Mode - Black Golden Cloud Switch (giá tham khảo: 1.890k → 1.590k, -16%)
(N'Bàn phím cơ Dareu EK75 Pro Triple Mode - Black Golden Cloud Switch',
 N'Bàn phím cơ Dareu EK75 Pro layout 75%, kết nối 3 chế độ, gasket mount, Golden Cloud Switch linear, south-facing RGB, màu đen chuyên nghiệp',
 1890000, 16, 1590000, 30, @ValidSubSubcategoryID, N'Dareu', 'Active'),

-- 4. Bàn phím Leobog AMG65 - Trắng - Jadeite Switch (giá tham khảo: 2.490k → 2.290k, -8%)
(N'Bàn phím Leobog AMG65 - Trắng - Jadeite Switch',
 N'Bàn phím cơ Leobog AMG65 layout 65%, gasket mount, hot-swap switch, Jadeite Switch tactile, màu trắng thanh lịch, RGB per-key',
 2490000, 8, 2290000, 18, @ValidSubSubcategoryID, N'Leobog', 'Active'),

-- 5. Bàn phím Leobog Hi86 - Đen Hồng - Star Vector Switch (giá tham khảo: 2.990k → 2.690k, -10%)
(N'Bàn phím Leobog Hi86 - Đen Hồng - Star Vector Switch',
 N'Bàn phím cơ Leobog Hi86 layout 75%, cấu trúc nhôm CNC, gasket mount, Star Vector Switch linear, màu đen hồng nổi bật, RGB tùy chỉnh',
 2990000, 10, 2690000, 15, @ValidSubSubcategoryID, N'Leobog', 'Active');

-- Lấy ProductIDs với logic đơn giản
DECLARE @AulaF68ID INT = (SELECT ProductID FROM Products WHERE Name = N'Bàn phím AULA F68 - Tím - Ice Crystal Switch' AND Brand = N'AULA');
DECLARE @LeobogAMG65PurpleID INT = (SELECT ProductID FROM Products WHERE Name = N'Bàn phím Leobog AMG65 - Tím Đen - Jadeite Switch' AND Brand = N'Leobog');
DECLARE @DareuEK75ProID INT = (SELECT ProductID FROM Products WHERE Name = N'Bàn phím cơ Dareu EK75 Pro Triple Mode - Black Golden Cloud Switch' AND Brand = N'Dareu');
DECLARE @LeobogAMG65WhiteID INT = (SELECT ProductID FROM Products WHERE Name = N'Bàn phím Leobog AMG65 - Trắng - Jadeite Switch' AND Brand = N'Leobog');
DECLARE @LeobogHi86ID INT = (SELECT ProductID FROM Products WHERE Name = N'Bàn phím Leobog Hi86 - Đen Hồng - Star Vector Switch' AND Brand = N'Leobog');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES (THEO YÊU CẦU CỤ THỂ)
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- Sản phẩm 1: AULA F68 - 1 ảnh
(@AulaF68ID, 'https://product.hstatic.net/200000722513/product/f68__15__6a2c4e3f753d4ccd9b9fb84906d15c87_1024x1024.png', 1, 1),

-- Sản phẩm 2: Leobog AMG65 Tím Đen - 1 ảnh
(@LeobogAMG65PurpleID, 'https://product.hstatic.net/200000722513/product/amg65_35afdddaaabb46ed8523e52013d5fffd_1024x1024.png', 1, 1),

-- Sản phẩm 3: Dareu EK75 Pro - 5 ảnh
(@DareuEK75ProID, 'https://product.hstatic.net/200000722513/product/download__2__8212092b7bf74608ba37d25630cae5e0_1024x1024.png', 1, 1),
(@DareuEK75ProID, 'https://product.hstatic.net/200000722513/product/download__5__3ababa19642f42ef8c4b89f43c7df94e_1024x1024.png', 0, 2),
(@DareuEK75ProID, 'https://product.hstatic.net/200000722513/product/download__4__7466bc167e9e4991a77f26fe2b68ff6e_1024x1024.png', 0, 3),
(@DareuEK75ProID, 'https://product.hstatic.net/200000722513/product/download__6__e5d4ededa6a64ef4b4d44230ba24a9b2_1024x1024.png', 0, 4),
(@DareuEK75ProID, 'https://product.hstatic.net/200000722513/product/download__3__ccf52dd691fe4b3598ddf14f1f5bfbc4_1024x1024.png', 0, 5),

-- Sản phẩm 4: Leobog AMG65 Trắng - 1 ảnh
(@LeobogAMG65WhiteID, 'https://product.hstatic.net/200000722513/product/amg65_a6a5e42ac0e84a1db2ce578bfce839e5_1024x1024.png', 1, 1),

-- Sản phẩm 5: Leobog Hi86 - 4 ảnh
(@LeobogHi86ID, 'https://product.hstatic.net/200000722513/product/1_edb0eaf8b2994509bbee55e64ea275c0_1024x1024.png', 1, 1),
(@LeobogHi86ID, 'https://product.hstatic.net/200000722513/product/2_5c3dbbec9a0e40609eb032c22da9ac67_1024x1024.png', 0, 2),
(@LeobogHi86ID, 'https://product.hstatic.net/200000722513/product/3_15f6bdb08e634727b24cc05b7447eae1_1024x1024.png', 0, 3),
(@LeobogHi86ID, 'https://product.hstatic.net/200000722513/product/4_c64059bd02bd4ec5a4cfb61654357fd7_1024x1024.png', 0, 4);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    -- Thương hiệu Values
    (@ThuongHieuID, N'AULA'), (@ThuongHieuID, N'Leobog'), (@ThuongHieuID, N'Dareu'),
    -- Kết nối Values  
    (@KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), (@KetNoiID, N'Bluetooth 5.0'), (@KetNoiID, N'2.4GHz Wireless'), (@KetNoiID, N'USB-C Wired'),
    -- Switch Values
    (@SwitchID, N'Ice Crystal Switch'), (@SwitchID, N'Jadeite Switch'), (@SwitchID, N'Golden Cloud Switch'), (@SwitchID, N'Star Vector Switch'),
    -- Layout Values
    (@LayoutID, N'65% Layout'), (@LayoutID, N'75% Layout'),
    -- Màu sắc Values
    (@MauSacID, N'Tím'), (@MauSacID, N'Tím Đen'), (@MauSacID, N'Đen'), (@MauSacID, N'Trắng'), (@MauSacID, N'Đen Hồng'),
    -- Kiểu bàn phím Values
    (@KieuBanPhimID, N'Cơ - Gasket Mount'), (@KieuBanPhimID, N'Cơ - Hot-swap'), (@KieuBanPhimID, N'Cơ - RGB'),
    -- Pin Values
    (@PinID, N'2800mAh'), (@PinID, N'3000mAh'), (@PinID, N'4000mAh')
  ) AS t(AttributeID, ValueName)
)
INSERT INTO AttributeValue (AttributeID, ValueName)
SELECT ad.AttributeID, ad.ValueName
FROM AttributeData ad
WHERE NOT EXISTS (
  SELECT 1 FROM AttributeValue av 
  WHERE av.AttributeID = ad.AttributeID AND av.ValueName = ad.ValueName
);

-- ========================================================
-- BATCH INSERT: PRODUCT-ATTRIBUTE MAPPINGS
-- ========================================================

WITH ProductAttributeMappings AS (
  SELECT ProductID, av.ValueID FROM (VALUES
    -- AULA F68 mappings
    (@AulaF68ID, @ThuongHieuID, N'AULA'), 
    (@AulaF68ID, @KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), 
    (@AulaF68ID, @SwitchID, N'Ice Crystal Switch'),
    (@AulaF68ID, @LayoutID, N'65% Layout'), 
    (@AulaF68ID, @MauSacID, N'Tím'), 
    (@AulaF68ID, @KieuBanPhimID, N'Cơ - Gasket Mount'), 
    (@AulaF68ID, @PinID, N'2800mAh'),
    
    -- Leobog AMG65 Tím Đen mappings  
    (@LeobogAMG65PurpleID, @ThuongHieuID, N'Leobog'), 
    (@LeobogAMG65PurpleID, @KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), 
    (@LeobogAMG65PurpleID, @SwitchID, N'Jadeite Switch'),
    (@LeobogAMG65PurpleID, @LayoutID, N'65% Layout'), 
    (@LeobogAMG65PurpleID, @MauSacID, N'Tím Đen'), 
    (@LeobogAMG65PurpleID, @KieuBanPhimID, N'Cơ - Hot-swap'), 
    (@LeobogAMG65PurpleID, @PinID, N'3000mAh'),
    
    -- Dareu EK75 Pro mappings
    (@DareuEK75ProID, @ThuongHieuID, N'Dareu'), 
    (@DareuEK75ProID, @KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), 
    (@DareuEK75ProID, @SwitchID, N'Golden Cloud Switch'),
    (@DareuEK75ProID, @LayoutID, N'75% Layout'), 
    (@DareuEK75ProID, @MauSacID, N'Đen'), 
    (@DareuEK75ProID, @KieuBanPhimID, N'Cơ - Gasket Mount'), 
    (@DareuEK75ProID, @PinID, N'3000mAh'),
    
    -- Leobog AMG65 Trắng mappings
    (@LeobogAMG65WhiteID, @ThuongHieuID, N'Leobog'), 
    (@LeobogAMG65WhiteID, @KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), 
    (@LeobogAMG65WhiteID, @SwitchID, N'Jadeite Switch'),
    (@LeobogAMG65WhiteID, @LayoutID, N'65% Layout'), 
    (@LeobogAMG65WhiteID, @MauSacID, N'Trắng'), 
    (@LeobogAMG65WhiteID, @KieuBanPhimID, N'Cơ - Hot-swap'), 
    (@LeobogAMG65WhiteID, @PinID, N'3000mAh'),
    
    -- Leobog Hi86 mappings
    (@LeobogHi86ID, @ThuongHieuID, N'Leobog'), 
    (@LeobogHi86ID, @KetNoiID, N'Triple Mode (BT/2.4G/USB-C)'), 
    (@LeobogHi86ID, @SwitchID, N'Star Vector Switch'),
    (@LeobogHi86ID, @LayoutID, N'75% Layout'), 
    (@LeobogHi86ID, @MauSacID, N'Đen Hồng'), 
    (@LeobogHi86ID, @KieuBanPhimID, N'Cơ - RGB'), 
    (@LeobogHi86ID, @PinID, N'4000mAh')
  ) AS t(ProductID, AttributeID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName AND av.AttributeID = t.AttributeID
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;

-- ========================================================
-- HOÀN THÀNH: BÀN PHÍM CƠ BÁN CHẠY ĐÃ ĐƯỢC THÊM
-- ========================================================

PRINT 'Successfully inserted 5 keyboard products with attributes and images.';
GO 