FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY UrlShortener/UrlShortener.csproj UrlShortener/
RUN dotnet restore UrlShortener/UrlShortener.csproj
COPY . .
RUN dotnet publish UrlShortener/UrlShortener.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UrlShortener.dll"]