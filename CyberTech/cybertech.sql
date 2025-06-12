CREATE DATABASE cybertech
USE cybertech
GO

-- =======================================================
-- NHÓM 1: Các bảng độc lập (không có foreign key)
-- =======================================================
CREATE TABLE Ranks (
    RankId          INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    RankName        NVARCHAR(50)                    NOT NULL UNIQUE,
    MinTotalSpent   DECIMAL(18,2)                   NOT NULL CHECK (MinTotalSpent >= 0),
    DiscountPercent DECIMAL(5,2)                    NULL CHECK (DiscountPercent >= 0),
    PriorityLevel   INT                             NOT NULL CHECK (PriorityLevel >= 0),
    Description     NVARCHAR(255)                   NULL
);

INSERT INTO Ranks (
    RankName, 
    MinTotalSpent, 
    DiscountPercent, 
    PriorityLevel, 
    Description
) VALUES
    (N'Đồng', 0.00, 0.00, 1, N'Xếp hạng cơ bản cho khách hàng mới.'),
    (N'Bạc', 500000.00, 5.00, 2, N'Cho khách hàng chi tiêu vừa phải, được giảm giá nhẹ.'),
    (N'Vàng', 2000000.00, 10.00, 3, N'Cho khách hàng trung thành với chi tiêu đáng kể, được giảm giá tốt hơn.'),
    (N'Bạch Kim', 5000000.00, 15.00, 4, N'Cho khách hàng chi tiêu cao với quyền lợi cao cấp.'),
    (N'Kim Cương', 10000000.00, 20.00, 5, N'Xếp hạng cao nhất với quyền lợi độc quyền và giảm giá tối đa.');

CREATE TABLE Category (
    CategoryID      INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)                   NOT NULL UNIQUE,
    Description     NVARCHAR(MAX)                             NULL
);

-- 4. Bảng ProductAttribute
CREATE TABLE ProductAttribute (
    AttributeID     INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    AttributeName   NVARCHAR(100)                    NOT NULL,
    AttributeType   NVARCHAR(50)                     NOT NULL
);

-- Bảng Vouchers
CREATE TABLE Vouchers (
    VoucherID       INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Code            VARCHAR(50)                     NOT NULL UNIQUE,
    Description     NVARCHAR(250)                            NULL,
    DiscountType    VARCHAR(10)                     NOT NULL CHECK (DiscountType IN ('PERCENT', 'FIXED')),
    DiscountValue   DECIMAL(18,2)                   NOT NULL CHECK (DiscountValue > 0),
    QuantityAvailable INT                           NULL CHECK (QuantityAvailable IS NULL OR QuantityAvailable >= 0),
    ValidFrom       DATETIME                        NOT NULL,
    ValidTo         DATETIME                        NOT NULL,
    IsActive        BIT                             NOT NULL DEFAULT 1,
    AppliesTo       VARCHAR(10)                     NOT NULL DEFAULT 'Order' CHECK (AppliesTo IN ('Order', 'Product')),
    IsSystemWide    BIT                             NOT NULL DEFAULT 0,
    CHECK (ValidTo > ValidFrom)
);

-- =======================================================
-- NHÓM 2: Các bảng phụ thuộc cấp 1
-- =======================================================

-- Bảng CategoryAttributes (unchanged, already plural)
CREATE TABLE CategoryAttributes (
    CategoryAttributeID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    AttributeName NVARCHAR(100) NOT NULL,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);

-- 7. Bảng Subcategory (phụ thuộc Category)
CREATE TABLE Subcategory (
    SubcategoryID   INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)                   NOT NULL,
    Description     NVARCHAR(MAX)                             NULL,
    CategoryID      INT                             NOT NULL,
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);


-- 8. Bảng AttributeValue (phụ thuộc ProductAttribute)
CREATE TABLE AttributeValue (
    ValueID         INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    AttributeID     INT                             NOT NULL,
    ValueName       NVARCHAR(255)                    NOT NULL,
    FOREIGN KEY (AttributeID) REFERENCES ProductAttribute(AttributeID) ON DELETE CASCADE
);


-- Bảng Users (unchanged, already plural)
CREATE TABLE Users (
    UserID          INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)                   NOT NULL,
    Username        VARCHAR(50)                     NOT NULL UNIQUE,
    Email           VARCHAR(100)                    NOT NULL UNIQUE,
    ProfileImageURL VARCHAR(500)                    NULL,
    Role            VARCHAR(20)                     NOT NULL CHECK (Role IN ('Customer', 'Support', 'Manager', 'SuperAdmin')),
    Phone           VARCHAR(20)                     NULL,
    Salary          DECIMAL(18,2)                   NULL CHECK (Salary >= 0),
    TotalSpent      DECIMAL(18,2)                   NOT NULL DEFAULT 0 CHECK (TotalSpent >= 0),
    OrderCount      INT                             NOT NULL DEFAULT 0 CHECK (OrderCount >= 0),
    RankId          INT                             NULL,
    EmailVerified   BIT                             NOT NULL DEFAULT 0,
    UserStatus      VARCHAR(20)                     NOT NULL DEFAULT 'Active' CHECK (UserStatus IN ('Active', 'Inactive', 'Suspended')),
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    Gender          TINYINT                         NULL, -- 1=Male, 2=Female
    DateOfBirth     DATE                            NULL,
    FOREIGN KEY (RankId) REFERENCES Ranks(RankId) ON DELETE SET NULL
);

CREATE TABLE SubSubcategory (
    SubSubcategoryID INT            IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)                   NOT NULL,
    Description     NVARCHAR(MAX)                               NULL,
    SubcategoryID   INT                             NOT NULL,
    FOREIGN KEY (SubcategoryID) REFERENCES Subcategory(SubcategoryID) ON DELETE CASCADE
);

-- Bảng UserAuthMethods (unchanged, already plural)
CREATE TABLE UserAuthMethods (
    ID              INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT                             NOT NULL,
    AuthType        NVARCHAR(50)                    NOT NULL CHECK (AuthType IN ('Password', 'Google', 'Facebook')),
    AuthKey         NVARCHAR(256)                   NOT NULL,
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    CONSTRAINT UQ_UserAuthMethods UNIQUE (UserID, AuthType, AuthKey)
);

