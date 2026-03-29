using Microsoft.AspNetCore.Http;

namespace Inertia.AspNetCore;

/// <summary>Non-generic marker for prop types. Used by PropsResolver for type dispatch.</summary>
internal interface IResolvableProp
{
    /// <summary>Resolves the property value, boxing to object. Called by PropsResolver.</summary>
    Task<object?> ResolveAsObjectAsync();
}

/// <summary>Generic contract for prop types that resolve their wrapped value without boxing.</summary>
/// <typeparam name="T">The type of the resolved value.</typeparam>
internal interface IResolvableProp<T> : IResolvableProp
{
    /// <summary>Resolves the property value with its original type preserved.</summary>
    Task<T> ResolveAsync();
}

/// <summary>Marker interface for AlwaysProp — always included in partial responses.</summary>
internal interface IAlwaysProp;

/// <summary>Internal contract for ScrollProp configuration and metadata access.</summary>
internal interface IScrollPropInternal
{
    /// <summary>Configures merge intent based on the infinite scroll merge intent header.</summary>
    void ConfigureMergeIntent(HttpRequest? request);

    /// <summary>Returns scroll metadata as a dictionary.</summary>
    IDictionary<string, object?> Metadata();
}
