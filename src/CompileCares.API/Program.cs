using CompileCares.API.Extensions;
using CompileCares.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 🚨 ADD THIS DATABASE CREATION CODE (BEFORE app.Run())
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("🔄 Creating database...");
    db.Database.EnsureCreated();
    Console.WriteLine("✅ Database created!");
}

app.Run();