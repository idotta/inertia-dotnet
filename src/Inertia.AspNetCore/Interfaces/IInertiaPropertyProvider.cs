namespace Inertia.AspNetCore;

/// <summary>
/// Allows objects to dynamically provide a collection of properties for an Inertia page response.
/// </summary>
public interface IInertiaPropertyProvider
{
    /// <summary>
    /// Provides a collection of key-value pairs to be included in the Inertia page response.
    /// </summary>
    /// <param name="context">The render context for the current Inertia response.</param>
    /// <returns>An enumerable of property key-value pairs.</returns>
    IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context);
}
