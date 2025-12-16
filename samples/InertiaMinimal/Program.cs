using Inertia.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Configure Inertia
builder.Services.AddInertia(options =>
{
    options.RootView = "app";
});

var app = builder.Build();

// Configure middleware pipeline
app.UseStaticFiles();
app.UseRouting();

// Add Inertia middleware
app.UseInertia<InertiaMinimal.Middleware.HandleInertiaRequests>();

app.MapControllers();
app.Run();
