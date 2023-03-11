using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Universe.SqlInsights.W3Api.Helpers
{
    public class AssemblyVisualizer
    {

        static string GetFileName(Assembly a)
        {
            return a.IsDynamic ? ">dynamic<" : Path.GetFileName(GetLocation(a));
        }
        static string GetDirName(Assembly a)
        {
            return a.IsDynamic ? "" : Path.GetDirectoryName(GetLocation(a));
        }


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
                Console.WriteLine($"LOADING ASSEMBLY: {TryEval(() => assembly.GetName().Version),-12} {GetFileName(assembly)} <- {GetDirName(assembly)}");
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
            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .OrderBy(x => GetFileName(x))
                .ThenBy(x => GetDirName(x))
                .ToList();

            StringBuilder ret = new StringBuilder();

            string format = new string('0', 1 + (int)Math.Log10(assemblies.Count));
            int n = 0;
            foreach (var assembly in assemblies)
            {
                n++;
                ret.AppendLine($"{n.ToString(format)}) {TryEval(() => assembly.GetName().Version),-12} {GetFileName(assembly)} <- {GetDirName(assembly)}");
            }

            Console.WriteLine(caption + Environment.NewLine + ret);
        }


    }
}
