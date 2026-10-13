using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using HotelApi.Data;
using HotelApi.Middleware;
using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- JWT -----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

// ----- Services -----
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStaffReservationService, StaffReservationService>();
builder.Services.AddSingleton<IBookingLock, BookingLock>();   // Singleton: một khóa dùng chung cho MỌI request
builder.Services.AddHostedService<HoldCleanupService>();      // tác vụ nền: dọn booking hết hạn giữ phòng
builder.Services.AddHostedService<BackupService>();           // tác vụ nền: sao lưu định kỳ
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPricingService, PricingService>();


var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Tự áp migration và tạo tài khoản mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    db.Database.Migrate();
    DbSeeder.SeedUsers(db, hasher);
    if (app.Environment.IsDevelopment())
        DbSeeder.SeedDemoActivity(db);   // chỉ tạo dữ liệu mẫu khi đang phát triển
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Dùng khi phát triển/demo: không cho trình duyệt giữ bản cũ của file HTML/JS
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
    }
});

if (!app.Environment.IsDevelopment())
    app.UseHsts();   // bảo trình duyệt luôn dùng HTTPS với trang này (NFR-SEC-005)

app.UseHttpsRedirection();
app.UseAuthentication();   // "bạn là ai?" PHẢI đứng trước
app.UseAuthorization();    // "bạn được làm gì?"
app.MapControllers();

app.Run();