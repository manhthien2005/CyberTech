-- ========================================================
-- FILE: INSERT PRODUCTS - MÀN HÌNH BÁN CHẠY (OPTIMIZED)
-- Mô tả: Thêm 5 sản phẩm màn hình vào database CyberTech từ GearVN
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY CHO MÀN HÌNH
-- ========================================================

-- Tạo attributes cho màn hình (kiểm tra tồn tại trước) - SỬ DỤNG THUỘC TÍNH MỚI
INSERT INTO ProductAttribute (AttributeName, AttributeType) 
SELECT * FROM (VALUES
  (N'Kích thước', 'Text'), (N'Độ phân giải', 'Text'), (N'Tần số quét', 'Text'),
  (N'Tấm nền', 'Text'), (N'Thời gian phản hồi', 'Text'), (N'Công nghệ đồng bộ', 'Text'), (N'Cổng kết nối', 'Text')
) AS NewAttrs(AttributeName, AttributeType)
WHERE NOT EXISTS (
  SELECT 1 FROM ProductAttribute pa 
  WHERE pa.AttributeName = NewAttrs.AttributeName
);

-- Kiểm tra CategoryID cho màn hình
DECLARE @MonitorCategoryID INT;
SELECT @MonitorCategoryID = CategoryID FROM Category WHERE Name LIKE N'%màn hình%' OR Name LIKE N'%monitor%';

-- Nếu không tìm thấy, tạo category mới
IF @MonitorCategoryID IS NULL
BEGIN
    INSERT INTO Category (Name, Description) VALUES (N'Màn hình', N'Các sản phẩm màn hình máy tính');
    SET @MonitorCategoryID = SCOPE_IDENTITY();
END

INSERT INTO CategoryAttributes (CategoryID, AttributeName)
SELECT * FROM (VALUES
  (@MonitorCategoryID, N'Kích thước'), (@MonitorCategoryID, N'Độ phân giải'), (@MonitorCategoryID, N'Tần số quét'), (@MonitorCategoryID, N'Tấm nền'), 
  (@MonitorCategoryID, N'Thời gian phản hồi'), (@MonitorCategoryID, N'Công nghệ đồng bộ'), (@MonitorCategoryID, N'Cổng kết nối')
) AS NewCatAttrs(CategoryID, AttributeName)
WHERE NOT EXISTS (
  SELECT 1 FROM CategoryAttributes ca 
  WHERE ca.CategoryID = NewCatAttrs.CategoryID AND ca.AttributeName = NewCatAttrs.AttributeName
);

-- Variables cho AttributeID - SỬ DỤNG TÊN THUỘC TÍNH MỚI
DECLARE @KichThuocID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Kích thước');
DECLARE @DoPhanGiaiID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Độ phân giải');
DECLARE @TanSoQuetID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Tần số quét');
DECLARE @TamNenID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Tấm nền');
DECLARE @ThoiGianPhanHoiID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Thời gian phản hồi');
DECLARE @CongNgheDongBoID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Công nghệ đồng bộ');
DECLARE @CongKetNoiID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Cổng kết nối');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM MÀN HÌNH BÁN CHẠY
-- ========================================================

DECLARE @ProductIDs TABLE (ProductID INT, ProductName NVARCHAR(255));

-- Insert all products at once
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
OUTPUT INSERTED.ProductID, INSERTED.Name INTO @ProductIDs
VALUES 
-- 1. ViewSonic XG2409 24" IPS 180Hz Gsync (từ GearVN: 3.990k → 2.690k, -33%)
(N'Màn hình ViewSonic XG2409 24" IPS 180Hz Gsync chuyên game',
 N'Màn hình 24" SuperClear IPS, 180Hz, 1ms, G-Sync Compatible, Eye ProTech, 104% sRGB, FHD 1920x1080',
 3990000, 33, 2690000, 25, 230, N'ViewSonic', 'Active'), -- SubSubcategoryID 230 = "ViewSonic"

