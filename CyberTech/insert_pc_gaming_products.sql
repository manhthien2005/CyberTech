-- ========================================================
-- FILE: INSERT PRODUCTS - PC GAMING BÁN CHẠY (FIXED)
-- Mô tả: Thêm 5 sản phẩm PC Gaming vào database CyberTech từ GearVN
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY CHO PC GAMING
-- ========================================================

-- Tạo attributes cho PC Gaming (kiểm tra tồn tại trước)
INSERT INTO ProductAttribute (AttributeName, AttributeType) 
SELECT * FROM (VALUES
  (N'CPU', 'Text'), (N'VGA', 'Text'), (N'RAM', 'Text'),
  (N'Mainboard', 'Text'), (N'Ổ cứng', 'Text'), (N'PSU', 'Text'), (N'Case', 'Text')
) AS NewAttrs(AttributeName, AttributeType)
WHERE NOT EXISTS (
  SELECT 1 FROM ProductAttribute pa 
  WHERE pa.AttributeName = NewAttrs.AttributeName
);

-- CategoryID cho PC Gaming = 3
DECLARE @PCGamingCategoryID INT = 3;

INSERT INTO CategoryAttributes (CategoryID, AttributeName)
SELECT * FROM (VALUES
  (@PCGamingCategoryID, N'CPU'), (@PCGamingCategoryID, N'VGA'), (@PCGamingCategoryID, N'RAM'), (@PCGamingCategoryID, N'Mainboard'), 
  (@PCGamingCategoryID, N'Ổ cứng'), (@PCGamingCategoryID, N'PSU'), (@PCGamingCategoryID, N'Case')
) AS NewCatAttrs(CategoryID, AttributeName)
WHERE NOT EXISTS (
  SELECT 1 FROM CategoryAttributes ca 
  WHERE ca.CategoryID = NewCatAttrs.CategoryID AND ca.AttributeName = NewCatAttrs.AttributeName
);

-- Variables cho AttributeID
DECLARE @CPUID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'CPU');
DECLARE @VGAID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'VGA');
DECLARE @RAMID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'RAM');
DECLARE @MainboardID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Mainboard');
DECLARE @OcungID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Ổ cứng');
DECLARE @PSUID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'PSU');
DECLARE @CaseID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Case');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM PC GAMING BÁN CHẠY
-- ========================================================

-- FIX: Sử dụng SubSubcategoryID hợp lệ - "PC Core I5" thay vì 47
DECLARE @ValidSubSubcategoryID INT = (
  SELECT TOP 1 ssc.SubSubcategoryID 
  FROM SubSubcategory ssc 
  INNER JOIN Subcategory sc ON ssc.SubcategoryID = sc.SubcategoryID 
  WHERE sc.CategoryID = 3 AND ssc.Name = N'PC Core I5'
);

-- Fallback nếu không tìm thấy, lấy SubSubcategoryID đầu tiên của CategoryID = 3
IF @ValidSubSubcategoryID IS NULL
BEGIN
  SET @ValidSubSubcategoryID = (
    SELECT TOP 1 ssc.SubSubcategoryID 
    FROM SubSubcategory ssc 
    INNER JOIN Subcategory sc ON ssc.SubcategoryID = sc.SubcategoryID 
    WHERE sc.CategoryID = 3
  );
END

-- Insert all products at once với SubSubcategoryID hợp lệ
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
VALUES 
-- 1. PC GVN Intel i5-12400F/ VGA RTX 4060 (từ GearVN: 20.220k → 17.890k, -12%)
(N'PC GVN Intel i5-12400F/ VGA RTX 4060',
 N'PC Gaming Intel Core i5-12400F, RTX 4060 8GB, RAM 16GB DDR4, SSD 500GB NVMe, B760M Gaming WiFi, PSU 550W Bronze',
 20220000, 12, 17890000, 15, @ValidSubSubcategoryID, N'GEARVN', 'Active'),

-- 2. PC GVN Intel i5/ VGA RTX 3060 (ước tính: 18.990k → 16.490k, -13%)
(N'PC GVN Intel i5/ VGA RTX 3060',
 N'PC Gaming Intel Core i5-12400F, RTX 3060 12GB, RAM 16GB DDR4, SSD 500GB NVMe, B760M Gaming WiFi, PSU 550W Bronze',
 18990000, 13, 16490000, 20, @ValidSubSubcategoryID, N'GEARVN', 'Active'),

