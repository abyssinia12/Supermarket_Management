using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Services;

var builder = WebApplication.CreateBuilder(args);

// Ensure local keys directory for DataProtection
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
if (!Directory.Exists(keysFolder)) Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

// Register your DbContext (adjust connection string name as needed)
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";         // Redirect here if unauthenticated
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect here if unauthorized
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

//var app = builder.Build();

// Configure Cookie Authentication

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

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

//app.UseAuthorization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
