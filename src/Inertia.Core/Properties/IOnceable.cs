namespace Inertia.Core.Properties;

/// <summary>
/// Interface for properties that should be resolved once and cached across navigations.
/// </summary>
/// <remarks>
/// Properties implementing this interface are resolved once per session and then reused
/// for subsequent navigations. This is useful for expensive operations or data that doesn't
/// change frequently, such as translations, configuration, or user permissions.
/// 
/// The once resolution is tracked using the render context and session storage.
/// </remarks>
public interface IOnceable
{
    /// <summary>
    /// Gets a value indicating whether this property should use once resolution semantics.
    /// </summary>
    /// <returns><c>true</c> if this property should be resolved once and cached; otherwise, <c>false</c>.</returns>
    bool IsOnce();
}
