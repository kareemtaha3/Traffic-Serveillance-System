using backend_dotnet.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. الخدمات الأساسية ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // سواجير عادي بدون إعدادات القفل

// --- 2. إعداد قاعدة البيانات ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- 3. Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// حذفنا UseAuthentication هنا
app.UseAuthorization();

app.MapControllers();

// --- 4. تهيئة البيانات (Seeding) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    context.Database.EnsureCreated();

    if (!context.Cars.Any())
    {
        context.Cars.Add(new backend_dotnet.Models.Car 
        { 
            LicensePlate = "أ ب ج 123", 
            OwnerName = "كريم طه", 
            LicenseExpiration = DateTime.UtcNow.AddMonths(-2) 
        });
        context.SaveChanges();
    }
}

app.Run();