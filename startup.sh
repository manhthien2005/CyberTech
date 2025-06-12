#!/bin/bash

# Hiển thị thông tin môi trường
echo "Starting application..."
echo "ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "PORT: $PORT"

# Đảm bảo database đã được migrate
# Bỏ comment dòng dưới nếu cần migrate database
# dotnet ef database update --project CyberTech/CyberTechShop.csproj

# Khởi động ứng dụng
cd /app
dotnet CyberTechShop.dll 