-- 3. PC GVN Intel i5K/ VGA RTX 4060Ti (ước tính: 25.990k → 22.990k, -12%)
(N'PC GVN Intel i5K/ VGA RTX 4060Ti',
 N'PC Gaming Intel Core i5-13600KF, RTX 4060Ti 16GB, RAM 16GB DDR5, SSD 1TB NVMe, Z790 Gaming WiFi, PSU 650W Bronze',
 25990000, 12, 22990000, 12, @ValidSubSubcategoryID, N'GEARVN', 'Active'),

-- 4. PC GVN x MSI Project Zero White (ước tính: 35.990k → 32.990k, -8%)
(N'PC GVN x MSI Project Zero White',
 N'PC Gaming cao cấp Intel Core i7-13700KF, RTX 4070 12GB, RAM 32GB DDR5, SSD 1TB NVMe, MSI MAG B760M WiFi, Case MSI Project Zero White',
 35990000, 8, 32990000, 8, @ValidSubSubcategoryID, N'GEARVN', 'Active'),

-- 5. PC GVN Intel i5/ VGA RTX 4060 RGB (ước tính: 19.990k → 18.490k, -8%)
(N'PC GVN Intel i5/ VGA RTX 4060 RGB',
 N'PC Gaming Intel Core i5-12400F, RTX 4060 8GB, RAM 16GB DDR4, SSD 500GB NVMe, B760M Gaming WiFi, PSU 550W Bronze, Case Gaming RGB',
 19990000, 8, 18490000, 18, @ValidSubSubcategoryID, N'GEARVN', 'Active');

-- FIX: Lấy ProductIDs với logic đơn giản hơn
DECLARE @PC4060ID INT = (SELECT ProductID FROM Products WHERE Name = N'PC GVN Intel i5-12400F/ VGA RTX 4060' AND Brand = N'GEARVN');
DECLARE @PC3060ID INT = (SELECT ProductID FROM Products WHERE Name = N'PC GVN Intel i5/ VGA RTX 3060' AND Brand = N'GEARVN');
DECLARE @PC4060TiID INT = (SELECT ProductID FROM Products WHERE Name = N'PC GVN Intel i5K/ VGA RTX 4060Ti' AND Brand = N'GEARVN');
DECLARE @PCProjectZeroID INT = (SELECT ProductID FROM Products WHERE Name = N'PC GVN x MSI Project Zero White' AND Brand = N'GEARVN');
DECLARE @PC4060_2ID INT = (SELECT ProductID FROM Products WHERE Name = N'PC GVN Intel i5/ VGA RTX 4060 RGB' AND Brand = N'GEARVN');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES (5 IMAGES MỖI SẢN PHẨM)
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- PC GVN Intel i5-12400F/ VGA RTX 4060 Images
(@PC4060ID, 'https://product.hstatic.net/200000722513/product/pc_case_xigmatek_-_26_bc4f94f5e6484520abc738e769053df4_1024x1024.png', 1, 1), 
(@PC4060ID, 'https://product.hstatic.net/200000722513/product/post-01_08d1bb4db5d24693b7a4f497c449631c_1024x1024.jpg', 0, 2), 
(@PC4060ID, 'https://product.hstatic.net/200000722513/product/post-05_77315f3818334f35b7827a88733e8890_1024x1024.jpg', 0, 3), 
(@PC4060ID, 'https://product.hstatic.net/200000722513/product/post-06_08db1d849439435a8cd8189d23cd7d6d_1024x1024.jpg', 0, 4), 
(@PC4060ID, 'https://product.hstatic.net/200000722513/product/post-08_40cc3fda2e62424eb58d2d753d65bd13_1024x1024.jpg', 0, 5),

-- PC GVN Intel i5/ VGA RTX 3060 Images
(@PC3060ID, 'https://product.hstatic.net/200000722513/product/pc_case_xigmatek_-_26_50758d07dff4461ebd00809e2699e2e0_1024x1024.png', 1, 1), 
(@PC3060ID, 'https://product.hstatic.net/200000722513/product/post-01_bd8978f790a6418fa200aa289a4d3cc6_1024x1024.jpg', 0, 2), 
(@PC3060ID, 'https://product.hstatic.net/200000722513/product/post-03_01d21271548447019619a86b977f5c6d_1024x1024.jpg', 0, 3), 
(@PC3060ID, 'https://product.hstatic.net/200000722513/product/post-08_d63ed27b4741425ca8894907b4fc8fba_1024x1024.jpg', 0, 4), 
(@PC3060ID, 'https://product.hstatic.net/200000722513/product/post-04_fa0162430dc94bc8a5468986546eaef7_1024x1024.jpg', 0, 5),

