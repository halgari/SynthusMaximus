using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SynthusMaximus.Patchers;

namespace SynthusMaximus
{
    public static class ServiceExtensions
    {
        public static void AddAllOfInterface<T>(this IServiceCollection collection)
        {
            var types = typeof(ServiceExtensions).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.IsAssignableTo(typeof(T)))
                .Where(t => t != typeof(T));

            foreach (var type in types)
                collection.AddTransient(typeof(T), type);

        }
        
    }
}