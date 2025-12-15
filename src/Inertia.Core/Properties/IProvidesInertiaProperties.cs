namespace Inertia.Core.Properties;

/// <summary>
/// Interface for objects that provide multiple Inertia properties.
/// </summary>
/// <remarks>
/// This interface allows custom objects to contribute multiple properties to an Inertia response.
/// When an object implementing this interface is passed as a prop, it will be expanded into
/// multiple properties using the dictionary returned by <see cref="ToInertiaProperties"/>.
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
///     public Dictionary&lt;string, object?&gt; ToInertiaProperties()
///     {
///         return new Dictionary&lt;string, object?&gt;
///         {
///             ["userName"] = Name,
///             ["userEmail"] = Email
///         };
///     }
/// }
/// </code>
/// </example>
public interface IProvidesInertiaProperties
{
    /// <summary>
    /// Converts this object into a dictionary of Inertia properties.
    /// </summary>
    /// <returns>A dictionary where keys are property names and values are the property values.</returns>
    Dictionary<string, object?> ToInertiaProperties();
}