-- PC GVN Intel i5K/ VGA RTX 4060Ti Images
(@PC4060TiID, 'https://product.hstatic.net/200000722513/product/4060ti_dfb001bdb8cd410d9a85e38c5a4c568b_1024x1024.png', 1, 1), 
(@PC4060TiID, 'https://product.hstatic.net/200000722513/product/gearvn-pc-gvn-intel-i5k-4060ti-2_0f66fa9802004a288e281e59825c5be9_1024x1024.jpg', 0, 2), 
(@PC4060TiID, 'https://product.hstatic.net/200000722513/product/post-01_ebcf4dc58c39435a9605df5f55da38b2_1024x1024.jpg', 0, 3), 
(@PC4060TiID, 'https://product.hstatic.net/200000722513/product/post-03_8f0a5d424d164fab8167b38652cb1f5a_1024x1024.jpg', 0, 4), 
(@PC4060TiID, 'https://product.hstatic.net/200000722513/product/post-04_05b4ae8480f1452c8d78889f25134541_1024x1024.jpg', 0, 5),

-- PC GVN x MSI Project Zero White Images
(@PCProjectZeroID, 'https://product.hstatic.net/200000722513/product/artboard_2_e6aeb76ab97048a0b9514f5e7da18853_1024x1024.png', 1, 1), 
(@PCProjectZeroID, 'https://product.hstatic.net/200000722513/product/pc_khach_msi_project_zero-01345_fe3b7c79989b4ad2ba4819b4f8b53bcc_1024x1024.jpg', 0, 2), 
(@PCProjectZeroID, 'https://product.hstatic.net/200000722513/product/pc_khach_msi_project_zero-01305_51063cbd7a0a4f789b2d8886fdcdda15_1024x1024.jpg', 0, 3), 
(@PCProjectZeroID, 'https://product.hstatic.net/200000722513/product/zero_msi_-_4_bdb11a08556a42f6a6a6d3e98c691aba_1024x1024.png', 0, 4), 
(@PCProjectZeroID, 'https://product.hstatic.net/200000722513/product/pc_khach_msi_project_zero-01306_95343762977c4f7296b7185429aa37dc_1024x1024.jpg', 0, 5),

