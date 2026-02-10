using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Playwright;
using System.Net;
using YP_API.Data;
using YP_API.Interfaces;
using YP_API.Repositories;
using YP_API.Services;

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Recipe Planner API",
        Version = "v1",
        Description = "API for managing recipes, menus, shopping lists and ingredients"
    });
});



var connectionString = "Server=127.0.0.1;Port=3306;Database=recipe_planner;Uid=root;Pwd=;";

builder.Services.AddDbContext<RecipePlannerContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuService, MenuService>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IPovarScraperService, PovarScraperService>();
builder.Services.AddScoped<IImageGenerationService, ImageGenerationService>();


builder.Services.AddSingleton<IPlaywright>(sp => Playwright.CreateAsync().GetAwaiter().GetResult());
builder.Services.AddSingleton<IBrowser>(sp => {
    var playwright = sp.GetRequiredService<IPlaywright>();
    return playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = false,
        Args = new[] {
            "--disable-blink-features=AutomationControlled",
            "--no-sandbox",
            "--start-maximized"
        }
    }).GetAwaiter().GetResult();
});

builder.Services.AddScoped<IPriceParserService, PriceParserService>();

builder.Services.AddHttpClient<IPovarScraperService, PovarScraperService>();

var app = builder.Build();
;

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RecipePlannerContext>();

    try
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    catch (Exception)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE DATABASE IF NOT EXISTS recipe_planner CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await cmd.ExecuteNonQueryAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch { }
    }
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe Planner API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();

app.MapGet("/api/debug/db-status", async (RecipePlannerContext context) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new
        {
            success = true,
            database = "recipe_planner",
            connected = canConnect
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message
        });
    }
});

app.MapGet("/api/debug/create-db", async (RecipePlannerContext context) =>
{
    try
    {
        var created = await context.Database.EnsureCreatedAsync();
        return Results.Ok(new
        {
            success = true,
            created = created
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message
        });
    }
});

app.Run();