-- 2. ASUS TUF Gaming VG27AQ5A 27" Fast IPS 2K 210Hz
(N'Màn hình ASUS TUF Gaming VG27AQ5A 27" Fast IPS 2K 210Hz chuyên game',
 N'Màn hình 27" Fast IPS, QHD 2560x1440, 210Hz, 1ms, G-Sync Compatible, ELMB Sync, HDR10',
 6990000, 15, 5940000, 20, 229, N'ASUS', 'Active'), -- SubSubcategoryID 229 = "Asus"

-- 3. MSI MAG 276CF E20 27" 200Hz Curved Gaming
(N'Màn hình cong MSI MAG 276CF E20 27" 200Hz chuyên game',
 N'Màn hình cong 27" VA, FHD 1920x1080, 200Hz, 1ms, FreeSync Premium, RGB Mystic Light',
 4490000, 18, 3690000, 18, 232, N'MSI', 'Active'), -- SubSubcategoryID 232 = "Gigabyte" (sử dụng cho MSI tạm thời)

-- 4. AOC 24G4H 24" Fast IPS 200Hz Gaming  
(N'Màn hình AOC 24G4H 24" Fast IPS 200Hz chuyên game',
 N'Màn hình 24" Fast IPS, FHD 1920x1080, 200Hz, 1ms, FreeSync Premium, Low Input Lag Mode',
 2990000, 20, 2390000, 30, 233, N'AOC', 'Active'), -- SubSubcategoryID 233 = "AOC"

-- 5. Acer KA272 G0 27" IPS 120Hz
(N'Màn hình Acer KA272 G0 27" IPS 120Hz',
 N'Màn hình 27" IPS, FHD 1920x1080, 120Hz, 1ms, FreeSync, Blue Light Shield, Eye Sense',
 2490000, 12, 2190000, 35, 234, N'Acer', 'Active'); -- SubSubcategoryID 234 = "Acer"

-- Get ProductIDs
DECLARE @ViewSonicID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%ViewSonic%');
DECLARE @ASUSID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%ASUS%');
DECLARE @MSIID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%MSI%');
DECLARE @AOCID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%AOC%');
DECLARE @AcerID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Acer%');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES (5 IMAGES MỖI SẢN PHẨM)
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- ViewSonic XG2409 Images (từ GearVN)
(@ViewSonicID, 'https://product.hstatic.net/200000722513/product/view_xg2409_gearvn_5b5de5b872084814b3fd6276994dac30_1024x1024.jpg', 1, 1), 
(@ViewSonicID, 'https://product.hstatic.net/200000722513/product/gpg-24-mon-xg2409-prdp_l02-1_060a9733fec34303adc2f28ce0532267_1024x1024.png', 0, 2), 
(@ViewSonicID, 'https://product.hstatic.net/200000722513/product/gpg-24-mon-xg2409-prdp_l05_26de2a58da5e4795846ca452c52c6f1d_1024x1024.png', 0, 3), 
(@ViewSonicID, 'https://product.hstatic.net/200000722513/product/gpg-24-mon-xg2409-prdp_lb02_f96116ba286e40f88b15c9a22df3a5b3_1024x1024.png', 0, 4), 
(@ViewSonicID, 'https://product.hstatic.net/200000722513/product/gpg-24-mon-xg2409-prdp_rf02_e523cfec2ca24ed3a38676bdcceeb079_1024x1024.png', 0, 5),

-- ASUS TUF Gaming Images  
(@ASUSID, 'https://product.hstatic.net/200000722513/product/asus_vg27aq5a_gearvn_8a48559a1dad420e9e07e804da103d4a_1024x1024.jpg', 1, 1), (@ASUSID, 'https://product.hstatic.net/200000722513/product/tuf-gaming-vg27aq5a-01_e146294b5b544cb18d2031e994276148_1024x1024.jpg', 0, 2), (@ASUSID, 'https://product.hstatic.net/200000722513/product/tuf-gaming-vg27aq5a-03_d6482b96e9bb4fffb4869c4970169494_1024x1024.jpg', 0, 3), (@ASUSID, 'https://product.hstatic.net/200000722513/product/tuf-gaming-vg27aq5a-04_791b9d9a7fa841628c42b4e99b3d6ace_1024x1024.jpg', 0, 4), (@ASUSID, 'https://product.hstatic.net/200000722513/product/tuf-gaming-vg27aq5a-05_94f81b1cd84948db9d1e6685954240a0_1024x1024.jpg', 0, 5),

