using Inertia.AspNetCore;

namespace InertiaReact.Middleware;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    public override string? Version(HttpRequest request)
    {
        return "1.0.0";
    }

    public override IDictionary<string, object?> Share(HttpRequest request)
    {
        var shared = base.Share(request);
        
        shared["appName"] = "Inertia React Sample";
        
        // Share flash messages
        if (request.HttpContext.Session.TryGetValue("flash", out var flashBytes))
        {
            var flash = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(flashBytes);
            shared["flash"] = flash;
            request.HttpContext.Session.Remove("flash");
        }
        
        return shared;
    }
}
