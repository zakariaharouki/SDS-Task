using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Repositories;

namespace SchemeTracker.Tests.Support;

public static class TestServiceExtensions
{
    /// <summary>Stops the API trying to reach MongoDB at startup, for tests that don't use it.</summary>
    public static void RemoveIndexInitializer(this IServiceCollection services) =>
        services.Remove(services.Single(service => service.ImplementationType == typeof(SchemeIndexInitializer)));
}
