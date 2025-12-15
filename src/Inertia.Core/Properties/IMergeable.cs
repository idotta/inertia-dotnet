namespace Inertia.Core.Properties;

/// <summary>
/// Interface for properties that support merging with existing client-side data.
/// </summary>
/// <remarks>
/// Properties implementing this interface can be merged with existing data on the client side
/// instead of replacing it entirely. This is particularly useful for paginated data, infinite scroll,
/// and other scenarios where you want to append or update data rather than replace it.
/// 
/// Merge behavior can be shallow (top-level only) or deep (nested objects).
/// Merge can also be restricted to partial reloads only via <see cref="OnlyOnPartial"/>.
/// </remarks>
public interface IMergeable
{
    /// <summary>
    /// Gets a value indicating whether this property should be merged with existing client data.
    /// </summary>
    /// <returns><c>true</c> if the property should be merged; otherwise, <c>false</c>.</returns>
    bool ShouldMerge();

    /// <summary>
    /// Gets the path within the property data where merging should occur.
    /// </summary>
    /// <returns>
    /// The dot-notation path to the merge location (e.g., "data.items"), 
    /// or <c>null</c> to merge at the root level.
    /// </returns>
    string? GetMergePath();

    /// <summary>
    /// Gets a value indicating whether deep merging should be used.
    /// </summary>
    /// <returns>
    /// <c>true</c> for deep merge (recursively merge nested objects); 
    /// <c>false</c> for shallow merge (top-level only).
    /// </returns>
    bool IsDeepMerge();

    /// <summary>
    /// Gets a value indicating whether merging should only occur during partial reloads.
    /// </summary>
    /// <returns>
    /// <c>true</c> if merge behavior should only apply to partial reloads; 
    /// <c>false</c> to merge on all requests.
    /// </returns>
    bool OnlyOnPartial();
}
