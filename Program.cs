using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core.Interfaces;
using PeakOps.Core.Services;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PeakOps.Core.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); 
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Add_Edit_User", policy => policy.Requirements.Add(new PermissionRequirement("Add_Edit_User")));
    options.AddPolicy("Delete_User", policy => policy.Requirements.Add(new PermissionRequirement("Delete_User")));
    options.AddPolicy("View_User", policy => policy.Requirements.Add(new PermissionRequirement("View_User")));
    options.AddPolicy("Manage_Users", policy => policy.Requirements.Add(new PermissionRequirement("Manage_Users")));
    options.AddPolicy("MANAGE_ROLES_PERMISSIONS", policy => policy.Requirements.Add(new PermissionRequirement("MANAGE_ROLES_PERMISSIONS")));
});

builder.Services.AddScoped<PeakOps.Core.Interfaces.IAuthService, PeakOps.Core.Services.AuthService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<PermissionService>();






builder.Services.AddControllersWithViews(options =>
{
   
  
});
var app = builder.Build();


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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
