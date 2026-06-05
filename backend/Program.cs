using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ContactFormApi.Models;
using ContactFormApi.Validators;
using ContactFormApi.Data;
using ContactFormApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Contact Form API", Version = "v1" });
});

builder.Services.AddScoped<IValidator<ContactFormModel>, ContactFormValidator>();

// ── Blob Storage ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<BlobService>();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowedToAllowWildcardSubdomains());
});

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// Drop and recreate database to apply new schema (AttachmentUrl, AttachmentName columns)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        db.Database.EnsureDeleted();   // ← drop old schema
        db.Database.EnsureCreated();   // ← recreate with new columns
        log.LogInformation("Database schema recreated successfully.");
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "Database initialization failed — app will continue.");
    }
}

// Enable Swagger in all environments for debugging
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AngularPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
