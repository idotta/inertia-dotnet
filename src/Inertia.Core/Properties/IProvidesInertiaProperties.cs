namespace Inertia.Core.Properties;

/// <summary>
/// Interface for objects that provide multiple Inertia properties.
/// </summary>
/// <remarks>
/// This interface allows custom objects to contribute multiple properties to an Inertia response.
/// When an object implementing this interface is passed as a prop, it will be expanded into
/// multiple properties using the dictionary returned by <see cref="ToInertiaProperties"/>.
/// 
/// The RenderContext provides information about the current render operation, such as the component
/// being rendered and the HTTP request, allowing for context-aware property generation.
/// 
/// This is useful for view models, resource collections, or any object that represents
/// multiple related properties that should be exposed to the client.
/// </remarks>
/// <example>
/// <code>
/// public class UserViewModel : IProvidesInertiaProperties
/// {
///     public string Name { get; set; }
///     public string Email { get; set; }
///     
///     public Dictionary&lt;string, object?&gt; ToInertiaProperties(RenderContext context)
///     {
///         return new Dictionary&lt;string, object?&gt;
///         {
///             ["userName"] = Name,
///             ["userEmail"] = Email,
///             ["requestPath"] = context.Request.Path
///         };
///     }
/// }
/// </code>
/// </example>
public interface IProvidesInertiaProperties
{
    /// <summary>
    /// Converts this object into a dictionary of Inertia properties with access to the render context.
    /// </summary>
    /// <param name="context">The render context providing information about the current render operation.</param>
    /// <returns>A dictionary where keys are property names and values are the property values.</returns>
    Dictionary<string, object?> ToInertiaProperties(object context);
}
