-- ========================================================
-- FILE: INSERT PRODUCTS - LAPTOP GAMING BÁN CHẠY (OPTIMIZED)
-- Mô tả: Thêm 5 sản phẩm laptop gaming vào database CyberTech
-- ========================================================

USE cybertech;
GO

-- ========================================================
-- SETUP: TẠO THUỘC TÍNH & CATEGORY CHO LAPTOP GAMING
-- ========================================================

-- Tạo attributes và category mappings cho laptop gaming (kiểm tra tồn tại trước)
INSERT INTO ProductAttribute (AttributeName, AttributeType) 
SELECT * FROM (VALUES
  (N'CPU', 'Text'), (N'Card Đồ Họa', 'Text'), (N'RAM', 'Text'),
  (N'Ổ Cứng', 'Text'), (N'Màn Hình', 'Text'), (N'Hệ Điều Hành', 'Text'), (N'Tần Số Quét', 'Text')
) AS NewAttrs(AttributeName, AttributeType)
WHERE NOT EXISTS (
  SELECT 1 FROM ProductAttribute pa 
  WHERE pa.AttributeName = NewAttrs.AttributeName
);

INSERT INTO CategoryAttributes (CategoryID, AttributeName)
SELECT * FROM (VALUES
  (2, N'CPU'), (2, N'Card Đồ Họa'), (2, N'RAM'), (2, N'Ổ Cứng'), 
  (2, N'Màn Hình'), (2, N'Hệ Điều Hành'), (2, N'Tần Số Quét')
) AS NewCatAttrs(CategoryID, AttributeName)
WHERE NOT EXISTS (
  SELECT 1 FROM CategoryAttributes ca 
  WHERE ca.CategoryID = NewCatAttrs.CategoryID AND ca.AttributeName = NewCatAttrs.AttributeName
);

-- Variables cho AttributeID (sử dụng TOP 1 để tránh lỗi)
DECLARE @CPUID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'CPU');
DECLARE @CardDoHoaID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Card Đồ Họa');
DECLARE @RAMID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'RAM');
DECLARE @OCungID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Ổ Cứng');
DECLARE @ManHinhID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Màn Hình');
DECLARE @HeDieuHanhID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Hệ Điều Hành');
DECLARE @TanSoQuetID INT = (SELECT TOP 1 AttributeID FROM ProductAttribute WHERE AttributeName = N'Tần Số Quét');

-- ========================================================
-- BATCH INSERT: 5 SẢN PHẨM LAPTOP GAMING
-- ========================================================

DECLARE @ProductIDs TABLE (ProductID INT, ProductName NVARCHAR(255));

-- Insert all products at once
INSERT INTO Products (Name, Description, Price, SalePercentage, SalePrice, Stock, SubSubcategoryID, Brand, Status)
OUTPUT INSERTED.ProductID, INSERTED.Name INTO @ProductIDs
VALUES 
-- 1. Acer Predator Helios Neo
(N'Laptop gaming Acer Predator Helios Neo 16 PHN16 72 78L4',
 N'Intel Core i7-14700HX (20 lõi/28 luồng), RTX 4050 6GB, 16GB DDR5 5600MHz, 1TB PCIe NVMe SSD, 16" WQXGA 2560x1600 240Hz, Nvidia Advanced Optimus',
 38990000, 10, 34990000, 30, 51, N'Acer', 'Active'), -- SubSubcategoryID 51 = "Predator Series"

-- 2. Gigabyte G5 MF
(N'Laptop gaming Gigabyte G5 MF F2VN333SH',
 N'Intel Core i7-13620H, RTX 4050 6GB, 16GB DDR4 3200MHz, 512GB PCIe NVMe SSD, 15.6" FHD 144Hz, backlit keyboard',
 25990000, 15, 22090000, 25, 42, N'Gigabyte', 'Active'), -- SubSubcategoryID 42 = "GIGABYTE / AORUS" 

-- 3. Acer Nitro Lite 16 (thay thế Nitro V)
(N'Laptop gaming Acer Nitro Lite 16 NL16 71G 71UJ',
 N'Intel Core i7-13620H, NVIDIA RTX 4050, 16GB DDR5, 512GB SSD, 16" FHD 165Hz',
 24990000, 0, 24990000, 10, 49, N'Acer', 'Active'), -- SubSubcategoryID 49 = "Nitro Series"

