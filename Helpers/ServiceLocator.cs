using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClubManagementApp.Helpers
{
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T? GetService<T>()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider has not been set. Call SetServiceProvider first.");
            }

            try
            {
                return _serviceProvider.GetService<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServiceLocator] Error getting service {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider has not been set. Call SetServiceProvider first.");
            }

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}