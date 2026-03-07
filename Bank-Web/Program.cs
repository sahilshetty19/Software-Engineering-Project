using Bank.Web.Data;
using Microsoft.EntityFrameworkCore;
using Bank.Web.Services;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<BankDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("BankDb"));
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(Console.WriteLine, LogLevel.Information);
});
//builder.Services.AddDbContext<BankDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("BankDb")));
builder.Services.AddHttpClient("CKYC", client =>
{
    var baseUrl = builder.Configuration["CKYC:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
});
builder.Services.AddScoped<CkycApiClient>();
builder.Services.AddScoped<OcrService>();
builder.Services.AddSingleton<PscOcrService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader() .AllowAnyHeader() .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("ReactDev");
app.UseAuthorization();
app.MapControllers();

// ✅ keep this for MVC views like /KycUpload/Create
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Admin@123"));

app.Run();
