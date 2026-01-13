using Inertia.AspNetCore;

namespace InertiaMinimal.Middleware;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    public override string? Version(HttpRequest request)
    {
        return "1.0.0";
    }

    public override IDictionary<string, object?> Share(HttpRequest request)
    {
        var shared = base.Share(request);
        shared["appName"] = "Inertia Minimal";
        return shared;
    }
}
