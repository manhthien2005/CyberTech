#!/bin/bash

# Hiển thị thông tin môi trường
echo "Starting application..."
echo "ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "PORT: $PORT"

# Kiểm tra DATABASE_URL
if [ -z "$DATABASE_URL" ]; then
  echo "WARNING: DATABASE_URL is not set!"
else
  echo "DATABASE_URL is configured"
fi

# Đảm bảo database đã được migrate
# Bỏ comment dòng dưới nếu cần migrate database
# dotnet ef database update --project CyberTech/CyberTechShop.csproj

# Khởi động ứng dụng
cd /app
dotnet CyberTechShop.dll 