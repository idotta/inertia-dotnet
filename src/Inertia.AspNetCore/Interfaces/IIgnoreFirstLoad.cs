namespace Inertia.AspNetCore;

/// <summary>
/// Marker interface indicating that the property should be excluded from the initial page load.
/// Properties implementing this interface are only included when explicitly requested via partial reloads.
/// </summary>
public interface IIgnoreFirstLoad;
