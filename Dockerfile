FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép toàn bộ source code trước
COPY . .

# Xóa các thư mục có thể gây ra vấn đề
RUN rm -rf /src/CyberTech/bin /src/CyberTech/obj

# Cấu hình NuGet
RUN dotnet nuget locals all --clear

# Restore với cờ bảo mật thấp hơn
WORKDIR "/src/CyberTech"
RUN dotnet restore "CyberTechShop.csproj" --disable-parallel --no-cache --force

# Build
RUN dotnet build "CyberTechShop.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CyberTechShop.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CyberTechShop.dll"] 