-- 4. Lenovo LOQ 15IRX9 (Phiên bản 1)
(N'Laptop gaming Lenovo LOQ 15IRX9 83DV012LVN',
 N'Intel Core i5-13450HX, RTX 4050 6GB, 12GB DDR5 (4+8GB), 512GB PCIe NVMe SSD, 15.6" FHD 144Hz, Legion Coldfront cooling',
 23990000, 12, 21090000, 35, 66, N'Lenovo', 'Active'), -- SubSubcategoryID 66 = "LOQ series"

-- 5. Lenovo LOQ 15IRX9 (Phiên bản 2)  
(N'Laptop gaming Lenovo LOQ 15IRX9 83DV012LVN Pro',
 N'Intel Core i7-13650HX, RTX 4060 8GB, 16GB DDR5 (8+8GB), 1TB PCIe NVMe SSD, 15.6" FHD 165Hz, Enhanced Legion Coldfront',
 29990000, 8, 27590000, 20, 66, N'Lenovo', 'Active');

-- Get ProductIDs
DECLARE @AcerHeliosID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Helios%');
DECLARE @GigabyteG5ID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Gigabyte%');
DECLARE @AcerNitroID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%Nitro Lite%');
DECLARE @LenovoLOQ1ID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%LOQ%' AND ProductName NOT LIKE N'%Pro%');
DECLARE @LenovoLOQ2ID INT = (SELECT ProductID FROM @ProductIDs WHERE ProductName LIKE N'%LOQ%' AND ProductName LIKE N'%Pro%');

-- ========================================================
-- BATCH INSERT: PRODUCT IMAGES (5 DEMO.PNG CHO MỖI SẢN PHẨM)
-- ========================================================