-- Bảng PasswordResetTokens (unchanged, already plural)
CREATE TABLE PasswordResetTokens (
    ID              INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT                             NOT NULL,
    Token           NVARCHAR(256)                   NOT NULL,
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    ExpiresAt       DATETIME                        NOT NULL,
    Used            BIT                             NOT NULL DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Bảng UserAddresses (changed from UserAddress)
CREATE TABLE UserAddresses (
    AddressID       INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT                             NOT NULL,
    RecipientName   NVARCHAR(100)                   NOT NULL,
    AddressLine     NVARCHAR(255)                   NOT NULL,
    City            NVARCHAR(100)                   NULL,
    District        NVARCHAR(100)                   NULL,
    Ward            NVARCHAR(100)                   NULL,
    Phone           VARCHAR(20)                     NULL,
    IsPrimary       BIT                             NOT NULL DEFAULT 0,
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Bảng Carts (changed from Cart)
CREATE TABLE Carts (
    CartID          INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT                             NOT NULL,
    TotalPrice      DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (TotalPrice >= 0),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- Bảng Orders (unchanged, already plural)
CREATE TABLE Orders (
    OrderID                 INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID                  INT                             NOT NULL,
    TotalPrice             DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (TotalPrice >= 0),
    ProductDiscountAmount  DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (ProductDiscountAmount >= 0),
    RankDiscountAmount     DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (RankDiscountAmount >= 0),
    VoucherDiscountAmount  DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (VoucherDiscountAmount >= 0),
    TotalDiscountAmount    DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (TotalDiscountAmount >= 0),
    FinalPrice             DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (FinalPrice >= 0),  
    Status                 VARCHAR(50)                     NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Returned')),
    CreatedAt              DATETIME                        NOT NULL DEFAULT GETDATE(),
    UserAddressID          INT,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (UserAddressID) REFERENCES UserAddresses(AddressID)
);

-- =======================================================
-- NHÓM 3: Các bảng phụ thuộc cấp 2
-- =======================================================

-- Bảng Products (changed from Product)
CREATE TABLE Products (
    ProductID       INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    Name            NVARCHAR(100)                   NOT NULL,
    Description      NVARCHAR(MAX)                             NULL,
    Price           DECIMAL(18,2)                   NOT NULL CHECK (Price >= 0),
    SalePercentage  DECIMAL(5,2)                    NULL CHECK (SalePercentage >= 0 AND SalePercentage <= 100),
    SalePrice       DECIMAL(18,2)                   NULL CHECK (SalePrice >= 0),
    Stock           INT                             NOT NULL CHECK (Stock >= 0),
    SubSubcategoryID INT                            NOT NULL,
    Brand           NVARCHAR(100)                   NULL,
    Status          VARCHAR(20)                     NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active', 'Inactive')),
    CreatedAt       DATETIME                       NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME                       NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (SubSubcategoryID) REFERENCES SubSubcategory(SubSubcategoryID)
);

-- Bảng VoucherProducts (unchanged, already plural)
CREATE TABLE VoucherProducts (
    VoucherID INT NOT NULL,
    ProductID INT NOT NULL,
    PRIMARY KEY (VoucherID, ProductID),
    FOREIGN KEY (VoucherID) REFERENCES Vouchers(VoucherID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Bảng Payments (changed from Payment)
CREATE TABLE Payments (
    PaymentID       INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    OrderID         INT                             NOT NULL,
    Amount          DECIMAL(18,2)                   NOT NULL CHECK (Amount >= 0),
    PaymentMethod   NVARCHAR(50)                    NOT NULL CHECK (PaymentMethod IN ('COD', 'VNPay', 'Momo', 'SePay')),
    PaymentStatus   VARCHAR(50)                     NOT NULL DEFAULT 'Pending' CHECK (PaymentStatus IN ('Pending', 'Completed', 'Failed', 'Refunded')),
    PaymentDate     DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

-- =======================================================
-- NHÓM 4: Các bảng phụ thuộc cấp 3
-- =======================================================

-- Bảng ProductImages (changed from ProductImage)
CREATE TABLE ProductImages (
    ImageID         INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    ProductID       INT                             NOT NULL,
    ImageURL        VARCHAR(255)                    NOT NULL,
    IsPrimary       BIT                             NOT NULL DEFAULT 0,
    DisplayOrder    INT                             NOT NULL DEFAULT 0,
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Bảng ProductAttributeValues (changed from ProductAttributeValue)
CREATE TABLE ProductAttributeValue (
    ProductID       INT                             NOT NULL,
    ValueID         INT                             NOT NULL,
    PRIMARY KEY (ProductID, ValueID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (ValueID) REFERENCES AttributeValue(ValueID) ON DELETE CASCADE
);

CREATE TABLE WishlistItems (
    WishlistItemID  INT             IDENTITY(1,1) PRIMARY KEY,
    UserID          INT                             NOT NULL,
    ProductID       INT                             NOT NULL,
    AddedDate       DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
    CONSTRAINT UQ_User_Product UNIQUE (UserID, ProductID) -- Tránh trùng sản phẩm
);

-- Bảng CartItems (changed from CartItem)
CREATE TABLE CartItems (
    CartItemID      INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    CartID          INT                             NOT NULL,
    ProductID       INT                             NOT NULL,
    Quantity        INT                             NOT NULL CHECK (Quantity > 0),
    Subtotal        DECIMAL(18,2)                   NOT NULL CHECK (Subtotal >= 0),
    FOREIGN KEY (CartID) REFERENCES Carts(CartID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Bảng OrderItems (unchanged, already plural)
CREATE TABLE OrderItems (
    OrderItemID     INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    OrderID         INT                             NOT NULL,
    ProductID       INT                             NOT NULL,
    Quantity        INT                             NOT NULL CHECK (Quantity > 0),
    UnitPrice       DECIMAL(18,2)                   NOT NULL CHECK (UnitPrice >= 0),
    Subtotal        DECIMAL(18,2)                   NOT NULL CHECK (Subtotal >= 0),
    DiscountAmount  DECIMAL(18,2)                   NOT NULL DEFAULT 0.00 CHECK (DiscountAmount >= 0),
    FinalSubtotal   DECIMAL(18,2)                   NOT NULL CHECK (FinalSubtotal >= 0),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Bảng Reviews (changed from Review)
CREATE TABLE Reviews (
    ReviewID        INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT                             NOT NULL,
    ProductID       INT                             NOT NULL,
    Rating          INT                             NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment          NVARCHAR(MAX)                             NULL,
    CreatedAt       DATETIME                        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Bảng UserVouchers
CREATE TABLE UserVouchers (
    UserVoucherID   INT             IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    UserID          INT             NOT NULL,
    VoucherID       INT             NOT NULL,
    AssignedDate    DATETIME        NOT NULL,
    UsedDate        DATETIME        NULL,
    IsUsed          BIT             NOT NULL DEFAULT 0,
    OrderID         INT             NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (VoucherID) REFERENCES Vouchers(VoucherID),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);
CREATE TABLE VoucherTokens (
    TokenID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Token NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ExpiresAt DATETIME NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    UsedAt DATETIME NULL,
    VoucherCode NVARCHAR(50) NULL,
    CONSTRAINT FK_VoucherTokens_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

CREATE TABLE ProductStockNotifications (
    NotificationID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    UserID INT NOT NULL,
    IsNotified BIT NOT NULL DEFAULT 0,
    NotificationSentAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ProductStockNotifications_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    CONSTRAINT FK_ProductStockNotifications_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT UQ_ProductStockNotifications_User_Product UNIQUE (UserID, ProductID)
);
--------------------------------- THÊM DỮ LIỆU -------------------------------------
-- Thêm Dữ Liệu Vào Danh Mục Chính
INSERT INTO Category (Name, Description)
VALUES 
    ('Laptop', 'Laptops and notebooks'),
    ('Laptop Gaming', 'Gaming laptops'),
    ('PC GVN', 'Pre-built PCs'),
    ('Main, CPU, VGA', 'Motherboards, CPUs, and GPUs'),
    (N'Case, Nguồn, Tản', 'Cases, power supplies, and cooling'),
    (N'Ổ cứng, RAM, Thẻ nhớ', 'Storage and memory'),
    (N'Loa, Micro, Webcam', 'Audio and video peripherals'),
    (N'Màn hình', 'Monitors'),
    (N'Bàn phím', 'Keyboards'),
    (N'Chuột + Lót chuột', 'Mice and mousepads'),
    ('Tai Nghe', 'Headphones'),
    (N'Ghế - Bàn', 'Chairs and desks'),
    (N'Phần mềm, mạng', 'Software and networking'),
    ('Handheld, Console', 'Handheld and console gaming'),
    (N'Phụ kiện (Hub, sạc, cáp..)', 'Accessory'),
    (N'Dịch vụ và thông tin khác', 'Services and other information');

-- Thêm Dữ Liệu Vào Danh Mục Phụ
INSERT INTO Subcategory (Name, Description, CategoryID)
VALUES 
    -- Category 'Laptop' (CategoryID = 1)
    (N'Thương hiệu', 'Laptop brands', 1),
    (N'Giá bán', 'Price ranges', 1),
    ('CPU Intel - AMD', 'Processor types', 1),
    (N'Nhu cầu sử dụng', 'Usage needs', 1),
    (N'Linh phụ kiện Laptop', 'Laptop accessory', 1),
    ('Laptop ASUS', 'ASUS laptops', 1),
    ('Laptop ACER', 'ACER laptops', 1),
    ('Laptop MSI', 'MSI laptops', 1),
    ('Laptop Lenovo', 'Lenovo laptops', 1),
    ('Laptop Dell', 'Dell laptops', 1),
    ('Laptop AI', 'AI laptops', 1),

    -- Category 'Laptop Gaming' (CategoryID = 2)
    (N'Thương hiệu', 'Gaming laptop brands', 2),
    (N'Giá bán', 'Price ranges', 2),
    ('ACER | PREDATOR', 'ACER gaming sery', 2),
    ('ASUS | ROG Gaming', 'ASUS gaming sery', 2),
    ('MSI Gaming', 'MSI gaming sery', 2),
    ('LENOVO Gaming', 'Lenovo gaming sery', 2),
    ('Dell Gaming', 'Dell gaming sery', 2),
    ('HP Gaming', 'HP gaming sery', 2),
    (N'Cấu hình', 'Configurations', 2),
    (N'Linh - Phụ kiện Laptop', 'Laptop accessory', 2),

    -- Category 'PC GVN' (CategoryID = 3)
    (N'KHUYẾN MÃI HOT', 'Hot promotions', 3),
    (N'PC KHUYẾN MÃI', 'Promotional PCs', 3),
    (N'PC theo cấu hình VGA', 'PCs by VGA configuration', 3),
    ('A.I PC - GVN', 'AI PCs', 3),
    (N'PC theo CPU Intel', 'PCs by Intel CPU', 3),
    (N'PC theo CPU AMD', 'PCs by AMD CPU', 3),
    (N'PC Văn phòng', 'Office PCs', 3),
    (N'Phần mềm bản quyền', 'Licensed software', 3),

    -- Category 'Main, CPU, VGA' (CategoryID = 4)
    ('VGA RTX 50 SERy', 'RTX 50 sery GPUs', 4),
    ('VGA (Trên 12 GB VRAM)', 'GPUs with over 12GB VRAM', 4),
    ('VGA (Dưới 12 GB VRAM)', 'GPUs with under 12GB VRAM', 4),
    ('VGA - Card màn hình', 'Graphics cards', 4),
    (N'Bo mạch chủ Intel', 'Intel motherboards', 4),
    (N'Bo mạch chủ AMD', 'AMD motherboards', 4),
    (N'CPU - Bộ vi xử lý Intel', 'Intel CPUs', 4),
    (N'CPU - Bộ vi xử lý AMD', 'AMD CPUs', 4),

    -- Category 'Case, Nguồn, Tản' (CategoryID = 5)
    ('Case - Theo hãng', 'Cases by brand', 5),
    ('Case - Theo giá', 'Cases by price', 5),
    (N'Nguồn - Theo Hãng', 'Power supplies by brand', 5),
    (N'Nguồn - Theo công suất', 'Power supplies by wattage', 5),
    (N'Phụ kiện PC', 'PC accessory', 5),
    (N'Loại tản nhiệt', 'Cooling types', 5),

    -- Category 'Ổ cứng, RAM, Thẻ nhớ' (CategoryID = 6)
    (N'Dung lượng RAM', 'RAM capacities', 6),
    (N'Loại RAM', 'RAM types', 6),
    (N'Hãng RAM', 'RAM brands', 6),
    (N'Dung lượng HDD', 'HDD capacities', 6),
    (N'Hãng HDD', 'HDD brands', 6),
    (N'Dung lượng SSD', 'SSD capacities', 6),
    (N'Hãng SSD', 'SSD brands', 6),
    (N'Thẻ nhớ / USB', 'Memory cards and USB drives', 6),
    (N'Ổ cứng di động', 'Portable hard drives', 6),

    -- Category 'Loa, Micro, Webcam' (CategoryID = 7)
    (N'Thương hiệu loa', 'Speaker brands', 7),
    (N'Kiểu Loa', 'Speaker types', 7),
    ('Webcam', 'Webcams', 7),
    ('Microphone', 'Microphones', 7),

    -- Category 'Màn hình' (CategoryID = 8)
    (N'Hãng sản xuất', 'Monitor brands', 8),
    (N'Giá tiền', 'Price ranges', 8),
    (N'Độ Phân giải', 'Resolutions', 8),
    (N'Tần số quét', 'Refresh rates', 8),
    (N'Màn hình cong', 'Curved monitors', 8),
    (N'Kích thước', 'Screen sizes', 8),
    (N'Màn hình đồ họa', 'Graphic design monitors', 8),
    (N'Phụ kiện màn hình', 'Monitor accessory', 8),
    (N'Màn hình di động', 'Portable monitors', 8),
    (N'Màn hình Oled', 'OLED monitors', 8),

    -- Category 'Bàn phím' (CategoryID = 9)
    (N'Thương hiệu', 'Keyboard brands', 9),
    (N'Giá tiền', 'Price ranges', 9),
    (N'Kết nối', 'Connection types', 9),
    (N'Phụ kiện bàn phím cơ', 'Mechanical keyboard accessory', 9),

    -- Category 'Chuột + Lót chuột' (CategoryID = 10)
    (N'Thương hiệu chuột', 'Mouse brands', 10),
    (N'Chuột theo giá tiền', 'Mice by price', 10),
    (N'Loại Chuột', 'Mouse types', 10),
    ('Logitech', 'Logitech mice', 10),
    (N'Thương hiệu lót chuột', 'Mousepad brands', 10),
    (N'Các loại lót chuột', 'Mousepad types', 10),
    (N'Lót chuột theo size', 'Mousepad sizes', 10),

    -- Category 'Tai Nghe' (CategoryID = 11)
    (N'Thương hiệu tai nghe', 'Headphone brands', 11),
    (N'Tai nghe theo giá', 'Headphones by price', 11),
    (N'Kiểu kết nối', 'Connection types', 11),
    (N'Kiểu tai nghe', 'Headphone styles', 11),

    -- Category 'Ghế - Bàn' (CategoryID = 12)
    (N'Thương hiệu ghế Gaming', 'Gaming chair brands', 12),
    (N'Thương hiệu ghế CTH', 'Ergonomic chair brands', 12),
    (N'Kiểu ghế', 'Chair types', 12),
    (N'Bàn Gaming', 'Gaming desks', 12),
    (N'Bàn công thái học', 'Ergonomic desks', 12),
    (N'Giá tiền', 'Price ranges', 12),

    -- Category 'Phần mềm, mạng' (CategoryID = 13)
    (N'Hãng sản xuất', 'Manufacturers', 13),
    ('Router Wi-Fi', 'Wi-Fi routers', 13),
    (N'USB Thu sóng - Card mạng', 'Network adapters', 13),
    ('Microsoft Office', 'Microsoft Office software', 13),
    ('Microsoft Windows', 'Microsoft Windows software', 13),

    -- Category 'Handheld, Console' (CategoryID = 14)
    ('Handheld PC', 'Handheld gaming PCs', 14),
    (N'Tay cầm', 'Controllers', 14),
    (N'Vô lăng lái xe, máy bay', 'Steering wheels and flight sticks', 14),
    ('Sony Playstation', 'Sony Playstation consoles', 14),

    -- Category 'Phụ kiện (Hub, sạc, cáp..)' (CategoryID = 15)
    (N'Hub, sạc, cáp', 'Hubs, chargers, and cables', 15),
    (N'Quạt cầm tay, Quạt mini', 'Handheld and mini fans', 15),

    -- Category 'Dịch vụ và thông tin khác' (CategoryID = 16)
    (N'Dịch vụ', 'Services', 16),
    (N'Chính sách', 'Policies', 16),
    ('Build PC', 'PC building services', 16);

-- Thêm Dữ Liệu Vào Danh Mục Chi Tiết Bằng CTE

-- PHIÊN BẢN 1: CTE ĐẦY ĐỦ (Tối ưu nhất)
WITH SubcategoryLookup AS (
    SELECT 
        Name,
        SubcategoryID,
        CategoryID
    FROM Subcategory 
    WHERE CategoryID = 1
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        -- Thương hiệu laptops
        ('ASUS', 'ASUS laptops', N'Thương hiệu'),
        ('ACER', 'ACER laptops', N'Thương hiệu'),
        ('MSI', 'MSI laptops', N'Thương hiệu'),
        ('LENOVO', 'LENOVO laptops', N'Thương hiệu'),
        ('DELL', 'DELL laptops', N'Thương hiệu'),
        ('HP - Pavilion', 'HP - Pavilion laptops', N'Thương hiệu'),
        ('LG - Gram', 'LG - Gram laptops', N'Thương hiệu'),
        
        -- Giá bán
        (N'Dưới 15 triệu', 'Laptops under 15 million', N'Giá bán'),
        (N'Từ 15 đến 20 triệu', 'Laptops from 15 to 20 million', N'Giá bán'),
        (N'Trên 20 triệu', 'Laptops over 20 million', N'Giá bán'),
        
        -- CPU Intel - AMD
        ('Intel Core i3', 'Laptops with Intel Core i3', 'CPU Intel - AMD'),
        ('Intel Core i5', 'Laptops with Intel Core i5', 'CPU Intel - AMD'),
        ('Intel Core i7', 'Laptops with Intel Core i7', 'CPU Intel - AMD'),
        ('AMD Ryzen', 'Laptops with AMD Ryzen', 'CPU Intel - AMD'),
        
        -- Nhu cầu sử dụng
        (N'Đồ họa - Studio', 'Laptops for graphics and studio', N'Nhu cầu sử dụng'),
        (N'Học sinh - Sinh viên', 'Laptops for students', N'Nhu cầu sử dụng'),
        (N'Mỏng nhẹ cao cấp', 'Premium thin and light laptops', N'Nhu cầu sử dụng'),
        
        -- Linh phụ kiện Laptop
        ('Ram laptop', 'Laptop RAM', N'Linh phụ kiện Laptop'),
        ('SSD laptop', 'Laptop SSD', N'Linh phụ kiện Laptop'),
        (N'Ổ cứng di động', 'Portable hard drives', N'Linh phụ kiện Laptop'),
        
        -- Laptop ASUS
        ('ASUS OLED Sery', 'ASUS OLED laptops', 'Laptop ASUS'),
        ('Vivobook Sery', 'ASUS Vivobook laptops', 'Laptop ASUS'),
        ('Zenbook Sery', 'ASUS Zenbook laptops', 'Laptop ASUS'),
        
        -- Laptop ACER
        ('Aspire Sery', 'ACER Aspire laptops', 'Laptop ACER'),
        ('Swift Sery', 'ACER Swift laptops', 'Laptop ACER'),
        
        -- Laptop MSI
        ('Modern Sery', 'MSI Modern laptops', 'Laptop MSI'),
        ('Prestige Sery', 'MSI Prestige laptops', 'Laptop MSI'),
        
        -- Laptop Lenovo
        ('Thinkbook Sery', 'Lenovo Thinkbook laptops', 'Laptop Lenovo'),
        ('Ideapad Sery', 'Lenovo Ideapad laptops', 'Laptop Lenovo'),
        ('Thinkpad Sery', 'Lenovo Thinkpad laptops', 'Laptop Lenovo'),
        ('Yoga Sery', 'Lenovo Yoga laptops', 'Laptop Lenovo'),
        
        -- Laptop Dell
        ('Inspirion Sery', 'Dell Inspirion laptops', 'Laptop Dell'),
        ('Vostro Sery', 'Dell Vostro laptops', 'Laptop Dell'),
        ('Latitude Sery', 'Dell Latitude laptops', 'Laptop Dell'),
        ('XPS Sery', 'Dell XPS laptops', 'Laptop Dell'),
        
        -- Laptop AI
        ('Laptop AI', 'AI laptops', 'Laptop AI')
    ) AS SubSubcategoryValues(Name, Description, SubcategoryName)
)

-- Thực hiện INSERT với JOIN để lấy SubcategoryID
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.Name
WHERE sl.SubcategoryID IS NOT NULL;

-- CTE ĐẦY ĐỦ cho Laptop Gaming Category (CategoryID = 2)
WITH SubcategoryLookup AS (
    SELECT 
        Name,
        SubcategoryID,
        CategoryID
    FROM Subcategory 
    WHERE CategoryID = 2
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        -- Thương hiệu gaming laptops
        ('ACER / PREDATOR', 'ACER / PREDATOR gaming laptops', N'Thương hiệu'),
        ('ASUS / ROG', 'ASUS / ROG gaming laptops', N'Thương hiệu'),
        ('MSI', 'MSI gaming laptops', N'Thương hiệu'),
        ('LENOVO', 'LENOVO gaming laptops', N'Thương hiệu'),
        ('DELL', 'DELL gaming laptops', N'Thương hiệu'),
        ('GIGABYTE / AORUS', 'GIGABYTE / AORUS gaming laptops', N'Thương hiệu'),
        ('HP', 'HP gaming laptops', N'Thương hiệu'),
        
        -- Giá bán gaming laptops
        (N'Dưới 20 triệu', 'Gaming laptops under 20 million', N'Giá bán'),
        (N'Từ 20 đến 25 triệu', 'Gaming laptops from 20 to 25 million', N'Giá bán'),
        (N'Từ 25 đến 30 triệu', 'Gaming laptops from 25 to 30 million', N'Giá bán'),
        (N'Trên 30 triệu', 'Gaming laptops over 30 million', N'Giá bán'),
        ('Gaming RTX 50 Sery', 'Gaming laptops with RTX 50 Sery', N'Giá bán'),
        
        -- ACER | PREDATOR Sery
        ('Nitro Sery', 'ACER Nitro gaming laptops', 'ACER | PREDATOR'),
        ('Aspire Sery', 'ACER Aspire gaming laptops', 'ACER | PREDATOR'),
        ('Predator Sery', 'ACER Predator gaming laptops', 'ACER | PREDATOR'),
        ('ACER RTX 50 Sery', 'ACER gaming laptops with RTX 50 Sery', 'ACER | PREDATOR'),
        
        -- ASUS | ROG Gaming Sery
        ('ROG Sery', 'ASUS ROG gaming laptops', 'ASUS | ROG Gaming'),
        ('TUF Sery', 'ASUS TUF gaming laptops', 'ASUS | ROG Gaming'),
        ('Zephyrus Sery', 'ASUS Zephyrus gaming laptops', 'ASUS | ROG Gaming'),
        ('ASUS RTX 50 Sery', 'ASUS gaming laptops with RTX 50 Sery', 'ASUS | ROG Gaming'),
        
        -- MSI Gaming Sery
        ('Titan GT Sery', 'MSI Titan GT gaming laptops', 'MSI Gaming'),
        ('Stealth GS Sery', 'MSI Stealth GS gaming laptops', 'MSI Gaming'),
        ('Raider GE Sery', 'MSI Raider GE gaming laptops', 'MSI Gaming'),
        ('Vector GP Sery', 'MSI Vector GP gaming laptops', 'MSI Gaming'),
        ('Crosshair / Pulse GL Sery', 'MSI Crosshair / Pulse GL gaming laptops', 'MSI Gaming'),
        ('Sword / Katana GF66 Sery', 'MSI Sword / Katana GF66 gaming laptops', 'MSI Gaming'),
        ('Cyborg / Thin GF Sery', 'MSI Cyborg / Thin GF gaming laptops', 'MSI Gaming'),
        ('MSI RTX 50 Sery', 'MSI gaming laptops with RTX 50 Sery', 'MSI Gaming'),
        
        -- LENOVO Gaming Sery
        ('Legion Gaming', 'LENOVO Legion gaming laptops', 'LENOVO Gaming'),
        ('LOQ sery', 'LENOVO LOQ gaming laptops', 'LENOVO Gaming'),
        ('RTX 50 Sery', 'LENOVO gaming laptops with RTX 50 Sery', 'LENOVO Gaming'),
        
        -- Dell Gaming Sery
        ('Dell Gaming G Sery', 'Dell Gaming G Sery laptops', 'Dell Gaming'),
        ('Alienware Sery', 'Dell Alienware gaming laptops', 'Dell Gaming'),
        
        -- HP Gaming Sery
        ('HP Victus', 'HP Victus gaming laptops', 'HP Gaming'),
        ('Hp Omen', 'HP Omen gaming laptops', 'HP Gaming'),
        
        -- Cấu hình
        ('RTX 50 Sery', 'Gaming laptops with RTX 50 Sery', N'Cấu hình'),
        ('CPU Core Ultra', 'Gaming laptops with CPU Core Ultra', N'Cấu hình'),
        ('CPU AMD', 'Gaming laptops with CPU AMD', N'Cấu hình'),
        
        -- Linh phụ kiện Laptop
        ('Ram laptop', 'Laptop RAM', N'Linh - Phụ kiện Laptop'),
        ('SSD laptop', 'Laptop SSD', N'Linh - Phụ kiện Laptop'),
        (N'Ổ cứng di động', 'Portable hard drives', N'Linh - Phụ kiện Laptop')
    ) AS SubSubcategoryValues(Name, Description, SubcategoryName)
)

-- Thực hiện INSERT với JOIN để lấy SubcategoryID
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.Name
WHERE sl.SubcategoryID IS NOT NULL;


WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 3
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('PC RTX 5090', 'PCs with RTX 5090', N'KHUYẾN MÃI HOT'),
        ('PC RTX 5080', 'PCs with RTX 5080', N'KHUYẾN MÃI HOT'),
        ('PC RTX 5070', 'PCs with RTX 5070', N'KHUYẾN MÃI HOT'),
        ('PC GVN RTX 5070Ti', 'PCs with RTX 5070Ti', N'KHUYẾN MÃI HOT'),
        (N'Thu cũ đổi mới VGA', 'Trade-in old VGA for new', N'KHUYẾN MÃI HOT'),
        ('BTF i7 - 4070Ti Super', 'BTF i7 - 4070Ti Super PCs', N'PC KHUYẾN MÃI'),
        ('I5 - 4060', 'I5 - 4060 PCs', N'PC KHUYẾN MÃI'),
        ('I5 - 4060Ti', 'I5 - 4060Ti PCs', N'PC KHUYẾN MÃI'),
        ('PC RX 6600 - 12TR690', 'PCs with RX 6600 - 12TR690', N'PC KHUYẾN MÃI'),
        ('PC RX 6500 - 9TR990', 'PCs with RX 6500 - 9TR990', N'PC KHUYẾN MÃI'),
        (N'PC sử dụng VGA 1650', 'PCs using VGA 1650', N'PC theo cấu hình VGA'),
        (N'PC sử dụng VGA 3050', 'PCs using VGA 3050', N'PC theo cấu hình VGA'),
        (N'PC sử dụng VGA 3060', 'PCs using VGA 3060', N'PC theo cấu hình VGA'),
        (N'PC sử dụng VGA RX 6600', 'PCs using VGA RX 6600', N'PC theo cấu hình VGA'),
        (N'PC sử dụng VGA RX 6500', 'PCs using VGA RX 6500', N'PC theo cấu hình VGA'),
        ('PC GVN X ASUS - PBA', 'PC GVN X ASUS - PBA', 'A.I PC - GVN'),
        ('PC GVN X MSI', 'PC GVN X MSI', 'A.I PC - GVN'),
        ('PC MSI - Powered by MSI', 'PC MSI - Powered by MSI', 'A.I PC - GVN'),
        ('PC Core I3', 'PCs with Core I3', N'PC theo CPU Intel'),
        ('PC Core I5', 'PCs with Core I5', N'PC theo CPU Intel'),
        ('PC Core I7', 'PCs with Core I7', N'PC theo CPU Intel'),
        ('PC Core I9', 'PCs with Core I9', N'PC theo CPU Intel'),
        ('PC AMD R3', 'PCs with AMD R3', N'PC theo CPU AMD'),
        ('PC AMD R5', 'PCs with AMD R5', N'PC theo CPU AMD'),
        ('PC AMD R7', 'PCs with AMD R7', N'PC theo CPU AMD'),
        ('PC AMD R9', 'PCs with AMD R9', N'PC theo CPU AMD'),
        (N'Homework Athlon - Giá chỉ 3.990k', 'Homework Athlon - Only 3.990k', N'PC Văn phòng'),
        (N'Homework R3 - Giá chỉ 5,690k', 'Homework R3 - Only 5,690k', N'PC Văn phòng'),
        (N'Homework R5 - Giá chỉ 5,690k', 'Homework R5 - Only 5,690k', N'PC Văn phòng'),
        (N'Homework I5 - Giá chỉ 5,690k', 'Homework I5 - Only 5,690k', N'PC Văn phòng'),
        (N'Window bản quyền - Chỉ từ 2.990K', 'Licensed Windows - From 2.990K', N'Phần mềm bản quyền'),
        (N'Office 365 bản quyền - Chỉ từ 990K', 'Licensed Office 365 - From 990K', N'Phần mềm bản quyền')
    ) AS t(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON sl.SubcategoryName = ssd.SubcategoryName;

WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 4
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('RTX 5090', 'RTX 5090 GPUs', 'VGA RTX 50 SERy'),
        ('RTX 5080', 'RTX 5080 GPUs', 'VGA RTX 50 SERy'),
        ('RTX 5070Ti', 'RTX 5070Ti GPUs', 'VGA RTX 50 SERy'),
        ('RTX 5070', 'RTX 5070 GPUs', 'VGA RTX 50 SERy'),
        ('RTX 5060Ti', 'RTX 5060Ti GPUs', 'VGA RTX 50 SERy'),
        ('RTX 4070 SUPER (12GB)', 'RTX 4070 SUPER (12GB) GPUs', N'VGA (Trên 12 GB VRAM)'),
        ('RTX 4070Ti SUPER (16GB)', 'RTX 4070Ti SUPER (16GB) GPUs', N'VGA (Trên 12 GB VRAM)'),
        ('RTX 4080 SUPER (16GB)', 'RTX 4080 SUPER (16GB) GPUs', N'VGA (Trên 12 GB VRAM)'),
        ('RTX 4090 SUPER (24GB)', 'RTX 4090 SUPER (24GB) GPUs', N'VGA (Trên 12 GB VRAM)'),
        ('RTX 4060Ti (8 - 16GB)', 'RTX 4060Ti (8 - 16GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('RTX 4060 (8GB)', 'RTX 4060 (8GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('RTX 3060 (12GB)', 'RTX 3060 (12GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('RTX 3050 (6 - 8GB)', 'RTX 3050 (6 - 8GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('GTX 1650 (4GB)', 'GTX 1650 (4GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('GT 710 / GT 1030 (2-4GB)', 'GT 710 / GT 1030 (2-4GB) GPUs', N'VGA (Dưới 12 GB VRAM)'),
        ('NVIDIA Quadro', 'NVIDIA Quadro GPUs', N'VGA - Card màn hình'),
        ('AMD Radeon', 'AMD Radeon GPUs', N'VGA - Card màn hình'),
        (N'Z890 (Mới)', 'Z890 motherboards', N'Bo mạch chủ Intel'),
        ('Z790', 'Z790 motherboards', N'Bo mạch chủ Intel'),
        ('B760', 'B760 motherboards', N'Bo mạch chủ Intel'),
        ('H610', 'H610 motherboards', N'Bo mạch chủ Intel'),
        ('X299X', 'X299X motherboards', N'Bo mạch chủ Intel'),
        (N'Xem tất cả', 'View all', N'Bo mạch chủ Intel'),
        (N'AMD X870 (Mới)', 'AMD X870 motherboards', N'Bo mạch chủ AMD'),
        ('AMD X670', 'AMD X670 motherboards', N'Bo mạch chủ AMD'),
        ('AMD X570', 'AMD X570 motherboards', N'Bo mạch chủ AMD'),
        (N'AMD B650 (Mới)', 'AMD B650 motherboards', N'Bo mạch chủ AMD'),
        ('AMD B550', 'AMD B550 motherboards', N'Bo mạch chủ AMD'),
        ('AMD A320', 'AMD A320 motherboards', N'Bo mạch chủ AMD'),
        ('AMD TRX40', 'AMD TRX40 motherboards', N'Bo mạch chủ AMD'),
        (N'CPU Intel Core Ultra Sery 2 (Mới)', 'CPU Intel Core Ultra Sery 2', N'CPU - Bộ vi xử lý Intel'),
        ('CPU Intel 9', 'CPU Intel 9', N'CPU - Bộ vi xử lý Intel'),
        ('CPU Intel 7', 'CPU Intel 7', N'CPU - Bộ vi xử lý Intel'),
        ('CPU Intel 5', 'CPU Intel 5', N'CPU - Bộ vi xử lý Intel'),
        ('CPU Intel 3', 'CPU Intel 3', N'CPU - Bộ vi xử lý Intel'),
        ('CPU AMD Athlon', 'CPU AMD Athlon', N'CPU - Bộ vi xử lý AMD'),
        ('CPU AMD R3', 'CPU AMD R3', N'CPU - Bộ vi xử lý AMD'),
        ('CPU AMD R5', 'CPU AMD R5', N'CPU - Bộ vi xử lý AMD'),
        ('CPU AMD R7', 'CPU AMD R7', N'CPU - Bộ vi xử lý AMD'),
        ('CPU AMD R9', 'CPU AMD R9', N'CPU - Bộ vi xử lý AMD')
    ) AS t(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON sl.SubcategoryName = ssd.SubcategoryName;

WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 5
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Case ASUS', 'ASUS cases', 'Case - Theo hãng'),
        ('Case Corsair', 'Corsair cases', 'Case - Theo hãng'),
        ('Case Lianli', 'Lianli cases', 'Case - Theo hãng'),
        ('Case NZXT', 'NZXT cases', 'Case - Theo hãng'),
        ('Case Inwin', 'Inwin cases', 'Case - Theo hãng'),
        ('Case Thermaltake', 'Thermaltake cases', 'Case - Theo hãng'),
        (N'Xem tất cả', 'View all', 'Case - Theo hãng'),
        (N'Dưới 1 triệu', 'Cases under 1 million', 'Case - Theo giá'),
        (N'Từ 1 triệu đến 2 triệu', 'Cases from 1 to 2 million', 'Case - Theo giá'),
        (N'Trên 2 triệu', 'Cases over 2 million', 'Case - Theo giá'),
        (N'Xem tất cả', 'View all', 'Case - Theo giá'),
        (N'Nguồn ASUS', 'ASUS power supplies', N'Nguồn - Theo Hãng'),
        (N'Nguồn DeepCool', 'DeepCool power supplies', N'Nguồn - Theo Hãng'),
        (N'Nguồn Corsair', 'Corsair power supplies', N'Nguồn - Theo Hãng'),
        (N'Nguồn NZXT', 'NZXT power supplies', N'Nguồn - Theo Hãng'),
        (N'Nguồn MSI', 'MSI power supplies', N'Nguồn - Theo Hãng'),
        (N'Xem tất cả', 'View all', N'Nguồn - Theo Hãng'),
        (N'Từ 400w - 500w', 'Power supplies from 400w to 500w', N'Nguồn - Theo công suất'),
        (N'Từ 500w - 600w', 'Power supplies from 500w to 600w', N'Nguồn - Theo công suất'),
        (N'Từ 700w - 800w', 'Power supplies from 700w to 800w', N'Nguồn - Theo công suất'),
        (N'Trên 1000w', 'Power supplies over 1000w', N'Nguồn - Theo công suất'),
        (N'Xem tất cả', 'View all', N'Nguồn - Theo công suất'),
        (N'Dây LED', 'LED strips', N'Phụ kiện PC'),
        (N'Dây rise - Dựng VGA', 'Riser cables for VGA', N'Phụ kiện PC'),
        (N'Giá đỡ VGA', 'VGA holders', N'Phụ kiện PC'),
        (N'Keo tản nhiệt', 'Thermal paste', N'Phụ kiện PC'),
        (N'Xem tất cả', 'View all', N'Phụ kiện PC'),
        (N'Tản nhiệt AIO 240mm', '240mm AIO coolers', N'Loại tản nhiệt'),
        (N'Tản nhiệt AIO 280mm', '280mm AIO coolers', N'Loại tản nhiệt'),
        (N'Tản nhiệt AIO 360mm', '360mm AIO coolers', N'Loại tản nhiệt'),
        (N'Tản nhiệt AIO 420mm', '420mm AIO coolers', N'Loại tản nhiệt'),
        (N'Tản nhiệt khí', 'Air coolers', N'Loại tản nhiệt'),
        (N'Fan RGB', 'RGB fans', N'Loại tản nhiệt'),
        (N'Xem tất cả', 'View all', N'Loại tản nhiệt')
    ) AS t(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON sl.SubcategoryName = ssd.SubcategoryName;

-- 'Ổ cứng, RAM, Thẻ nhớ' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 6
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('8 GB', '8 GB RAM', N'Dung lượng RAM'),
        ('16 GB', '16 GB RAM', N'Dung lượng RAM'),
        ('32 GB', '32 GB RAM', N'Dung lượng RAM'),
        ('64 GB', '64 GB RAM', N'Dung lượng RAM'),
        (N'Xem tất cả', 'View all', N'Dung lượng RAM'),
        ('DDR4', 'DDR4 RAM', N'Loại RAM'),
        ('DDR5', 'DDR5 RAM', N'Loại RAM'),
        (N'Xem tất cả', 'View all', N'Loại RAM'),
        ('Corsair', 'Corsair RAM', N'Hãng RAM'),
        ('Kingston', 'Kingston RAM', N'Hãng RAM'),
        ('G.Skill', 'G.Skill RAM', N'Hãng RAM'),
        ('PNY', 'PNY RAM', N'Hãng RAM'),
        (N'Xem tất cả', 'View all', N'Hãng RAM'),
        ('HDD 1 TB', '1 TB HDD', N'Dung lượng HDD'),
        ('HDD 2 TB', '2 TB HDD', N'Dung lượng HDD'),
        ('HDD 4 TB - 6 TB', '4 TB - 6 TB HDD', N'Dung lượng HDD'),
        (N'HDD trên 8 TB', 'HDD over 8 TB', N'Dung lượng HDD'),
        (N'Xem tất cả', 'View all', N'Dung lượng HDD'),
        ('WesterDigital', 'Western Digital HDD', N'Hãng HDD'),
        ('Seagate', 'Seagate HDD', N'Hãng HDD'),
        ('Toshiba', 'Toshiba HDD', N'Hãng HDD'),
        (N'Xem tất cả', 'View all', N'Hãng HDD'),
        ('120GB - 128GB', '120GB - 128GB SSD', N'Dung lượng SSD'),
        ('250GB - 256GB', '250GB - 256GB SSD', N'Dung lượng SSD'),
        ('480GB - 512GB', '480GB - 512GB SSD', N'Dung lượng SSD'),
        ('960GB - 1TB', '960GB - 1TB SSD', N'Dung lượng SSD'),
        ('2TB', '2TB SSD', N'Dung lượng SSD'),
        (N'Trên 2TB', 'Over 2TB SSD', N'Dung lượng SSD'),
        (N'Xem tất cả', 'View all', N'Dung lượng SSD'),
        ('Samsung', 'Samsung SSD', N'Hãng SSD'),
        ('Wester Digital', 'Western Digital SSD', N'Hãng SSD'),
        ('Kingston', 'Kingston SSD', N'Hãng SSD'),
        ('Corsair', 'Corsair SSD', N'Hãng SSD'),
        ('PNY', 'PNY SSD', N'Hãng SSD'),
        (N'Xem tất cả', 'View all', N'Hãng SSD'),
        ('Sandisk', 'Sandisk memory cards and USB drives', N'Thẻ nhớ / USB'),
        ('Portable hard drives', 'Portable hard drives', N'Ổ cứng di động')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Loa, Micro, Webcam' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 7
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Edifier', 'Edifier speakers', N'Thương hiệu loa'),
        ('Razer', 'Razer speakers', N'Thương hiệu loa'),
        ('Logitech', 'Logitech speakers', N'Thương hiệu loa'),
        ('SoundMax', 'SoundMax speakers', N'Thương hiệu loa'),
        (N'Loa vi tính', 'Computer speakers', N'Kiểu Loa'),
        ('Loa Bluetooth', 'Bluetooth speakers', N'Kiểu Loa'),
        ('Loa Soundbar', 'Soundbar speakers', N'Kiểu Loa'),
        ('Loa mini', 'Mini speakers', N'Kiểu Loa'),
        (N'Sub phụ (Loa trầm)', 'Subwoofer speakers', N'Kiểu Loa'),
        (N'Độ phân giải 4k', '4k resolution webcams', 'Webcam'),
        (N'Độ phân giải Full HD (1080p)', 'Full HD (1080p) resolution webcams', 'Webcam'),
        (N'Độ phân giải 720p', '720p resolution webcams', 'Webcam'),
        ('Micro HyperX', 'HyperX microphones', 'Microphone')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Màn hình' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 8
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('LG', 'LG monitors', N'Hãng sản xuất'),
        ('Asus', 'Asus monitors', N'Hãng sản xuất'),
        ('ViewSonic', 'ViewSonic monitors', N'Hãng sản xuất'),
        ('Dell', 'Dell monitors', N'Hãng sản xuất'),
        ('Gigabyte', 'Gigabyte monitors', N'Hãng sản xuất'),
        ('AOC', 'AOC monitors', N'Hãng sản xuất'),
        ('Acer', 'Acer monitors', N'Hãng sản xuất'),
        ('HKC', 'HKC monitors', N'Hãng sản xuất'),
        (N'Dưới 5 triệu', 'Monitors under 5 million', N'Giá tiền'),
        (N'Từ 5 triệu đến 10 triệu', 'Monitors from 5 to 10 million', N'Giá tiền'),
        (N'Từ 10 triệu đến 20 triệu', 'Monitors from 10 to 20 million', N'Giá tiền'),
        (N'Từ 20 triệu đến 30 triệu', 'Monitors from 20 to 30 million', N'Giá tiền'),
        (N'Trên 30 triệu', 'Monitors over 30 million', N'Giá tiền'),
        (N'Màn hình Full HD', 'Full HD monitors', N'Độ Phân giải'),
        (N'Màn hình 2K 1440p', '2K 1440p monitors', N'Độ Phân giải'),
        (N'Màn hình 4K UHD', '4K UHD monitors', N'Độ Phân giải'),
        (N'Màn hình 6K', '6K monitors', N'Độ Phân giải'),
        ('60Hz', '60Hz monitors', N'Tần số quét'),
        ('75Hz', '75Hz monitors', N'Tần số quét'),
        ('100Hz', '100Hz monitors', N'Tần số quét'),
        ('144Hz', '144Hz monitors', N'Tần số quét'),
        ('240Hz', '240Hz monitors', N'Tần số quét'),
        ('24" Curved', '24" curved monitors', N'Màn hình cong'),
        ('27" Curved', '27" curved monitors', N'Màn hình cong'),
        ('32" Curved', '32" curved monitors', N'Màn hình cong'),
        (N'Trên 32" Curved', 'Over 32" curved monitors', N'Màn hình cong'),
        (N'Màn hình 22"', '22" monitors', N'Kích thước'),
        (N'Màn hình 24"', '24" monitors', N'Kích thước'),
        (N'Màn hình 27"', '27" monitors', N'Kích thước'),
        (N'Màn hình 29"', '29" monitors', N'Kích thước'),
        (N'Màn hình 32"', '32" monitors', N'Kích thước'),
        (N'Màn hình Trên 32"', 'Over 32" monitors', N'Kích thước'),
        (N'Hỗ trợ giá treo (VESA)', 'Monitors with VESA mount support', N'Kích thước'),
        (N'Màn hình đồ họa 24"', '24" graphic design monitors', N'Màn hình đồ họa'),
        (N'Màn hình đồ họa 27"', '27" graphic design monitors', N'Màn hình đồ họa'),
        (N'Màn hình đồ họa 32"', '32" graphic design monitors', N'Màn hình đồ họa'),
        (N'Giá treo màn hình', 'Monitor mounts', N'Phụ kiện màn hình'),
        (N'Phụ kiện dây HDMI,DP,LAN', 'HDMI, DP, LAN cables', N'Phụ kiện màn hình'),
        ('Full HD 1080p', 'Portable monitors with Full HD 1080p', N'Màn hình di động'),
        ('2K 1440p', 'Portable monitors with 2K 1440p', N'Màn hình di động'),
        (N'Có cảm ứng', 'Touchscreen portable monitors', N'Màn hình di động'),
        (N'Màn hình Oled', 'OLED monitors', N'Màn hình Oled')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Bàn phím' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 9
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('AKKO', 'AKKO keyboards', N'Thương hiệu'),
        ('AULA', 'AULA keyboards', N'Thương hiệu'),
        ('Dare-U', 'Dare-U keyboards', N'Thương hiệu'),
        ('Durgod', 'Durgod keyboards', N'Thương hiệu'),
        ('Leobog', 'Leobog keyboards', N'Thương hiệu'),
        ('FL-Esports', 'FL-Esports keyboards', N'Thương hiệu'),
        ('Corsair', 'Corsair keyboards', N'Thương hiệu'),
        ('E-Dra', 'E-Dra keyboards', N'Thương hiệu'),
        ('Cidoo', 'Cidoo keyboards', N'Thương hiệu'),
        ('Machenike', 'Machenike keyboards', N'Thương hiệu'),
        (N'Dưới 1 triệu', 'Keyboards under 1 million', N'Giá tiền'),
        (N'1 triệu - 2 triệu', 'Keyboards from 1 to 2 million', N'Giá tiền'),
        (N'2 triệu - 3 triệu', 'Keyboards from 2 to 3 million', N'Giá tiền'),
        (N'3 triệu - 4 triệu', 'Keyboards from 3 to 4 million', N'Giá tiền'),
        (N'Trên 4 triệu', 'Keyboards over 4 million', N'Giá tiền'),
        ('Bluetooth', 'Bluetooth keyboards', N'Kết nối'),
        ('Wireless', 'Wireless keyboards', N'Kết nối'),
        ('Keycaps', 'Keycaps', N'Phụ kiện bàn phím cơ'),
        ('Dwarf Factory', 'Dwarf Factory keycaps', N'Phụ kiện bàn phím cơ'),
        (N'Kê tay', 'Wrist rests', N'Phụ kiện bàn phím cơ')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Chuột + Lót chuột' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 10
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Logitech', 'Logitech mice', N'Thương hiệu chuột'),
        ('Razer', 'Razer mice', N'Thương hiệu chuột'),
        ('Corsair', 'Corsair mice', N'Thương hiệu chuột'),
        ('Pulsar', 'Pulsar mice', N'Thương hiệu chuột'),
        ('Microsoft', 'Microsoft mice', N'Thương hiệu chuột'),
        ('Dare U', 'Dare U mice', N'Thương hiệu chuột'),
        (N'Dưới 500 nghìn', 'Mice under 500 thousand', N'Chuột theo giá tiền'),
        (N'Từ 500 nghìn - 1 triệu', 'Mice from 500 thousand to 1 million', N'Chuột theo giá tiền'),
        (N'Từ 1 triệu - 2 triệu', 'Mice from 1 to 2 million', N'Chuột theo giá tiền'),
        (N'Trên 2 triệu - 3 triệu', 'Mice from 2 to 3 million', N'Chuột theo giá tiền'),
        (N'Trên 3 triệu', 'Mice over 3 million', N'Chuột theo giá tiền'),
        (N'Chuột chơi game', 'Gaming mice', N'Loại Chuột'),
        (N'Chuột văn phòng', 'Office mice', N'Loại Chuột'),
        ('Logitech Gaming', 'Logitech gaming mice', 'Logitech'),
        (N'Logitech Văn phòng', 'Logitech office mice', 'Logitech'),
        ('GEARVN', 'GEARVN mousepads', N'Thương hiệu lót chuột'),
        ('ASUS', 'ASUS mousepads', N'Thương hiệu lót chuột'),
        ('Steelsery', 'Steelsery mousepads', N'Thương hiệu lót chuột'),
        ('Dare-U', 'Dare-U mousepads', N'Thương hiệu lót chuột'),
        ('Razer', 'Razer mousepads', N'Thương hiệu lót chuột'),
        (N'Mềm', 'Soft mousepads', N'Các loại lót chuột'),
        (N'Cứng', 'Hard mousepads', N'Các loại lót chuột'),
        (N'Dày', 'Thick mousepads', N'Các loại lót chuột'),
        (N'Mỏng', 'Thin mousepads', N'Các loại lót chuột'),
        (N'Viền có led', 'Mousepads with LED edges', N'Các loại lót chuột'),
        (N'Nhỏ', 'Small mousepads', N'Lót chuột theo size'),
        (N'Vừa', 'Medium mousepads', N'Lót chuột theo size'),
        (N'Lớn', 'Large mousepads', N'Lót chuột theo size')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Tai Nghe' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 11
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('ASUS', 'ASUS headphones', N'Thương hiệu tai nghe'),
        ('HyperX', 'HyperX headphones', N'Thương hiệu tai nghe'),
        ('Corsair', 'Corsair headphones', N'Thương hiệu tai nghe'),
        ('Razer', 'Razer headphones', N'Thương hiệu tai nghe'),
        (N'Tai nghe dưới 1 triệu', 'Headphones under 1 million', N'Tai nghe theo giá'),
        (N'Tai nghe 1 triệu đến 2 triệu', 'Headphones from 1 to 2 million', N'Tai nghe theo giá'),
        (N'Tai nghe 2 đến 3 triệu', 'Headphones from 2 to 3 million', N'Tai nghe theo giá'),
        (N'Tai nghe 3 đến 4 triệu', 'Headphones from 3 to 4 million', N'Tai nghe theo giá'),
        (N'Tai nghe trên 4 triệu', 'Headphones over 4 million', N'Tai nghe theo giá'),
        (N'Tai nghe Wireless', 'Wireless headphones', N'Kiểu kết nối'),
        (N'Tai nghe Bluetooth', 'Bluetooth headphones', N'Kiểu kết nối'),
        ('Tai nghe Over-ear', 'Over-ear headphones', N'Kiểu tai nghe'),
        ('Tai nghe Gaming In-ear', 'Gaming in-ear headphones', N'Kiểu tai nghe')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    ssd.Name,
    ssd.Description,
    sl.SubcategoryID
FROM SubSubcategoryData ssd
INNER JOIN SubcategoryLookup sl ON ssd.SubcategoryName = sl.SubcategoryName;

-- 'Ghế - Bàn' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 12
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Corsair', 'Corsair gaming chairs', N'Thương hiệu ghế Gaming'),
        ('Warrior', 'Warrior gaming chairs', N'Thương hiệu ghế Gaming'),
        ('E-DRA', 'E-DRA gaming chairs', N'Thương hiệu ghế Gaming'),
        ('DXRacer', 'DXRacer gaming chairs', N'Thương hiệu ghế Gaming'),
        ('Cougar', 'Cougar gaming chairs', N'Thương hiệu ghế Gaming'),
        ('AKRaing', 'AKRaing gaming chairs', N'Thương hiệu ghế Gaming'),
        ('Warrior', 'Warrior ergonomic chairs', N'Thương hiệu ghế CTH'),
        ('Sihoo', 'Sihoo ergonomic chairs', N'Thương hiệu ghế CTH'),
        ('E-Dra', 'E-Dra ergonomic chairs', N'Thương hiệu ghế CTH'),
        (N'Ghế Công thái học', 'Ergonomic chairs', N'Kiểu ghế'),
        (N'Ghế Gaming', 'Gaming chairs', N'Kiểu ghế'),
        (N'Bàn Gaming DXRacer', 'DXRacer gaming desks', N'Bàn Gaming'),
        (N'Bàn Gaming E-Dra', 'E-Dra gaming desks', N'Bàn Gaming'),
        (N'Bàn Gaming Warrior', 'Warrior gaming desks', N'Bàn Gaming'),
        (N'Bàn CTH Warrior', 'Warrior ergonomic desks', N'Bàn công thái học'),
        (N'Phụ kiện bàn ghế', 'Desk and chair accessory', N'Bàn công thái học'),
        (N'Dưới 5 triệu', 'Chairs and desks under 5 million', N'Giá tiền'),
        (N'Từ 5 đến 10 triệu', 'Chairs and desks from 5 to 10 million', N'Giá tiền'),
        (N'Trên 10 triệu', 'Chairs and desks over 10 million', N'Giá tiền')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    d.Name,
    d.Description,
    s.SubcategoryID
FROM SubSubcategoryData d
INNER JOIN SubcategoryLookup s ON d.SubcategoryName = s.SubcategoryName;

-- 'Phần mềm, mạng' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 13
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Asus', 'Asus networking devices', N'Hãng sản xuất'),
        ('LinkSys', 'LinkSys networking devices', N'Hãng sản xuất'),
        ('TP-LINK', 'TP-LINK networking devices', N'Hãng sản xuất'),
        ('Mercusys', 'Mercusys networking devices', N'Hãng sản xuất'),
        ('Gaming', 'Gaming routers', 'Router Wi-Fi'),
        (N'Phổ thông', 'Standard routers', 'Router Wi-Fi'),
        (N'Xuyên tường', 'Wall-penetrating routers', 'Router Wi-Fi'),
        ('Router Mesh Pack', 'Mesh router packs', 'Router Wi-Fi'),
        ('Router WiFi 5', 'WiFi 5 routers', 'Router Wi-Fi'),
        ('Router WiFi 6', 'WiFi 6 routers', 'Router Wi-Fi'),
        ('Usb WiFi', 'USB WiFi adapters', N'USB Thu sóng - Card mạng'),
        ('Card WiFi', 'WiFi cards', N'USB Thu sóng - Card mạng'),
        (N'Dây cáp mạng', 'Network cables', N'USB Thu sóng - Card mạng'),
        ('Microsoft Office 365', 'Microsoft Office 365', 'Microsoft Office'),
        ('Office Home 2024', 'Office Home 2024', 'Microsoft Office'),
        ('Windows 11 Home', 'Windows 11 Home', 'Microsoft Windows'),
        ('Windows 11 Pro', 'Windows 11 Pro', 'Microsoft Windows')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    d.Name,
    d.Description,
    s.SubcategoryID
FROM SubSubcategoryData d
INNER JOIN SubcategoryLookup s ON d.SubcategoryName = s.SubcategoryName;

-- 'Handheld, Console' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 14
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        ('Rog Ally', 'Rog Ally handheld PCs', 'Handheld PC'),
        ('MSI Claw', 'MSI Claw handheld PCs', 'Handheld PC'),
        ('Legion Go', 'Legion Go handheld PCs', 'Handheld PC'),
        (N'Tay cầm Playstation', 'Playstation controllers', N'Tay cầm'),
        (N'Tay cầm Rapoo', 'Rapoo controllers', N'Tay cầm'),
        (N'Tay cầm DareU', 'DareU controllers', N'Tay cầm'),
        (N'Xem tất cả', 'View all', N'Tay cầm'),
        (N'Vô lăng lái xe, máy bay', 'Steering wheels and flight sticks', N'Vô lăng lái xe, máy bay'),
        (N'Sony PS5 (Máy) chính hãng', 'Official Sony PS5 consoles', 'Sony Playstation'),
        (N'Tay cầm chính hãng', 'Official controllers', 'Sony Playstation')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    d.Name,
    d.Description,
    s.SubcategoryID
FROM SubSubcategoryData d
INNER JOIN SubcategoryLookup s ON d.SubcategoryName = s.SubcategoryName;

-- 'Phụ kiện (Hub, sạc, cáp..)' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 15
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        (N'Hub chuyển đổi', 'Hub adapters', N'Hub, sạc, cáp'),
        (N'Dây cáp', 'Cables', N'Hub, sạc, cáp'),
        (N'Củ sạc', 'Chargers', N'Hub, sạc, cáp'),
        (N'Quạt cầm tay, Quạt mini', 'Handheld and mini fans', N'Quạt cầm tay, Quạt mini')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    d.Name,
    d.Description,
    s.SubcategoryID
FROM SubSubcategoryData d
INNER JOIN SubcategoryLookup s ON d.SubcategoryName = s.SubcategoryName;

-- 'Dịch vụ và thông tin khác' category - Sử dụng CTE đầy đủ
WITH SubcategoryLookup AS (
    SELECT 
        SubcategoryID,
        Name as SubcategoryName,
        CategoryID
    FROM Subcategory
    WHERE CategoryID = 16
),
SubSubcategoryData AS (
    SELECT * FROM (VALUES
        (N'Dịch vụ kỹ thuật tại nhà', 'Home technical services', N'Dịch vụ'),
        (N'Dịch vụ sửa chữa', 'Repair services', N'Dịch vụ'),
        (N'Chính sách & bảng giá thu VGA qua sử dụng', 'Policy and price list for used VGA', N'Chính sách'),
        (N'Chính sách bảo hành', 'Warranty policy', N'Chính sách'),
        (N'Chính sách giao hàng', 'Delivery policy', N'Chính sách'),
        (N'Chính sách đổi trả', 'Return policy', N'Chính sách'),
        (N'Build PC', 'PC building services', 'Build PC')
    ) AS T(Name, Description, SubcategoryName)
)
INSERT INTO SubSubcategory (Name, Description, SubcategoryID)
SELECT 
    d.Name,
    d.Description,
    s.SubcategoryID
FROM SubSubcategoryData d
INNER JOIN SubcategoryLookup s ON d.SubcategoryName = s.SubcategoryName;

-- Ví dụ dữ liệu
INSERT INTO CategoryAttributes (CategoryID, AttributeName) VALUES
(1, 'CPU'),    -- Laptop
(1, 'RAM'),
(1, N'Ổ Cứng'),
(1, N'Màn Hình'),
(1, N'Mainboard'),
(2, 'LED RGB'), -- Chuột
(2, N'Kết nối');
