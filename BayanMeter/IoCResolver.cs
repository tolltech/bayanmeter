using System;
using System.Linq;

namespace Tolltech.BayanMeter
{
    public static class IoCResolver
    {
        public static void Resolve(Action<Type, Type> resolve, params string[] assmeblyNames)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => assmeblyNames.Any(y => x.FullName.StartsWith(y + "."))).ToArray();
            var interfaces = assemblies.SelectMany(x => x.GetTypes().Where(y => y.IsInterface)).ToArray();
            var types = assemblies.SelectMany(x => x.GetTypes().Where(y => !y.IsInterface && y.IsClass && !y.IsAbstract)).ToArray();
            foreach (var @interface in interfaces)
            {
                var realisations = types.Where(x => @interface.IsAssignableFrom(x)).ToArray();
                foreach (var realisation in realisations)
                {
                    resolve(@interface, realisation);
                }
            }
        }
    }}