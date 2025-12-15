namespace Inertia.Core.Properties;

/// <summary>
/// Represents a property that is loaded only when explicitly requested via partial reloads.
/// </summary>
/// <remarks>
/// <para>
/// <strong>DEPRECATED:</strong> This class is deprecated and provided only for backward compatibility.
/// Use <see cref="OptionalProp"/> instead.
/// </para>
/// <para>
/// LazyProp is an alias for OptionalProp. In Inertia.js v2, "lazy" props were renamed to "optional" props
/// to better reflect their purpose. This class simply inherits from OptionalProp to maintain
/// compatibility with code that may reference the older naming convention.
/// </para>
/// </remarks>
[Obsolete("LazyProp is deprecated. Use OptionalProp instead.", false)]
public class LazyProp : OptionalProp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyProp"/> class with a synchronous callback.
    /// </summary>
    /// <param name="callback">The callback function that returns the value.</param>
    public LazyProp(Func<object?> callback) : base(callback)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyProp"/> class with an asynchronous callback.
    /// </summary>
    /// <param name="asyncCallback">The async callback function that returns the value.</param>
    public LazyProp(Func<Task<object?>> asyncCallback) : base(asyncCallback)
    {
    }
}
