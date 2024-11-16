using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Set up Serilog logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()  // Log to console
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)  // Log to file with rolling
    .CreateLogger();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy for localhost
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Update with your frontend's local address and port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-very-strong-secret-key-that-is-long-enough")), // Replace with your secret key
            ValidIssuer = "your-app", // Set your issuer
            ValidAudience = "your-audience" // Set your audience
        };
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bearer", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog();  // Add Serilog as the logging provider
});

var app = builder.Build();

// Add the logic to download the JSON file during application startup
await DownloadFileAsync("https://raw.githubusercontent.com/supermarkt/checkjebon/main/data/supermarkets.json", "supermarkets.json", app.Services.GetRequiredService<ILogger<Program>>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use the CORS policy
app.UseCors("AllowLocalhost");

// Use authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Method to download the file
async Task DownloadFileAsync(string fileUrl, string savePath, ILogger<Program> logger)
{
    try
    {
        using (var httpClient = new HttpClient())
        {
            // Make a GET request to fetch the raw content of the file from GitHub
            var fileContent = await httpClient.GetStringAsync(fileUrl);

            // Save the content to the specified local path
            await File.WriteAllTextAsync(savePath, fileContent);
            logger.LogInformation("File downloaded successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"Error downloading file: {ex.Message}");
    }
}
