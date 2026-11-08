using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortner.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(ConnectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shorturl", async (UrlDto url, ApiDbContext dbContext, HttpContext ctx) =>
{
    // Validate the URL
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
    {
        return Results.BadRequest("Invalid URL");
    }
    // Creating a short version of the provided URL
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var randomStr = new String(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());

    var shortUrl = new UrlManagement
    {
        Url = url.Url,
        ShortUrl = randomStr
    };

    dbContext.Urls.Add(shortUrl);
    await dbContext.SaveChangesAsync();

    // construt the new URL based on the current request's scheme and host

    var resutlUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{shortUrl.ShortUrl}";

    return Results.Ok(new UrlShortResponseDto
    {
        Url = resutlUrl
    });
});

app.MapFallback(async (ApiDbContext dbContext, IDistributedCache cache, HttpContext ctx, ILogger<Program> logger) =>
{
    var sw = Stopwatch.StartNew();
    var path = ctx.Request.Path.ToUriComponent().Trim('/');

    // 1. جرّب الـ Cache
    var cachedUrl = await cache.GetStringAsync(path);
    if (cachedUrl is not null)
    {
        sw.Stop();
        ctx.Response.Headers["X-Cache"] = "HIT";
        ctx.Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
        logger.LogInformation(
            "✅ CACHE HIT | /{Code} -> {OriginalUrl} | {ElapsedMs} ms",
            path, cachedUrl, sw.Elapsed.TotalMilliseconds);
        return Results.Redirect(cachedUrl);
    }

    // 2. Cache miss: اسأل قاعدة البيانات
    var urlMatch = await dbContext.Urls.FirstOrDefaultAsync(x => x.ShortUrl == path);
    if (urlMatch is null)
    {
        sw.Stop();
        logger.LogWarning("⚠️ NOT FOUND | /{Code} | {ElapsedMs} ms", path, sw.Elapsed.TotalMilliseconds);
        return Results.NotFound("Short URL not found");
    }

    // 3. خزّنه في الـ Cache
    await cache.SetStringAsync(path, urlMatch.Url, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    });

    sw.Stop();
    ctx.Response.Headers["X-Cache"] = "MISS";
    ctx.Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
    logger.LogInformation(
        "❌ CACHE MISS (DB) | /{Code} -> {OriginalUrl} | {ElapsedMs} ms",
        path, urlMatch.Url, sw.Elapsed.TotalMilliseconds);
    return Results.Redirect(urlMatch.Url);
});



app.Run();

class ApiDbContext : DbContext
{
    public virtual DbSet<UrlManagement> Urls { get; set; } = null!;
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {

    }
}