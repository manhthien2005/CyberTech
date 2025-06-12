-- ========================================================
-- FILE: INSERT PRODUCTS - CHUỘT BÁN CHẠY (OPTIMIZED)
-- Mô tả: Thêm 5 sản phẩm chuột vào database CyberTech
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY
-- ========================================================

-- Tạo attributes và category mappings
INSERT INTO ProductAttribute (AttributeName, AttributeType) VALUES 
(N'Cảm biến', 'Text'), (N'LED', 'Text'), (N'DPI', 'Text'),
(N'Kết nối', 'Text'), (N'Số nút', 'Text'), (N'Trọng lượng', 'Text'), (N'Thời lượng pin', 'Text');

INSERT INTO CategoryAttributes (CategoryID, AttributeName) VALUES 
(10, N'Cảm biến'), (10, N'LED'), (10, N'DPI'), (10, N'Kết nối'), 
(10, N'Số nút'), (10, N'Trọng lượng'), (10, N'Thời lượng pin');

-- Variables cho AttributeID
DECLARE @CamBienID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'Cảm biến');
DECLARE @LEDID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'LED');
DECLARE @DPIID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'DPI');
DECLARE @KetNoiID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'Kết nối');
DECLARE @SoNutID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'Số nút');
DECLARE @TrongLuongID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'Trọng lượng');
DECLARE @ThoiLuongPinID INT = (SELECT AttributeID FROM ProductAttribute WHERE AttributeName = N'Thời lượng pin');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM CHUỘT
-- ========================================================

DECLARE @ProductIDs TABLE (ProductID INT, ProductName NVARCHAR(255));

-- Insert all products at once
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
OUTPUT INSERTED.ProductID, INSERTED.Name INTO @ProductIDs
VALUES 
-- Logitech G502 X Plus
(N'Chuột Logitech G502 X Plus LightSpeed Black', 
 N'Chuột gaming không dây HERO 25K, LIGHTSPEED, RGB 8 vùng, 130 giờ pin, 13 nút lập trình, DPI 100-25.600, 106g',
 3590000, 11, 3200000, 50, 303, N'Logitech', 'Active'),
-- Logitech G102 
(N'Chuột Logitech G102 LightSync RGB Black',
 N'Chuột gaming RGB 16.8 triệu màu, Mercury 200-8000 DPI, 6 nút lập trình, Omron 50M clicks, 85g',
 599000, 32, 405000, 100, 303, N'Logitech', 'Active'),
-- Razer White
(N'Chuột Razer DeathAdder Essential White',
 N'Chuột gaming ergonomic, cảm biến quang học 6400 DPI, 5 nút lập trình, thiết kế thuận tay phải, 105g',
 690000, 0, 690000, 80, 303, N'Razer', 'Active'),
-- Razer Black  
(N'Chuột Razer DeathAdder Essential Black',
 N'Chuột gaming ergonomic, cảm biến quang học 6400 DPI, 5 nút lập trình, thiết kế thuận tay phải, màu đen classic',
 690000, 0, 690000, 90, 303, N'Razer', 'Active'),
-- Rapoo M21
(N'Chuột không dây Rapoo M21 Silent',
 N'Chuột văn phòng không dây Silent Click, tiết kiệm pin, thiết kế tối giản, DPI 1600, kết nối 2.4GHz',
 350000, 0, 350000, 150, 303, N'Rapoo', 'Active');

