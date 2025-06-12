FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép nuget.config trước
COPY ["nuget.config", "."]

# Sao chép csproj và restore
COPY ["CyberTech/CyberTechShop.csproj", "CyberTech/"]
RUN dotnet restore "CyberTech/CyberTechShop.csproj" --disable-parallel

# Sao chép toàn bộ source code
COPY . .

# Buộc restore lại trước khi build
WORKDIR "/src/CyberTech"
RUN dotnet restore --force
RUN dotnet build "CyberTechShop.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "CyberTechShop.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CyberTechShop.dll"] 