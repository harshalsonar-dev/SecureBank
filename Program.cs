using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureBank.BLL.Services;
using SecureBank.DAL.Infrastructure;
using SecureBank.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services to the container
builder.Services.AddControllersWithViews();

// 1. Connection string configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SecureBankDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// 2. Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 3. Register Session Management with distributed memory cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true; // Defense against XSS cookie theft
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict; // Defense against CSRF
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// 4. Anti-Forgery Token Configuration
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".SecureBank.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// 5. Register Custom Classes for Dependency Injection (DI)
// DAL Infrastructure & Repositories
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserRepository>();

// BLL Services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Global Exception Handling: Route uncaught exceptions cleanly to /Home/Error
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // In production portfolio demo mode, handle exceptions cleanly
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware Pipeline: UseSession is called before UseRouting and UseAuthorization
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// Map default route to HomeController which handles routing to Dashboard or Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
