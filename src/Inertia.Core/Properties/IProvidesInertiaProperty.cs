namespace Inertia.Core.Properties;

/// <summary>
/// Interface for objects that provide a single Inertia property with a custom key.
/// </summary>
/// <remarks>
/// This interface allows custom objects to be converted into a single named property
/// in an Inertia response. The key returned by <see cref="GetKey"/> will be used as
/// the property name, and the value from <see cref="GetValue"/> will be the property value.
/// 
/// This is useful for custom value objects or data transfer objects that need to control
/// how they are serialized and named in the Inertia response.
/// </remarks>
/// <example>
/// <code>
/// public class CurrentDate : IProvidesInertiaProperty
/// {
///     public string GetKey() => "currentDate";
///     public object GetValue() => DateTime.Now.ToString("yyyy-MM-dd");
/// }
/// </code>
/// </example>
public interface IProvidesInertiaProperty
{
    /// <summary>
    /// Gets the property key (name) that should be used in the Inertia response.
    /// </summary>
    /// <returns>The property key as a string.</returns>
    string GetKey();

    /// <summary>
    /// Gets the property value that should be included in the Inertia response.
    /// </summary>
    /// <returns>The property value.</returns>
    object? GetValue();
}
