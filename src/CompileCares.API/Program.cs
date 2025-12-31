using CompileCares.API.Extensions;
using CompileCares.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services using extension method
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// ========== CONFIGURE PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CompileCares API v1");
        c.OAuthClientId("swagger");
        c.OAuthAppName("CompileCares API");
    });
}

app.UseHttpsRedirection();

// Add CORS
app.UseCors("AllowAll");

// 🚨 MUST BE IN THIS ORDER:
app.UseAuthentication(); // 1. Check authentication
app.UseAuthorization();  // 2. Check authorization

app.MapControllers();

// 🚨 DATABASE CREATION
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("🔄 Creating database...");
    db.Database.EnsureCreated();
    Console.WriteLine("✅ Database created!");
}

app.Run();