-- MSI MAG Images
(@MSIID, 'https://product.hstatic.net/200000722513/product/msi_mag_255xf_gearvn_1c860101b3084c2791b881c9ceff3e90_1024x1024.jpg', 1, 1), (@MSIID, 'https://product.hstatic.net/200000722513/product/1024__1__66284a8adf90433d8ee0ebd7af171a24_1024x1024.png', 0, 2), (@MSIID, 'https://product.hstatic.net/200000722513/product/1024__2__32eb004980474788a6c30768a03b4364_1024x1024.png', 0, 3), (@MSIID, 'https://product.hstatic.net/200000722513/product/1024__3__c4143f9fe13949d1b10bdb3595908195_1024x1024.png', 0, 4), (@MSIID, 'https://product.hstatic.net/200000722513/product/1024__4__fe92024df96743cfa2c4b57a28861993_1024x1024.pngg', 0, 5),

-- AOC 24G4H Images
(@AOCID, 'https://product.hstatic.net/200000722513/product/aoc_24g4h_200hz_gearvn_2e44669d656a4c688fc62379e991572e_1024x1024.jpg', 1, 1), (@AOCID, 'https://product.hstatic.net/200000722513/product/24g4h_b_15a7812507444252a7520d4d5efa9c34_1024x1024.png', 0, 2), (@AOCID, 'https://product.hstatic.net/200000722513/product/24g4h_f_6eebc7d041574a2bbb0abca478f96ffc_1024x1024.png', 0, 3), (@AOCID, 'https://product.hstatic.net/200000722513/product/24g4h_ftl_p_3058c3dea4254fb8ae9c5cd49a4391db_1024x1024.png', 0, 4), (@AOCID, 'https://product.hstatic.net/200000722513/product/24g4h_ftr_ce040583ceb8469bbc8e866b319711f5_1024x1024.png', 0, 5),