INSERT INTO ProductImages (ProductID, ImageURL, IsPrimary, DisplayOrder) VALUES 
-- Acer Helios Images
(@AcerHeliosID, 'https://product.hstatic.net/200000722513/product/predator_helios_neo_16_phn16-72_bd4a25eebfaf4ae1b538772dbba0b34e_1024x1024.png', 1, 1), (@AcerHeliosID, 'https://product.hstatic.net/200000722513/product/acer_predator_helios_neo_16_2024__1__c77d8224e5c94adaab6d675493a5a5fc_1024x1024.png', 0, 2), (@AcerHeliosID, 'https://product.hstatic.net/200000722513/product/acer_predator_helios_neo_16_2024__2__3ffd04967bc44b82b78f3e0cee408665_1024x1024.png', 0, 3), (@AcerHeliosID, 'https://product.hstatic.net/200000722513/product/acer_predator_helios_neo_16_2024__3__812c58fac7a546bcbe8e36f2bd3bef01_1024x1024.png', 0, 4), (@AcerHeliosID, 'https://product.hstatic.net/200000722513/product/acer_predator_helios_neo_16_2024__4__bf61bbeff6a247ff8a8b96aa13fb8a40_1024x1024.png', 0, 5),
-- Gigabyte G5 Images
(@GigabyteG5ID, 'https://product.hstatic.net/200000722513/product/g5_ge_51vn213sh_9e945568d75145b48fdfb2d3d589bf0b_large_2129e0f3b85842419e9c2f8fe071be74_1024x1024.png', 1, 1), (@GigabyteG5ID, 'https://product.hstatic.net/200000722513/product/g5-mf-f2vn333sh_650c328dd1d84dfba48f8292935db7f6_large_db9b19a657a741d98366cca1d3345c2a_1024x1024.png', 0, 2), (@GigabyteG5ID, 'https://product.hstatic.net/200000722513/product/gigabyte-g5-mf-f2vn333sh-i5-12450h_1_f4015aa8f4344aa282ce267d200d594a_c01e0d0a62de424585ad85ebd04e6a3a_1024x1024.png', 0, 3), (@GigabyteG5ID, 'https://product.hstatic.net/200000722513/product/gigabyte-g5-mf-f2vn333sh-i5-12450h_2_bb8a4c606b3e4e81910d1aaec147d192_098ab96c845a40378cda7f14cd25ff19_1024x1024.png', 0, 4), (@GigabyteG5ID, 'https://product.hstatic.net/200000722513/product/gigabyte-g5-mf-f2vn333sh-i5-12450h_4_b5588fc59865441992a25abff3c1e621_f07198ba9d4541d594e5e8b9c2bc22f1_1024x1024.png', 0, 5),
-- Acer Nitro Images (với link thật từ cybertech.sql)
(@AcerNitroID, 'https://product.hstatic.net/200000722513/product/acer-gaming-nitro-lite-16-nl16-7_b9c923301cac40ec96fdf625748b97ff_grande.png', 1, 1), 
(@AcerNitroID, 'https://product.hstatic.net/200000722513/product/acer-gaming-nitro-lite-16-nl16-7__1__ae9b903e25a2460486e98f74cf872415_1024x1024.png', 0, 2), 
-- Lenovo LOQ 1 Images
(@LenovoLOQ1ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct1_03_62ef93ef41334b458ee6a7daa3657e01_1024x1024.png', 1, 1), (@LenovoLOQ1ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct1_03_a2cfd871a19d4b14915f007339ddf559_1024x1024.png', 0, 2), (@LenovoLOQ1ID, 'https://product.hstatic.net/200000722513/product/loq_15iax9_ct2_03_3f79c790248c45e3bc6eeb13c160c544_1024x1024.png', 0, 3), (@LenovoLOQ1ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct2_02_645f1fa7747041539a20519817d7ba7e_1024x1024.png', 0, 4), (@LenovoLOQ1ID, 'https://product.hstatic.net/200000722513/product/khung-laptop-23_f6099a7e367948bf9ea221d8cdb3263c_1024x1024.png', 0, 5),
-- Lenovo LOQ 2 Images
(@LenovoLOQ2ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct1_03_62ef93ef41334b458ee6a7daa3657e01_1024x1024.png', 1, 1), (@LenovoLOQ2ID, 'https://product.hstatic.net/200000722513/product/loq_15iax9_ct2_03_3f79c790248c45e3bc6eeb13c160c544_1024x1024.png', 0, 2), (@LenovoLOQ2ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct2_02_645f1fa7747041539a20519817d7ba7e_1024x1024.png', 0, 3), (@LenovoLOQ2ID, 'https://product.hstatic.net/200000722513/product/khung-laptop-23_f6099a7e367948bf9ea221d8cdb3263c_1024x1024.png', 0, 4), (@LenovoLOQ2ID, 'https://product.hstatic.net/200000722513/product/loq_15irx9_ct2_04_b7e11335b6894ffb83487b77e150852a_1024x1024.png', 0, 5);

-- ========================================================
-- BATCH INSERT: ATTRIBUTE VALUES (AVOID DUPLICATES)
-- ========================================================

WITH AttributeData AS (
  SELECT * FROM (VALUES
    -- CPU Values
    (@CPUID, N'Intel Core i7-14700HX'), (@CPUID, N'Intel Core i7-13620H'), (@CPUID, N'Intel Core i5-13420H'), 
    (@CPUID, N'Intel Core i5-13450HX'), (@CPUID, N'Intel Core i7-13650HX'),
    -- Card Đồ Họa Values  
    (@CardDoHoaID, N'NVIDIA RTX 4050 6GB'), (@CardDoHoaID, N'NVIDIA RTX 4060 8GB'),
    -- RAM Values
    (@RAMID, N'16GB DDR5 5600MHz'), (@RAMID, N'16GB DDR4 3200MHz'), (@RAMID, N'8GB DDR4'), 
    (@RAMID, N'12GB DDR5 (4+8GB)'), (@RAMID, N'16GB DDR5 (8+8GB)'), (@RAMID, N'16GB DDR5'),
    -- Ổ Cứng Values
    (@OCungID, N'1TB PCIe NVMe SSD'), (@OCungID, N'512GB PCIe NVMe SSD'),
    -- Màn Hình Values
    (@ManHinhID, N'16" WQXGA 2560x1600'), (@ManHinhID, N'15.6" FHD 1920x1080'), (@ManHinhID, N'16" FHD 1920x1080'),
    -- Hệ Điều Hành Values
    (@HeDieuHanhID, N'Windows 11 Home'), (@HeDieuHanhID, N'Windows 11'),
    -- Tần Số Quét Values
    (@TanSoQuetID, N'240Hz'), (@TanSoQuetID, N'144Hz'), (@TanSoQuetID, N'165Hz')
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
    -- Acer Helios mappings (với AttributeID để tránh conflict)
    (@AcerHeliosID, @CPUID, N'Intel Core i7-14700HX'), 
    (@AcerHeliosID, @CardDoHoaID, N'NVIDIA RTX 4050 6GB'), 
    (@AcerHeliosID, @RAMID, N'16GB DDR5 5600MHz'),
    (@AcerHeliosID, @OCungID, N'1TB PCIe NVMe SSD'), 
    (@AcerHeliosID, @ManHinhID, N'16" WQXGA 2560x1600'), 
    (@AcerHeliosID, @HeDieuHanhID, N'Windows 11 Home'), 
    (@AcerHeliosID, @TanSoQuetID, N'240Hz'),
    
    -- Gigabyte G5 mappings  
    (@GigabyteG5ID, @CPUID, N'Intel Core i7-13620H'), 
    (@GigabyteG5ID, @CardDoHoaID, N'NVIDIA RTX 4050 6GB'), 
    (@GigabyteG5ID, @RAMID, N'16GB DDR4 3200MHz'),
    (@GigabyteG5ID, @OCungID, N'512GB PCIe NVMe SSD'), 
    (@GigabyteG5ID, @ManHinhID, N'15.6" FHD 1920x1080'), 
    (@GigabyteG5ID, @HeDieuHanhID, N'Windows 11'), 
    (@GigabyteG5ID, @TanSoQuetID, N'144Hz'),
    
    -- Acer Nitro mappings
    (@AcerNitroID, @CPUID, N'Intel Core i7-13620H'), 
    (@AcerNitroID, @CardDoHoaID, N'NVIDIA RTX 4050 6GB'), 
    (@AcerNitroID, @RAMID, N'16GB DDR5'),
    (@AcerNitroID, @OCungID, N'512GB PCIe NVMe SSD'), 
    (@AcerNitroID, @ManHinhID, N'16" FHD 1920x1080'), 
    (@AcerNitroID, @HeDieuHanhID, N'Windows 11'), 
    (@AcerNitroID, @TanSoQuetID, N'165Hz'),
    
    -- Lenovo LOQ 1 mappings
    (@LenovoLOQ1ID, @CPUID, N'Intel Core i5-13450HX'), 
    (@LenovoLOQ1ID, @CardDoHoaID, N'NVIDIA RTX 4050 6GB'), 
    (@LenovoLOQ1ID, @RAMID, N'12GB DDR5 (4+8GB)'),
    (@LenovoLOQ1ID, @OCungID, N'512GB PCIe NVMe SSD'), 
    (@LenovoLOQ1ID, @ManHinhID, N'15.6" FHD 1920x1080'), 
    (@LenovoLOQ1ID, @HeDieuHanhID, N'Windows 11'), 
    (@LenovoLOQ1ID, @TanSoQuetID, N'144Hz'),
    
    -- Lenovo LOQ 2 mappings
    (@LenovoLOQ2ID, @CPUID, N'Intel Core i7-13650HX'), 
    (@LenovoLOQ2ID, @CardDoHoaID, N'NVIDIA RTX 4060 8GB'), 
    (@LenovoLOQ2ID, @RAMID, N'16GB DDR5 (8+8GB)'),
    (@LenovoLOQ2ID, @OCungID, N'1TB PCIe NVMe SSD'), 
    (@LenovoLOQ2ID, @ManHinhID, N'15.6" FHD 1920x1080'), 
    (@LenovoLOQ2ID, @HeDieuHanhID, N'Windows 11'), 
    (@LenovoLOQ2ID, @TanSoQuetID, N'165Hz')
  ) AS t(ProductID, AttributeID, ValueName)
  INNER JOIN AttributeValue av ON av.ValueName = t.ValueName AND av.AttributeID = t.AttributeID
)
INSERT INTO ProductAttributeValue (ProductID, ValueID)
SELECT ProductID, ValueID FROM ProductAttributeMappings;

-- ========================================================
-- HOÀN THÀNH: ĐÃ SỬA LỖI PRODUCTATTRIBUTEVALUE MAPPING
-- Nguyên nhân: JOIN theo ValueName không có AttributeID gây conflict
-- Giải pháp: Thêm AttributeID vào JOIN condition
-- ========================================================

GO 