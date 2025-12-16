using Inertia.AspNetCore;
using Inertia.Core;
using Microsoft.AspNetCore.Mvc;

namespace InertiaReact.Controllers;

public class HomeController : Controller
{
    private readonly IInertia _inertia;

    public HomeController(IInertia inertia)
    {
        _inertia = inertia;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _inertia.RenderAsync("Home/Index", new Dictionary<string, object?>
        {
            ["message"] = "Welcome to Inertia React Sample!",
            ["features"] = new[]
            {
                "Multiple pages with navigation",
                "Forms and validation",
                "Partial reloads",
                "Property types (Optional, Defer, Always)",
                "Flash messages",
                "Shared layouts"
            }
        });
        
        return response.ToActionResult();
    }

    public async Task<IActionResult> About()
    {
        var response = await _inertia.RenderAsync("Home/About", new Dictionary<string, object?>
        {
            ["title"] = "About This Sample",
            ["description"] = "This is a comprehensive example of using Inertia.js with ASP.NET Core and React."
        });
        
        return response.ToActionResult();
    }
}
