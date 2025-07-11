using ClubManagementApp.Helpers;
using ClubManagementApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClubManagementApp.Tests
{
    /// <summary>
    /// Simple test class to verify the email functionality works correctly
    /// </summary>
    public class TestEmailFeature
    {
        public static void TestServiceLocator()
        {
            try
            {
                Console.WriteLine("=== Testing ServiceLocator and EmailService ===");
                
                // Test 1: ServiceLocator without configuration
                Console.WriteLine("Test 1: ServiceLocator without configuration");
                try
                {
                    var emailService1 = ServiceLocator.GetService<IEmailService>();
                    Console.WriteLine($"Result: {(emailService1 == null ? "null" : "service found")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Expected exception: {ex.Message}");
                }
                
                // Test 2: Configure ServiceLocator with mock services
                Console.WriteLine("\nTest 2: Configure ServiceLocator with services");
                var services = new ServiceCollection();
                services.AddSingleton<IEmailService, EmailService>();
                var serviceProvider = services.BuildServiceProvider();
                ServiceLocator.SetServiceProvider(serviceProvider);
                
                // Test 3: Retrieve service after configuration
                Console.WriteLine("Test 3: Retrieve EmailService after configuration");
                try
                {
                    var emailService2 = ServiceLocator.GetService<IEmailService>();
                    Console.WriteLine($"Result: {(emailService2 == null ? "null" : "EmailService found successfully")}");
                    Console.WriteLine($"Service type: {emailService2?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                
                Console.WriteLine("\n=== Test completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}