-- Get ProductIDs
DECLARE @G502ID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%G502%');
DECLARE @G102ID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%G102%'); 
DECLARE @RazerWhiteID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%White%');
DECLARE @RazerBlackID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Black%' AND ProductName LIKE N'%Razer%');
DECLARE @RapooID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Rapoo%');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES  
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- G502 Images
(@G502ID, 'https://product.hstatic.net/200000722513/product/g502x-plus-gallery-2-black_1db5bbb43d2f443ea2eaf758a6f97e77_ba770c37d454493f986eaaf4e81bddcf_1024x1024.png', 1, 1),
(@G502ID, 'https://product.hstatic.net/200000722513/product/gitech-g502-x-plus-lightspeed-black-2_d55fc115aec14c00a48cc59ae28b5e35_b86f94147a1b466cb8038f8302b937cc_1024x1024.jpg', 0, 2),
(@G502ID, 'https://product.hstatic.net/200000722513/product/gitech-g502-x-plus-lightspeed-black-3_d52ac757c55041ef9598a2587dde31dd_c13d3cb4606849929af10d223aeb8b50_1024x1024.jpg', 0, 3),
-- G102 Images
(@G102ID, 'https://product.hstatic.net/200000722513/product/logitech-g102-lightsync-rgb-black-1_bf4f5774229c4a0f81b8e8a2feebe4d8_aeb4ae49ee844c3e9d315883d4e482d4_1024x1024.jpg', 1, 1),
(@G102ID, 'https://product.hstatic.net/200000722513/product/logitech-g102-lightsync-rgb-black-2_7788492f5ed748248bd8cb2e967f9cc3_705d7bb9777440eab14aedb8e3975545_1024x1024.jpg', 0, 2),
-- Razer White Images  
(@RazerWhiteID, 'https://product.hstatic.net/200000722513/product/565656_22914bb589c146e599cb381f2c75b557_e6b08a36816248339bcf29ca71560fcb_1024x1024.png', 1, 1),
(@RazerWhiteID, 'https://product.hstatic.net/200000722513/product/fghfghjgfhfg_d0640724df5040709cba326097f94789_b37107e263b5437a96239bbe3884f81c_1024x1024.png', 0, 2),
-- Razer Black Images
(@RazerBlackID, 'https://product.hstatic.net/200000722513/product/thumbchuot_7445abea69bf461e881eeba2b6cbbd8d_1024x1024.jpg', 1, 1),
(@RazerBlackID, 'https://product.hstatic.net/200000722513/product/tttttt_a3febd70c7f74160abf2441546d1a8c0_95c2516690034447b7b8b4bf44b6c631_1024x1024.png', 0, 2),
-- Rapoo Images
(@RapooID, 'https://product.hstatic.net/200000722513/product/thumbchuot_319f928f991a4303a119531a028fca35_caa118908bcb4634847506512cb92ce8_1024x1024.png', 1, 1);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    (@CamBienID, N'HERO 25K'), (@CamBienID, N'Mercury Sensor'), (@CamBienID, N'Optical 6400 DPI'), (@CamBienID, N'Optical 1600 DPI'),
    (@LEDID, N'RGB 8 vùng'), (@LEDID, N'RGB 16.8 triệu màu'), (@LEDID, N'Không có LED'),
    (@DPIID, N'100-25.600'), (@DPIID, N'200-8000'), (@DPIID, N'6400'), (@DPIID, N'1600'),
    (@KetNoiID, N'LIGHTSPEED Wireless'), (@KetNoiID, N'USB có dây'), (@KetNoiID, N'2.4GHz không dây'),
    (@SoNutID, N'13 nút lập trình'), (@SoNutID, N'6 nút'), (@SoNutID, N'5 nút'), (@SoNutID, N'3 nút'),
    (@TrongLuongID, N'106g'), (@TrongLuongID, N'85g'), (@TrongLuongID, N'105g'), (@TrongLuongID, N'70g'),
    (@ThoiLuongPinID, N'130 giờ (37 giờ với RGB)'), (@ThoiLuongPinID, N'Không áp dụng'), (@ThoiLuongPinID, N'12 tháng')
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

-- Helper function to get ValueIDs
WITH ProductAttributeMappings AS (
  SELECT ProductID, av.ValueID FROM (VALUES
    -- G502 mappings
    (@G502ID, N'HERO 25K'), (@G502ID, N'RGB 8 vùng'), (@G502ID, N'100-25.600'), 
    (@G502ID, N'LIGHTSPEED Wireless'), (@G502ID, N'13 nút lập trình'), (@G502ID, N'106g'), (@G502ID, N'130 giờ (37 giờ với RGB)'),
    -- G102 mappings  
    (@G102ID, N'Mercury Sensor'), (@G102ID, N'RGB 16.8 triệu màu'), (@G102ID, N'200-8000'),
    (@G102ID, N'USB có dây'), (@G102ID, N'6 nút'), (@G102ID, N'85g'), (@G102ID, N'Không áp dụng'),
    -- Razer White mappings
    (@RazerWhiteID, N'Optical 6400 DPI'), (@RazerWhiteID, N'6400'), (@RazerWhiteID, N'Không có LED'),
    (@RazerWhiteID, N'USB có dây'), (@RazerWhiteID, N'5 nút'), (@RazerWhiteID, N'105g'), (@RazerWhiteID, N'Không áp dụng'),
    -- Razer Black mappings
    (@RazerBlackID, N'Optical 6400 DPI'), (@RazerBlackID, N'6400'), (@RazerBlackID, N'Không có LED'),
    (@RazerBlackID, N'USB có dây'), (@RazerBlackID, N'5 nút'), (@RazerBlackID, N'105g'), (@RazerBlackID, N'Không áp dụng'),
    -- Rapoo mappings
    (@RapooID, N'Optical 1600 DPI'), (@RapooID, N'1600'), (@RapooID, N'Không có LED'),
    (@RapooID, N'2.4GHz không dây'), (@RapooID, N'3 nút'), (@RapooID, N'70g'), (@RapooID, N'12 tháng')
  ) AS t(ProductID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;


GO 