-- PC GVN Intel i5/ VGA RTX 4060 RGB Images
(@PC4060_2ID, 'https://product.hstatic.net/200000722513/product/4060_ddae7a383d2c4f56b9116644fe20906f_1024x1024.png', 1, 1), 
(@PC4060_2ID, 'https://product.hstatic.net/200000722513/product/post-01_32646f45f48848faaf5f2ba69437b262_1024x1024.jpg', 0, 2), 
(@PC4060_2ID, 'https://product.hstatic.net/200000722513/product/post-02_f5f96ebabc704e07bb6be80a3ba5cfbe_1024x1024.jpg', 0, 3), 
(@PC4060_2ID, 'https://product.hstatic.net/200000722513/product/post-15_fce5be99437647cf89fbae817686c0cb_1024x1024.jpg', 0, 4), 
(@PC4060_2ID, 'https://product.hstatic.net/200000722513/product/post-14_46dfd1c0b85443d2b2cf04cd0a2a9b05_1024x1024.jpg', 0, 5);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    -- CPU Values
    (@CPUID, N'Intel Core i5-12400F'), (@CPUID, N'Intel Core i5-13600KF'), (@CPUID, N'Intel Core i7-13700KF'),
    -- VGA Values  
    (@VGAID, N'RTX 4060 8GB'), (@VGAID, N'RTX 3060 12GB'), (@VGAID, N'RTX 4060Ti 16GB'), (@VGAID, N'RTX 4070 12GB'),
    -- RAM Values
    (@RAMID, N'16GB DDR4'), (@RAMID, N'16GB DDR5'), (@RAMID, N'32GB DDR5'),
    -- Mainboard Values
    (@MainboardID, N'GIGABYTE B760M Gaming WiFi'), (@MainboardID, N'Z790 Gaming WiFi'), (@MainboardID, N'MSI MAG B760M WiFi'),
    -- Ổ cứng Values
    (@OcungID, N'500GB SSD NVMe'), (@OcungID, N'1TB SSD NVMe'),
    -- PSU Values
    (@PSUID, N'FSP 550W Bronze'), (@PSUID, N'FSP 650W Bronze'),
    -- Case Values
    (@CaseID, N'Xigmatek QUANTUM 3GF'), (@CaseID, N'MSI Project Zero White'), (@CaseID, N'Gaming RGB Case')
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
    -- PC GVN Intel i5-12400F/ VGA RTX 4060 mappings
    (@PC4060ID, @CPUID, N'Intel Core i5-12400F'), 
    (@PC4060ID, @VGAID, N'RTX 4060 8GB'), 
    (@PC4060ID, @RAMID, N'16GB DDR4'),
    (@PC4060ID, @MainboardID, N'GIGABYTE B760M Gaming WiFi'), 
    (@PC4060ID, @OcungID, N'500GB SSD NVMe'), 
    (@PC4060ID, @PSUID, N'FSP 550W Bronze'), 
    (@PC4060ID, @CaseID, N'Xigmatek QUANTUM 3GF'),
    
    -- PC GVN Intel i5/ VGA RTX 3060 mappings  
    (@PC3060ID, @CPUID, N'Intel Core i5-12400F'), 
    (@PC3060ID, @VGAID, N'RTX 3060 12GB'), 
    (@PC3060ID, @RAMID, N'16GB DDR4'),
    (@PC3060ID, @MainboardID, N'GIGABYTE B760M Gaming WiFi'), 
    (@PC3060ID, @OcungID, N'500GB SSD NVMe'), 
    (@PC3060ID, @PSUID, N'FSP 550W Bronze'), 
    (@PC3060ID, @CaseID, N'Xigmatek QUANTUM 3GF'),
    
    -- PC GVN Intel i5K/ VGA RTX 4060Ti mappings
    (@PC4060TiID, @CPUID, N'Intel Core i5-13600KF'), 
    (@PC4060TiID, @VGAID, N'RTX 4060Ti 16GB'), 
    (@PC4060TiID, @RAMID, N'16GB DDR5'),
    (@PC4060TiID, @MainboardID, N'Z790 Gaming WiFi'), 
    (@PC4060TiID, @OcungID, N'1TB SSD NVMe'), 
    (@PC4060TiID, @PSUID, N'FSP 650W Bronze'), 
    (@PC4060TiID, @CaseID, N'Gaming RGB Case'),
    
    -- PC GVN x MSI Project Zero White mappings
    (@PCProjectZeroID, @CPUID, N'Intel Core i7-13700KF'), 
    (@PCProjectZeroID, @VGAID, N'RTX 4070 12GB'), 
    (@PCProjectZeroID, @RAMID, N'32GB DDR5'),
    (@PCProjectZeroID, @MainboardID, N'MSI MAG B760M WiFi'), 
    (@PCProjectZeroID, @OcungID, N'1TB SSD NVMe'), 
    (@PCProjectZeroID, @PSUID, N'FSP 650W Bronze'), 
    (@PCProjectZeroID, @CaseID, N'MSI Project Zero White'),
    
    -- PC GVN Intel i5/ VGA RTX 4060 RGB mappings
    (@PC4060_2ID, @CPUID, N'Intel Core i5-12400F'), 
    (@PC4060_2ID, @VGAID, N'RTX 4060 8GB'), 
    (@PC4060_2ID, @RAMID, N'16GB DDR4'),
    (@PC4060_2ID, @MainboardID, N'GIGABYTE B760M Gaming WiFi'), 
    (@PC4060_2ID, @OcungID, N'500GB SSD NVMe'), 
    (@PC4060_2ID, @PSUID, N'FSP 550W Bronze'), 
    (@PC4060_2ID, @CaseID, N'Gaming RGB Case')
  ) AS t(ProductID, AttributeID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName AND av.AttributeID = t.AttributeID
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;

-- ========================================================
-- HOÀN THÀNH: PC GAMING BÁN CHẠY ĐÃ ĐƯỢC THÊM
-- ========================================================

PRINT 'Successfully inserted 5 PC Gaming products with attributes and images.';
GO 