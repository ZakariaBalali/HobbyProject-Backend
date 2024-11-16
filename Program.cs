var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")  // Allow requests from your frontend (React app)
               .AllowAnyMethod()                      // Allow any HTTP method (GET, POST, etc.)
               .AllowAnyHeader();                     // Allow any headers
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS before Authorization and other middleware
app.UseCors("AllowMyApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
