namespace Inertia.AspNetCore;

/// <summary>
/// Allows objects to convert themselves to a value suitable for inclusion in an Inertia page response.
/// </summary>
public interface IInertiaPropertyValueProvider
{
    /// <summary>
    /// Converts this instance to a value for the Inertia page response.
    /// </summary>
    /// <param name="context">The property context providing the key and sibling props.</param>
    /// <returns>The resolved property value.</returns>
    object? ToInertiaProperty(PropertyContext context);
}
