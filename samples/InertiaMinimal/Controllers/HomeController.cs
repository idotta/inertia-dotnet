using Inertia.AspNetCore;
using Inertia.Core;
using Microsoft.AspNetCore.Mvc;

namespace InertiaMinimal.Controllers;

[Route("/")]
public class HomeController : Controller
{
    private readonly IInertia _inertia;

    public HomeController(IInertia inertia)
    {
        _inertia = inertia;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _inertia.RenderAsync("Home", new Dictionary<string, object?>
        {
            ["message"] = "Welcome to Inertia.js with .NET!",
            ["timestamp"] = DateTime.UtcNow
        });
        
        return response.ToActionResult();
    }
}
