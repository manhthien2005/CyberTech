@echo off
echo === Building and publishing CyberTechShop ===

echo Cleaning previous build...
rmdir /s /q publish 2>nul
mkdir publish

echo Restoring packages...
dotnet restore CyberTech/CyberTechShop.csproj

echo Building and publishing...
dotnet publish CyberTech/CyberTechShop.csproj -c Release -o publish

echo Done! Check the publish folder.
echo You can now deploy to Railway using Dockerfile.simple
pause 