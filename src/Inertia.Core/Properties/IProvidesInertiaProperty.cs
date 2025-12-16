namespace Inertia.Core.Properties;

/// <summary>
/// Interface for objects that provide a single Inertia property value with access to property context.
/// </summary>
/// <typeparam name="TRequest">The type of the HTTP request object in the context.</typeparam>
/// <remarks>
/// This interface allows custom objects to be converted into a value for an Inertia response property.
/// Unlike IProvidesInertiaProperties which provides multiple properties, this interface transforms
/// an object into a single property value within the context of a specific property key.
/// 
/// The PropertyContext provides information about the property being resolved, including its key,
/// all props in the response, and the HTTP request, allowing for context-aware value generation.
/// 
/// This is useful for custom value objects or data transfer objects that need to control
/// how they are serialized based on the context in which they appear.
/// </remarks>
/// <example>
/// <code>
/// public class ConditionalValue : IProvidesInertiaProperty&lt;HttpRequest&gt;
/// {
///     private readonly object _value;
///     
///     public ConditionalValue(object value) => _value = value;
///     
///     public object? ToInertiaProperty(PropertyContext&lt;HttpRequest&gt; context)
///     {
///         // Only include value if user is authenticated
///         return context.Request.HttpContext.User.Identity?.IsAuthenticated == true 
///             ? _value 
///             : null;
///     }
/// }
/// </code>
/// </example>
public interface IProvidesInertiaProperty<TRequest>
{
    /// <summary>
    /// Converts this object into a property value for the Inertia response with access to the property context.
    /// </summary>
    /// <param name="context">The property context providing information about the property being resolved.</param>
    /// <returns>The property value.</returns>
    object? ToInertiaProperty(PropertyContext<TRequest> context);
}
