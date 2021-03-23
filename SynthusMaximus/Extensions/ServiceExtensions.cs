using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SynthusMaximus.Patchers;

namespace SynthusMaximus
{
    public static class ServiceExtensions
    {
        public static void AddPatchers(this IServiceCollection collection)
        {
            var types = typeof(ServiceExtensions).Assembly
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(IPatcher)))
                .Where(t => t != typeof(IPatcher));

            foreach (var type in types)
                collection.AddTransient(typeof(IPatcher), type);

        }
        
    }
}