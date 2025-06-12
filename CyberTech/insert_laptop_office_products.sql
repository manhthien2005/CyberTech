-- ========================================================
-- FILE: INSERT PRODUCTS - LAPTOP VĂN PHÒNG BÁN CHẠY (OPTIMIZED)
-- Mô tả: Thêm 5 sản phẩm laptop văn phòng vào database CyberTech từ GearVN
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY CHO LAPTOP VĂN PHÒNG
-- ========================================================

-- Tạo attributes cho laptop văn phòng (kiểm tra tồn tại trước)
INSERT INTO ProductAttribute (AttributeName, AttributeType) 
SELECT * FROM (VALUES
  (N'CPU', 'Text'), (N'RAM', 'Text'), (N'Ổ cứng', 'Text'),
  (N'Card đồ họa', 'Text'), (N'Màn hình', 'Text'), (N'Hệ điều hành', 'Text'), (N'Trọng lượng', 'Text')
) AS NewAttrs(AttributeName, AttributeType)
WHERE NOT EXISTS (
  SELECT 1 FROM ProductAttribute pa 
  WHERE pa.AttributeName = NewAttrs.AttributeName
);

-- CategoryID cho laptop văn phòng = 1
DECLARE @LaptopOfficeCategoryID INT = 1;

INSERT INTO CategoryAttributes (CategoryID, AttributeName)
SELECT * FROM (VALUES
  (@LaptopOfficeCategoryID, N'CPU'), (@LaptopOfficeCategoryID, N'RAM'), (@LaptopOfficeCategoryID, N'Ổ cứng'), (@LaptopOfficeCategoryID, N'Card đồ họa'), 
  (@LaptopOfficeCategoryID, N'Màn hình'), (@LaptopOfficeCategoryID, N'Hệ điều hành'), (@LaptopOfficeCategoryID, N'Trọng lượng')
) AS NewCatAttrs(CategoryID, AttributeName)
WHERE NOT EXISTS (
  SELECT 1 FROM CategoryAttributes ca 
  WHERE ca.CategoryID = NewCatAttrs.CategoryID AND ca.AttributeName = NewCatAttrs.AttributeName
);

-- Variables cho AttributeID
DECLARE @CPUID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'CPU');
DECLARE @RAMID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'RAM');
DECLARE @OcungID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Ổ cứng');
DECLARE @CardDoHoaID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Card đồ họa');
DECLARE @ManHinhID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Màn hình');
DECLARE @HeDieuHanhID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Hệ điều hành');
DECLARE @TrongLuongID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Trọng lượng');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM LAPTOP VĂN PHÒNG BÁN CHẠY
-- ========================================================

DECLARE @ProductIDs TABLE (ProductID INT, ProductName NVARCHAR(255));

-- Insert all products at once
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
OUTPUT INSERTED.ProductID, INSERTED.Name INTO @ProductIDs
VALUES 
-- 1. Lenovo IdeaPad Slim 3 15IRH10 (từ GearVN: 17.490k → 16.490k, -6%)
(N'Laptop Lenovo IdeaPad Slim 3 15IRH10 83K1000GVN',
 N'Laptop văn phòng 15.3" WUXGA IPS, Intel Core i5-13420H, 24GB DDR5, 512GB SSD NVMe, Intel UHD Graphics, Windows 11, 1.62kg',
 17490000, 6, 16490000, 25, 28, N'LENOVO', 'Active'), -- SubSubcategoryID 28 = "Thinkbook Sery"

-- 2. Acer Swift Edge SFA16-41-R3L6 (ước tính: 22.990k → 19.990k, -13%)
(N'Laptop Acer Swift Edge SFA16-41-R3L6',
 N'Laptop mỏng nhẹ 16" WUXGA, AMD Ryzen 5 7535U, 16GB LPDDR5, 512GB SSD NVMe, AMD Radeon Graphics, Windows 11, 1.23kg',
 22990000, 13, 19990000, 20, 18, N'ACER', 'Active'), -- SubSubcategoryID 18 = "Swift Sery"

-- 3. ASUS ExpertBook P1 P1403CVA-I5SE16-50W (ước tính: 18.990k → 16.990k, -11%)
(N'Laptop ASUS ExpertBook P1 P1403CVA-I5SE16-50W',
 N'Laptop doanh nghiệp 14" FHD, Intel Core i5-1335U, 16GB DDR4, 512GB SSD NVMe, Intel Iris Xe Graphics, Windows 11 Pro, 1.45kg',
 18990000, 11, 16990000, 30, 13, N'ASUS', 'Active'), -- SubSubcategoryID 13 = "Vivobook Sery"

