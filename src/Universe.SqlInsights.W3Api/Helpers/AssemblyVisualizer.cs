using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Universe.SqlInsights.W3Api.Helpers
{
    public class AssemblyVisualizer
    {

        static string GetLocation(Assembly a)
        {
            if (a.IsDynamic) return ">dynamic<";
            return a.Location;
        }

        public static void Subscribe()
        {
            AppDomain.CurrentDomain.AssemblyLoad += delegate(object sender, AssemblyLoadEventArgs args)
            {
                
                var assembly = args.LoadedAssembly;
                
                Console.WriteLine($"LOADING ASSEMBLY: {TryEval(() => assembly.GetName().Version),-14} {TryEval(() => GetLocation(assembly))}, NET {TryEval(() => assembly.ImageRuntimeVersion)}");
            };
        }

        static T TryEval<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch
            {
                return default;
            }
        }

        public static void Show(string caption)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName).ToList();
            StringBuilder ret = new StringBuilder();

            string format = new string('0', 1 + (int)Math.Log10(assemblies.Count));
            int n = 0;
            foreach (var assembly in assemblies)
            {
                n++;
                ret.AppendLine($"{n.ToString(format)}) {assembly.GetName().Version,-12} {GetLocation(assembly)}, NET {assembly.ImageRuntimeVersion}");
            }

            Console.WriteLine(caption + Environment.NewLine + ret);
        }


    }
}