-- Acer KA272 Images
(@AcerID, 'https://product.hstatic.net/200000722513/product/acer_ka272_g0_gearvn_d7ccbda688bd4f1f8ab2df0e02a731e4_1024x1024.jpg', 1, 1), (@AcerID, 'https://product.hstatic.net/200000722513/product/man-hinh-van-phong-giai-tri-27-inch-120hz-acer-viet-nam-anh-san-pham-5_307fe0c0460d425c96da09ea9dfed6f8_1024x1024.png', 0, 2), (@AcerID, 'https://product.hstatic.net/200000722513/product/man-hinh-van-phong-giai-tri-27-inch-120hz-acer-viet-nam-anh-san-pham-6_1e1efdb8181c415494027e13663fd8b9_1024x1024.png', 0, 3), (@AcerID, 'https://product.hstatic.net/200000722513/product/man-hinh-van-phong-giai-tri-27-inch-120hz-acer-viet-nam-anh-san-pham-7_78a4f8121762418ebb378293b5966ada_1024x1024.png', 0, 4), (@AcerID, 'https://product.hstatic.net/200000722513/product/man-hinh-van-phong-giai-tri-27-inch-120hz-acer-viet-nam-anh-san-pham-8_cf031a34268a4efc94955dfcf4dc47fa_1024x1024.png', 0, 5);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    -- Kích thước Values - SỬ DỤNG THUỘC TÍNH MỚI
    (@KichThuocID, N'24 inch'), (@KichThuocID, N'27 inch'),
    -- Độ phân giải Values  
    (@DoPhanGiaiID, N'Full HD (1920 x 1080)'), (@DoPhanGiaiID, N'QHD (2560 x 1440)'),
    -- Tần số quét Values
    (@TanSoQuetID, N'120Hz'), (@TanSoQuetID, N'180Hz'), (@TanSoQuetID, N'200Hz'), (@TanSoQuetID, N'210Hz'),
    -- Tấm nền Values
    (@TamNenID, N'SuperClear IPS'), (@TamNenID, N'Fast IPS'), (@TamNenID, N'VA'), (@TamNenID, N'IPS'),
    -- Thời gian phản hồi Values
    (@ThoiGianPhanHoiID, N'1ms'),
    -- Công nghệ đồng bộ Values
    (@CongNgheDongBoID, N'G-Sync Compatible'), (@CongNgheDongBoID, N'FreeSync Premium'), (@CongNgheDongBoID, N'FreeSync'),
    -- Cổng kết nối Values
    (@CongKetNoiID, N'2x HDMI 1.4, 1x DisplayPort'), (@CongKetNoiID, N'HDMI 2.0, DisplayPort 1.4'), 
    (@CongKetNoiID, N'HDMI, DisplayPort'), (@CongKetNoiID, N'HDMI 1.4, VGA')
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
    -- ViewSonic XG2409 mappings - SỬ DỤNG THUỘC TÍNH MỚI
    (@ViewSonicID, @KichThuocID, N'24 inch'), 
    (@ViewSonicID, @DoPhanGiaiID, N'Full HD (1920 x 1080)'), 
    (@ViewSonicID, @TanSoQuetID, N'180Hz'),
    (@ViewSonicID, @TamNenID, N'SuperClear IPS'), 
    (@ViewSonicID, @ThoiGianPhanHoiID, N'1ms'), 
    (@ViewSonicID, @CongNgheDongBoID, N'G-Sync Compatible'), 
    (@ViewSonicID, @CongKetNoiID, N'2x HDMI 1.4, 1x DisplayPort'),
    
    -- ASUS TUF Gaming mappings  
    (@ASUSID, @KichThuocID, N'27 inch'), 
    (@ASUSID, @DoPhanGiaiID, N'QHD (2560 x 1440)'), 
    (@ASUSID, @TanSoQuetID, N'210Hz'),
    (@ASUSID, @TamNenID, N'Fast IPS'), 
    (@ASUSID, @ThoiGianPhanHoiID, N'1ms'), 
    (@ASUSID, @CongNgheDongBoID, N'G-Sync Compatible'), 
    (@ASUSID, @CongKetNoiID, N'HDMI 2.0, DisplayPort 1.4'),
    
    -- MSI MAG mappings
    (@MSIID, @KichThuocID, N'27 inch'), 
    (@MSIID, @DoPhanGiaiID, N'Full HD (1920 x 1080)'), 
    (@MSIID, @TanSoQuetID, N'200Hz'),
    (@MSIID, @TamNenID, N'VA'), 
    (@MSIID, @ThoiGianPhanHoiID, N'1ms'), 
    (@MSIID, @CongNgheDongBoID, N'FreeSync Premium'), 
    (@MSIID, @CongKetNoiID, N'HDMI, DisplayPort'),
    
    -- AOC 24G4H mappings
    (@AOCID, @KichThuocID, N'24 inch'), 
    (@AOCID, @DoPhanGiaiID, N'Full HD (1920 x 1080)'), 
    (@AOCID, @TanSoQuetID, N'200Hz'),
    (@AOCID, @TamNenID, N'Fast IPS'), 
    (@AOCID, @ThoiGianPhanHoiID, N'1ms'), 
    (@AOCID, @CongNgheDongBoID, N'FreeSync Premium'), 
    (@AOCID, @CongKetNoiID, N'HDMI, DisplayPort'),
    
    -- Acer KA272 mappings
    (@AcerID, @KichThuocID, N'27 inch'), 
    (@AcerID, @DoPhanGiaiID, N'Full HD (1920 x 1080)'), 
    (@AcerID, @TanSoQuetID, N'120Hz'),
    (@AcerID, @TamNenID, N'IPS'), 
    (@AcerID, @ThoiGianPhanHoiID, N'1ms'), 
    (@AcerID, @CongNgheDongBoID, N'FreeSync'), 
    (@AcerID, @CongKetNoiID, N'HDMI 1.4, VGA')
  ) AS t(ProductID, AttributeID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName AND av.AttributeID = t.AttributeID
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;

-- ========================================================
-- HOÀN THÀNH: MÀN HÌNH BÁN CHẠY ĐÃ ĐƯỢC THÊM
-- ========================================================

GO 