-- 4. MSI Modern 14 F13MG-027VN (ước tính: 15.990k → 14.490k, -9%)
(N'Laptop MSI Modern 14 F13MG-027VN',
 N'Laptop văn phòng 14" FHD IPS, Intel Core i5-1335U, 16GB DDR4, 512GB SSD NVMe, NVIDIA GeForce MX450, Windows 11, 1.4kg',
 15990000, 9, 14490000, 28, 20, N'MSI', 'Active'), -- SubSubcategoryID 20 = "Modern Sery"

-- 5. ASUS VivoBook S 16 OLED S5606MA-MX051W (ước tính: 24.990k → 22.990k, -8%)
(N'Laptop ASUS VivoBook S 16 OLED S5606MA-MX051W',
 N'Laptop OLED cao cấp 16" 3.2K OLED, Intel Core Ultra 5 125H, 16GB LPDDR5X, 512GB SSD NVMe, Intel Arc Graphics, Windows 11, 1.5kg',
 24990000, 8, 22990000, 15, 13, N'ASUS', 'Active'); -- SubSubcategoryID 13 = "Vivobook Sery"

-- Get ProductIDs
DECLARE @LenovoID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Lenovo%');
DECLARE @AcerID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Acer%');
DECLARE @AsusExpertID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%ExpertBook%');
DECLARE @MSIID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%MSI%');
DECLARE @AsusVivoID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%VivoBook%');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES (5 IMAGES MỖI SẢN PHẨM)
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- Lenovo IdeaPad Slim 3 Images (demo)
(@LenovoID, 'https://product.hstatic.net/200000722513/product/ideapad_slim_3_15irh10_ct2_06_d927ac853d7241138eb02d35a9fb46e2_1024x1024.png', 1, 1), 
(@LenovoID, 'https://product.hstatic.net/200000722513/product/ideapad_slim_3_15irh10_ct1_02_1e9d602aafd24c5a8a62a4c5d1488c43_1024x1024.png', 0, 2), 
(@LenovoID, 'https://product.hstatic.net/200000722513/product/ideapad_slim_3_15irh10_ct2_05_8192d27eacc34d5587dcfbcff60159f7_1024x1024.png', 0, 3), 
(@LenovoID, 'https://product.hstatic.net/200000722513/product/ideapad_slim_3_15irh10_ct2_11_7a9c4c6f723549a594d17e1568ce0e0b_1024x1024.png', 0, 4), 
(@LenovoID, 'https://product.hstatic.net/200000722513/product/ideapad_slim_3_15irh10_ct2_10_157204c67ed64b1cb7a53afbe12b4164_1024x1024.png', 0, 5),

-- Acer Swift Edge Images (demo)
(@AcerID, 'https://product.hstatic.net/200000722513/product/r3l6_1b5292409d0c4f15ab545f6f766694e3_9a765ea64cf74899942ae8995814b488_1024x1024.png', 1, 1), 
(@AcerID, 'https://product.hstatic.net/200000722513/product/go-flax-white-snow-blue-03.tif-custom_f0afbfa3cb8b42978a61a3bef63c7b4e_937d4e00b0804d35b2e209e13ee481e3_1024x1024.png', 0, 2), 
(@AcerID, 'https://product.hstatic.net/200000722513/product/go-flax-white-snow-blue-02.tif-custom_cefdf068f4f348f6bfc77b6d12786704_444247744b0045ac9d98e70a470c1e8e_1024x1024.png', 0, 3), 
(@AcerID, 'https://product.hstatic.net/200000722513/product/it-flax-white-snow-blue-05.tif-custom_3265a6bc3b1c44e9a0d403a32d0e84c6_c8ce6489de62450fb149c55a2b07f6b3_1024x1024.png', 0, 4), 
(@AcerID, 'https://product.hstatic.net/200000722513/product/it-flax-white-snow-blue-09.tif-custom_8df101bec9b94d29bc9a9159c17e1aa3_c94235ad07bf4b6c8197a6fca0d35e24_1024x1024.png', 0, 5),

