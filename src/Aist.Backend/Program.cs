using Aist.Backend.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext - use connection string from config, or fallback to default path
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "aist", "main.db");
    connectionString = $"Data Source={dbPath}";
}

// Ensure the database directory exists using proper connection string parsing
var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
var dbFilePath = connectionStringBuilder.DataSource;
var dbDirectory = Path.GetDirectoryName(dbFilePath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<AistDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AistDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
