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
    PaymentMethod   NVARCHAR(50)                    NOT NULL CHECK (PaymentMethod IN ('COD', 'VNPay', 'Momo')),
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
CREATE TABLE UserVerifyTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(128) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ExpiresAt DATETIME NOT NULL,
    VerifiedAt DATETIME NULL,
    IsUsed BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_UserVerifyTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