-- ASUS ExpertBook Images (demo)
(@AsusExpertID, 'https://product.hstatic.net/200000722513/product/sus-expertbook-p1-p1403cva-i5se16-50w_cba58ce14b05424d8221224600b680f4_359e569240a14ee9badb38ed3dcacd27_1024x1024.png', 1, 1), 
(@AsusExpertID, 'https://product.hstatic.net/200000722513/product/expertbook-p1-p1403cva-i5se16-50w__8__f9120f92bbcf40409391d8b907b7c630_4a70cd341c0b41698042f819fe91b029_1024x1024.png', 0, 2), 
(@AsusExpertID, 'https://product.hstatic.net/200000722513/product/expertbook-p1-p1403cva-i5se16-50w__1__addb2c8932194026bab6f5d03898f1df_0f9ad9a880b943e68a367ae83e21ae7a_1024x1024.png', 0, 3), 
(@AsusExpertID, 'https://product.hstatic.net/200000722513/product/expertbook-p1-p1403cva-i5se16-50w__3__e7bf0aafc98f43c6a824af19da7d4168_3019c1dc6c664ad9bf1ef17b6046b636_1024x1024.png', 0, 4), 
(@AsusExpertID, 'https://product.hstatic.net/200000722513/product/expertbook-p1-p1403cva-i5se16-50w__5__b545d7ea621d4177aaafc2ac6b27dc64_9f480fa4675c43f9aa9f18bd0fa464bd_1024x1024.png', 0, 5),

-- MSI Modern 14 Images (demo)
(@MSIID, 'https://product.hstatic.net/200000722513/product/1024_5b3ad2cff4444235bdb9897806ebbc40_1024x1024.png', 1, 1), 
(@MSIID, 'https://product.hstatic.net/200000722513/product/_o9xi_vck1coivii2l-iwzxhlznxfyg8_8a1675d8ca6345788d95e44d09384863_1024x1024.png', 0, 2), 
(@MSIID, 'https://product.hstatic.net/200000722513/product/1024__1__e3dc02eec255463cbbf716b18f38c7be_1024x1024.png', 0, 3), 
(@MSIID, 'https://product.hstatic.net/200000722513/product/laptop-msi-modern-14-f13mg-027vn__1__b739952ed15e4f71af91d66294f9886b_1024x1024.png', 0, 4), 
(@MSIID, 'https://product.hstatic.net/200000722513/product/laptop-msi-modern-14-f13mg-027vn_a437eceab7ae4b14aa74aeca51858208_1024x1024.png', 0, 5),

