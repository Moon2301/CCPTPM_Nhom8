using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UngDungOnThiBangLai.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//  Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//  Đăng ký AppDbContext vào Dependency Injection (DI) container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
}).AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Chạy database seeding khi ứng dụng khởi động
builder.Services.AddScoped<IDataSeedService, DataSeedService>();
builder.Services.AddScoped<IExamService, ExamService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Cho phép đúng cổng của Vite React
                  .AllowAnyHeader()  // Cho phép mọi loại Header (JSON, Text...)
                  .AllowAnyMethod(); // Cho phép mọi phương thức (GET, POST, PUT, DELETE)
        });
});


var app = builder.Build();

// Tạo một scope để lấy dịch vụ IDataSeedService và chạy hàm SeedAllAsync
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<IDataSeedService>();
        // Chạy hàm Seed (vì là async nên dùng .Wait() hoặc gọi trong một Task)
        await seeder.SeedAllAsync();
        Console.WriteLine(">>> Seed dữ liệu thành công!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Lỗi khi Seed dữ liệu: {ex.Message}");
    }
}

app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=AdminLogin}/{id?}");

app.Run();
