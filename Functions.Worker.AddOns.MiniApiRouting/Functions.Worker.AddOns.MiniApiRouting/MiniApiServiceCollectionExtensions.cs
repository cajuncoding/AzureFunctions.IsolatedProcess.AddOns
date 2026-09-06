using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Functions.Worker.AddOns.MiniApiRouting;

public static class MiniApiServiceCollectionExtensions
{
    private static readonly object _syncRootLocker = new();
    private static Func<IServiceCollection, IServiceCollection>? _generatedRegistration;

    public static IServiceCollection AddFunctionsMiniApiRouting(this IServiceCollection services)
    {
        services.TryAddSingleton<IMiniApiRequestBodyDeserializer, MiniApiJsonRequestBodyDeserializer>();
        _ = _generatedRegistration?.Invoke(services);
        return services;
    }

    public static void RegisterGeneratedMiniApiRouting(Func<IServiceCollection, IServiceCollection> registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        lock (_syncRootLocker)
            _generatedRegistration += registration;
    }
}