-- ASUS VivoBook S 16 OLED Images (demo)
(@AsusVivoID, 'https://product.hstatic.net/200000722513/product/s-vivobook-s-16-oled-s5606ma-mx051w_0_075da8498daf4093b32f27312f486e35_22c77e392c85463e9afcd214f581101f_1024x1024.png', 1, 1), 
(@AsusVivoID, 'https://product.hstatic.net/200000722513/product/s-vivobook-s-16-oled-s5606ma-mx051w_1_b0f568af3bfe48c399c99a9ae7e3d0f8_8ea43a50f21a4d4290752931ecf1b7b5_1024x1024.png', 0, 2), 
(@AsusVivoID, 'https://product.hstatic.net/200000722513/product/s-vivobook-s-16-oled-s5606ma-mx051w_2_6fab4271f27c45e5bd24a9f2c6c069ed_61cf80fbfa634ea3b1787617cecf1735_1024x1024.png', 0, 3), 
(@AsusVivoID, 'https://product.hstatic.net/200000722513/product/s-vivobook-s-16-oled-s5606ma-mx051w_3_ffff3e385f1444bba90259fe7ec3c967_d393df93b7c9485e8285e055d68f705a_1024x1024.png', 0, 4), 
(@AsusVivoID, 'https://product.hstatic.net/200000722513/product/s-vivobook-s-16-oled-s5606ma-mx051w_4_d2a0d6bc46b84335a4579f57fa66ea08_aac0ac9a89f84ba68e76fe10dc5b044f_1024x1024.png', 0, 5);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    -- CPU Values
    (@CPUID, N'Intel Core i5-13420H'), (@CPUID, N'AMD Ryzen 5 7535U'), (@CPUID, N'Intel Core i5-1335U'), (@CPUID, N'Intel Core Ultra 5 125H'),
    -- RAM Values  
    (@RAMID, N'24GB DDR5'), (@RAMID, N'16GB LPDDR5'), (@RAMID, N'16GB DDR4'), (@RAMID, N'16GB LPDDR5X'),
    -- Ổ cứng Values
    (@OcungID, N'512GB SSD NVMe'),
    -- Card đồ họa Values
    (@CardDoHoaID, N'Intel UHD Graphics'), (@CardDoHoaID, N'AMD Radeon Graphics'), (@CardDoHoaID, N'Intel Iris Xe Graphics'), (@CardDoHoaID, N'NVIDIA GeForce MX450'), (@CardDoHoaID, N'Intel Arc Graphics'),
    -- Màn hình Values
    (@ManHinhID, N'15.3" WUXGA IPS'), (@ManHinhID, N'16" WUXGA'), (@ManHinhID, N'14" FHD'), (@ManHinhID, N'14" FHD IPS'), (@ManHinhID, N'16" 3.2K OLED'),
    -- Hệ điều hành Values
    (@HeDieuHanhID, N'Windows 11 Home'), (@HeDieuHanhID, N'Windows 11 Pro'),
    -- Trọng lượng Values
    (@TrongLuongID, N'1.62kg'), (@TrongLuongID, N'1.23kg'), (@TrongLuongID, N'1.45kg'), (@TrongLuongID, N'1.4kg'), (@TrongLuongID, N'1.5kg')
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
    -- Lenovo IdeaPad Slim 3 mappings
    (@LenovoID, @CPUID, N'Intel Core i5-13420H'), 
    (@LenovoID, @RAMID, N'24GB DDR5'), 
    (@LenovoID, @OcungID, N'512GB SSD NVMe'),
    (@LenovoID, @CardDoHoaID, N'Intel UHD Graphics'), 
    (@LenovoID, @ManHinhID, N'15.3" WUXGA IPS'), 
    (@LenovoID, @HeDieuHanhID, N'Windows 11 Home'), 
    (@LenovoID, @TrongLuongID, N'1.62kg'),
    
    -- Acer Swift Edge mappings  
    (@AcerID, @CPUID, N'AMD Ryzen 5 7535U'), 
    (@AcerID, @RAMID, N'16GB LPDDR5'), 
    (@AcerID, @OcungID, N'512GB SSD NVMe'),
    (@AcerID, @CardDoHoaID, N'AMD Radeon Graphics'), 
    (@AcerID, @ManHinhID, N'16" WUXGA'), 
    (@AcerID, @HeDieuHanhID, N'Windows 11 Home'), 
    (@AcerID, @TrongLuongID, N'1.23kg'),
    
    -- ASUS ExpertBook mappings
    (@AsusExpertID, @CPUID, N'Intel Core i5-1335U'), 
    (@AsusExpertID, @RAMID, N'16GB DDR4'), 
    (@AsusExpertID, @OcungID, N'512GB SSD NVMe'),
    (@AsusExpertID, @CardDoHoaID, N'Intel Iris Xe Graphics'), 
    (@AsusExpertID, @ManHinhID, N'14" FHD'), 
    (@AsusExpertID, @HeDieuHanhID, N'Windows 11 Pro'), 
    (@AsusExpertID, @TrongLuongID, N'1.45kg'),
    
    -- MSI Modern 14 mappings
    (@MSIID, @CPUID, N'Intel Core i5-1335U'), 
    (@MSIID, @RAMID, N'16GB DDR4'), 
    (@MSIID, @OcungID, N'512GB SSD NVMe'),
    (@MSIID, @CardDoHoaID, N'NVIDIA GeForce MX450'), 
    (@MSIID, @ManHinhID, N'14" FHD IPS'), 
    (@MSIID, @HeDieuHanhID, N'Windows 11 Home'), 
    (@MSIID, @TrongLuongID, N'1.4kg'),
    
    -- ASUS VivoBook S 16 OLED mappings
    (@AsusVivoID, @CPUID, N'Intel Core Ultra 5 125H'), 
    (@AsusVivoID, @RAMID, N'16GB LPDDR5X'), 
    (@AsusVivoID, @OcungID, N'512GB SSD NVMe'),
    (@AsusVivoID, @CardDoHoaID, N'Intel Arc Graphics'), 
    (@AsusVivoID, @ManHinhID, N'16" 3.2K OLED'), 
    (@AsusVivoID, @HeDieuHanhID, N'Windows 11 Home'), 
    (@AsusVivoID, @TrongLuongID, N'1.5kg')
  ) AS t(ProductID, AttributeID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName AND av.AttributeID = t.AttributeID
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;

-- ========================================================
-- HOÀN THÀNH: LAPTOP VĂN PHÒNG BÁN CHẠY ĐÃ ĐƯỢC THÊM
-- ========================================================

GO 