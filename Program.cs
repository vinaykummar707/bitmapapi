var builder = WebApplication.CreateBuilder(args);

// Add CORS services
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
var app = builder.Build();

// Use CORS middleware
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();
app.Run();
