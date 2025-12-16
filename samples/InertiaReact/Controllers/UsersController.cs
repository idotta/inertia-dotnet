using Inertia.AspNetCore;
using Inertia.Core;
using Inertia.Core.Properties;
using InertiaReact.Data;
using InertiaReact.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InertiaReact.Controllers;

[Route("users")]
public class UsersController : Controller
{
    private readonly IInertia _inertia;
    private readonly SampleDataService _dataService;

    public UsersController(IInertia inertia, SampleDataService dataService)
    {
        _inertia = inertia;
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _inertia.RenderAsync("Users/Index", new Dictionary<string, object?>
        {
            ["users"] = _dataService.GetUsers(),
            // Demonstrate optional prop - only loaded when requested
            ["stats"] = new OptionalProp(async () =>
            {
                await Task.Delay(100); // Simulate work
                return new { TotalUsers = _dataService.GetUsers().Count, ActiveToday = 2 };
            })
        });
        
        return response.ToActionResult();
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var response = await _inertia.RenderAsync("Users/Create", new Dictionary<string, object?>());
        return response.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromForm] User user)
    {
        if (!ModelState.IsValid)
        {
            return await Create();
        }

        _dataService.CreateUser(user);
        
        // Set flash message
        var flash = new Dictionary<string, string> { ["success"] = $"User {user.Name} created successfully!" };
        HttpContext.Session.Set("flash", JsonSerializer.SerializeToUtf8Bytes(flash));

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Show(int id)
    {
        var user = _dataService.GetUser(id);
        if (user == null) return NotFound();

        var response = await _inertia.RenderAsync("Users/Show", new Dictionary<string, object?>
        {
            ["user"] = user,
            // Demonstrate always prop - always included even on partial reloads
            ["timestamp"] = new AlwaysProp(DateTime.UtcNow)
        });
        
        return response.ToActionResult();
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = _dataService.GetUser(id);
        if (user == null) return NotFound();

        var response = await _inertia.RenderAsync("Users/Edit", new Dictionary<string, object?>
        {
            ["user"] = user
        });
        
        return response.ToActionResult();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] User user)
    {
        if (!ModelState.IsValid)
        {
            return await Edit(id);
        }

        if (!_dataService.UpdateUser(id, user))
        {
            return NotFound();
        }

        var flash = new Dictionary<string, string> { ["success"] = $"User {user.Name} updated successfully!" };
        HttpContext.Session.Set("flash", JsonSerializer.SerializeToUtf8Bytes(flash));

        return RedirectToAction(nameof(Index));
    }

    [HttpDelete("{id}")]
    public IActionResult Destroy(int id)
    {
        if (!_dataService.DeleteUser(id))
        {
            return NotFound();
        }

        var flash = new Dictionary<string, string> { ["success"] = "User deleted successfully!" };
        HttpContext.Session.Set("flash", JsonSerializer.SerializeToUtf8Bytes(flash));

        return RedirectToAction(nameof(Index));
    }
}
