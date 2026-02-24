using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TalkToMe.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services)
        {
            try
            {
                var assembly = Assembly.GetAssembly(typeof(ServiceRegistration));
                if (assembly == null) throw new InvalidOperationException("Assembly not found.");

                var serviceTypes = assembly.GetTypes().Where(t =>
                    t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase) &&
                    t.IsClass &&
                    !t.IsAbstract &&
                    !typeof(BackgroundService).IsAssignableFrom(t));

                foreach (var serviceType in serviceTypes)
                {
                    var expectedInterfaceName = $"I{serviceType.Name}";
                    var interfaceType = serviceType.GetInterfaces()
                        .FirstOrDefault(i => i.Name.Equals(expectedInterfaceName, StringComparison.OrdinalIgnoreCase));

                    if (interfaceType != null)
                    {
                        services.AddScoped(interfaceType, serviceType);
                        Console.WriteLine($"Registered SCOPED: {interfaceType.Name} -> {serviceType.Name}");
                    }
                    else
                    {
                        services.AddScoped(serviceType);
                        Console.WriteLine($"Registered SCOPED: {serviceType.Name} (Self)");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("AddDataServices", ex.ToString());
                Console.WriteLine($"Reflection Error: {ex.Message}");
            }

            return services;
        }

        private static void LogError(string fn, string msg)
        {
            LoggerHelper.WriteLog($"ServiceRegistration -> {fn}", msg);
        }
    }
}
