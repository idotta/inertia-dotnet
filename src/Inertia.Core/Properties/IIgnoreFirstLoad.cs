namespace Inertia.Core.Properties;

/// <summary>
/// Marker interface for properties that should be ignored on the first (non-partial) page load.
/// Properties implementing this interface are only included when explicitly requested via partial reloads.
/// </summary>
/// <remarks>
/// This interface is typically implemented by OptionalProp (formerly LazyProp).
/// When a property implements this interface, it will not be included in the initial page load
/// but will be loaded when the client explicitly requests it via the X-Inertia-Partial-Data header.
/// </remarks>
public interface IIgnoreFirstLoad
{
    // Marker interface - no methods
}
