using DemoIdentityProject.Configuration;
using DemoIdentityProject.Models.Entity;
using DemoIdentityProject.Repository.Context;
using DemoIdentityProject.Services.Interface;
using DemoIdentityProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("SqlConnection");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(
    option => option.UseSqlServer(connectionString));
builder.Services.AddIdentity<User, IdentityRole>(
    option =>
    {
        option.Password.RequiredUniqueChars = 0;
        option.Password.RequireUppercase = false;
        option.Password.RequireLowercase = false;
        option.Password.RequiredLength = 8;
        option.Password.RequireNonAlphanumeric = false;
        option.Password.RequireDigit = false;
        option.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(option =>
{
    option.SignIn.RequireConfirmedEmail = true;
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create default admin user
    var adminEmail = configuration["AdminUser:Email"];
    var adminPassword = configuration["AdminUser:Password"];
    var Name = configuration["AdminUser:Name"];
    var Address = configuration["AdminUser:Address"];
    var adminUser = await userManager.FindByEmailAsync(adminEmail!);
    if (adminUser == null)
    {
        User admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = Name,
            Address = Address,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword!);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

app.Run();
