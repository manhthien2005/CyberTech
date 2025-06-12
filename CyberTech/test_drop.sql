USE cybertech;
GO

-- Xóa tất cả foreign key constraints trước
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
              ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;
EXEC sp_executesql @sql;

-- Test DROP TABLE order
-- CẤPP 1: Các bảng junction và child tables
DROP TABLE IF EXISTS Reviews;                    
DROP TABLE IF EXISTS OrderItems;                   
DROP TABLE IF EXISTS CartItems;                  
DROP TABLE IF EXISTS WishlistItems;              
DROP TABLE IF EXISTS ProductAttributeValue;      
DROP TABLE IF EXISTS ProductImages;              
DROP TABLE IF EXISTS VoucherProducts;            

-- CẤPP 2: Các bảng phụ thuộc trung gian và order-related
DROP TABLE IF EXISTS Payments;                   
DROP TABLE IF EXISTS Wishlists;                  

-- CẤPP 3: Orders và Cart system  
DROP TABLE IF EXISTS Orders;                     
DROP TABLE IF EXISTS Carts;                      

-- CẤPP 4: Các bảng phụ thuộc Users
DROP TABLE IF EXISTS UserAddresses;              
DROP TABLE IF EXISTS PasswordResetTokens;        
DROP TABLE IF EXISTS UserAuthMethods;            

-- CẤPP 5: Products
DROP TABLE IF EXISTS Products;                   

-- CẤPP 6: Category hierarchy
DROP TABLE IF EXISTS SubSubcategory;             
DROP TABLE IF EXISTS Subcategory;                
DROP TABLE IF EXISTS CategoryAttributes;         

-- CẤPP 7: Attribute system
DROP TABLE IF EXISTS AttributeValue;             

-- CẤPP 8: Users
DROP TABLE IF EXISTS Users;                      

-- CẤPP 9: Các bảng gốc
DROP TABLE IF EXISTS Category;                   
DROP TABLE IF EXISTS ProductAttribute;           
DROP TABLE IF EXISTS Vouchers;                   
DROP TABLE IF EXISTS Ranks;
DROP TABLE IF EXISTS UserVouchers;
DROP TABLE IF EXISTS VoucherTokens;
DROP TABLE IF EXISTS ProductStockNotifications;

PRINT 'All tables dropped successfully!'; 