using Microsoft.EntityFrameworkCore;
using QTC_Admin_Application.Filters;
using QTC_Admin_Application.Models;
using QTC_Admin_Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<WorkflowContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<WorkflowStepService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WorkflowReportService>();
builder.Services.AddScoped<AdminAuthorizationFilter>();

// Login Monitor
builder.Services.AddSingleton<LoginMonitorService>();
builder.Services.AddHostedService<LoginMonitorBackgroundService>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
