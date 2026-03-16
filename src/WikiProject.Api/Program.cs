using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WikiProject.Api.Data;
using WikiProject.Api.DTOs;
using WikiProject.Api.Services;
using WikiProject.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WikiProject API",
        Version = "v1",
        Description = "Internal knowledge base REST API"
    });
});

// EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=wiki.db";
builder.Services.AddDbContext<WikiDbContext>(opts =>
    opts.UseSqlite(connectionString));

// Application services
builder.Services.AddScoped<IArticleService, ArticleService>();

// FluentValidation
builder.Services.AddScoped<IValidator<CreateArticleRequest>, CreateArticleRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateArticleRequest>, UpdateArticleRequestValidator>();

// CORS – allow the React dev server (and any configured origin)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Logging.AddConsole();

// --- App pipeline ---

var app = builder.Build();

// Apply pending migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WikiProject API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();

// Extension point: app.UseAuthentication(); app.UseAuthorization();

app.MapControllers();

app.Run();
