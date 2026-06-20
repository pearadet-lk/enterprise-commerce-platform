FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EnterpriseCommercePlatform.sln ./
COPY src/backend/BuildingBlocks/SharedKernel/SharedKernel.csproj src/backend/BuildingBlocks/SharedKernel/
COPY src/backend/BuildingBlocks/Contracts/Contracts.csproj src/backend/BuildingBlocks/Contracts/
COPY src/backend/BuildingBlocks/Common/Common.csproj src/backend/BuildingBlocks/Common/
COPY src/backend/Identity/IdentityServer.Host/IdentityServer.Host.csproj src/backend/Identity/IdentityServer.Host/

RUN dotnet restore src/backend/Identity/IdentityServer.Host/IdentityServer.Host.csproj

COPY src/backend/ src/backend/
RUN dotnet publish src/backend/Identity/IdentityServer.Host/IdentityServer.Host.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IdentityServer.Host.dll"]
