using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace Universe.SqlInsights.W3Api.Helpers
{
    public static class AssemblyVisualizer
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

        [Obsolete("Use GetExtendedVersion", true)]
        static string GetVersion(Assembly a)
        {
            var attributes1 = a.GetCustomAttributes().ToArray();
            var attributes2 = a.GetCustomAttributes().OfType<Attribute>().ToArray();
            return "not implemented";
        }


        public static T GetAssemblyAttribute<T>(this Assembly a)
        {
            return a.GetCustomAttributes().OfType<T>().FirstOrDefault();
        }

        static string GetVersionFromFullname(Assembly a)
        {
            var partVersion = a.FullName?
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => x.StartsWith("Version"))
                .FirstOrDefault();

            var ret = partVersion?.Split('=')?[1];
            return ret;
        }

        static string GetExtendedVersion(this Assembly a)
        {
            var attributes = a.GetCustomAttributes().OfType<Attribute>().ToArray();
            AssemblyFileVersionAttribute fv = a.GetAssemblyAttribute<AssemblyFileVersionAttribute>();
            AssemblyInformationalVersionAttribute ifv = a.GetAssemblyAttribute<AssemblyInformationalVersionAttribute>(); ;
            TargetFrameworkAttribute tf = a.GetAssemblyAttribute<TargetFrameworkAttribute>();

            string ret = ifv?.InformationalVersion;
            if (string.IsNullOrEmpty(ret))
                ret = fv?.Version;

            if (string.IsNullOrEmpty(ret))
                ret = a.GetName().Version.ToString();

            if (!string.IsNullOrEmpty(tf?.FrameworkName))
                ret += $" for '{tf.FrameworkName}'";

            if (a.FullName.IndexOf("SqlClient") >= 0 && Debugger.IsAttached) Debugger.Break();

            string[] parts = new[]
            {
                ifv?.InformationalVersion,
                fv?.Version,
                GetVersionFromFullname(a),
                a.GetName()?.Version?.ToString(),
                string.IsNullOrEmpty(tf?.FrameworkName) ? null : "for " + tf?.FrameworkName,
            };

            ret = string.Join(" ", parts.Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)));

            return ret;

        }

        [Conditional("DEBUG")]
        public static void Subscribe()
        {
            AppDomain.CurrentDomain.AssemblyLoad += delegate(object sender, AssemblyLoadEventArgs args)
            {
                
                var assembly = args.LoadedAssembly;
                Console.WriteLine($"[Loading assembly info] {TryEval(() => assembly.GetName().Version),-12} {GetFileName(assembly)} <- {GetDirName(assembly)}");
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

        [Conditional("DEBUG")]
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
                ret.AppendLine($"{n.ToString(format)}) {GetFileName(assembly)} {TryEval(() => GetExtendedVersion(assembly)),-12} <- {GetDirName(assembly)}");
            }

            Console.WriteLine(caption + Environment.NewLine + ret);
        }


    }
}
