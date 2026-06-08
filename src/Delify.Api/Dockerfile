FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Delify.Api/Delify.Api.csproj", "src/Delify.Api/"]
COPY ["src/Delify.Shared/Delify.Shared.csproj", "src/Delify.Shared/"]
COPY ["src/Delify.Modules.Identity/Delify.Modules.Identity.csproj", "src/Delify.Modules.Identity/"]
COPY ["src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj", "src/Delify.Modules.Catalog/"]
COPY ["src/Delify.Modules.Orders/Delify.Modules.Orders.csproj", "src/Delify.Modules.Orders/"]
COPY ["src/Delify.Modules.Payments/Delify.Modules.Payments.csproj", "src/Delify.Modules.Payments/"]
COPY ["src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj", "src/Delify.Modules.Delivery/"]

RUN dotnet restore "src/Delify.Api/Delify.Api.csproj"

COPY . .
RUN dotnet publish "src/Delify.Api/Delify.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Delify.Api.dll"]
