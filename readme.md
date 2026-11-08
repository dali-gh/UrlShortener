# UrlShortener

A lightweight URL shortener API built with **ASP.NET Core Minimal APIs**, **Entity Framework Core**, and **SQLite**.

## Features
- Shorten any valid URL into an 8-character code
- Redirect short links to the original URL
- SQLite storage with EF Core migrations
- Swagger UI for testing in development

## Tech Stack
- C# / .NET
- ASP.NET Core Minimal APIs
- Entity Framework Core (SQLite provider)
- Swagger / OpenAPI

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download)
- EF Core tools: `dotnet tool install --global dotnet-ef`

### Run locally
```bash
git clone https://github.com/USERNAME/UrlShortener.git
cd UrlShortener/UrlShortener
dotnet ef database update
dotnet run
```

Then open Swagger UI at `https://localhost:<port>/swagger`.

## API

### Create a short URL
`POST /shorturl

Request body:
```json
{
  "url": "https://www.example.com/some/very/long/path"
}
```

Response:
```json
{
  "url": "https://localhost:7000/aB3dE9xZ"
}
```

### Redirect
`GET /{shortCode}` redirects to the original URL, or returns `404` if the code does not exist.

## Configuration
The SQLite connection string is set in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=app.db"
}
```

## Project Structure
```
UrlShortener/
├── Migrations/          # EF Core migrations
├── Models/
│   ├── UrlDto.cs              # Incoming request model
│   ├── UrlManagement.cs       # Database entity
│   └── UrlShortResponseDto.cs # Response model
├── Program.cs           # Endpoints and app configuration
└── UrlShortener.http    # Sample requests
```

## License
MIT