using Microsoft.EntityFrameworkCore;
using UrlShortner.Models;

var builder = WebApplication.CreateBuilder(args);

var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(ConnectionString));

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shortner", async (UrlDto url, ApiDbContext dbContext, HttpContext ctx) =>
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

app.MapFallback(async (ApiDbContext dbContext, HttpContext ctx) =>
{
    var path = ctx.Request.Path.ToUriComponent().Trim('/');
    var urlMatch = await dbContext.Urls.FirstOrDefaultAsync(x =>
    x.ShortUrl.ToLower().Trim() == path.ToLower().Trim());

    if (urlMatch == null) { return Results.BadRequest("Invalid Request"); }

    return Results.Redirect(urlMatch.Url);
});



app.Run();

// virtual allows Entity Framework Core to override this property when using proxy-based features, such as lazy loading.
class ApiDbContext : DbContext
{
    public virtual DbSet<UrlManagement> Urls { get; set; } = null!;
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
        
    }
}