using System;
using System.Collections.Generic;
using System.Text;

namespace Universe.SqlInsights.SqlServerStorage
{
    internal class CrossPath
    {
        public static string Combine(bool isWindows, params string[] parts)
        {
            char sep = isWindows ? '\\' : '/'; 
            StringBuilder ret = new StringBuilder();
            foreach (var part in parts)
            {
                if (ret.Length > 0) ret.Append(sep);
                ret.Append(part.TrimEnd(sep));
            }

            return ret.ToString();
        }
        public static string GetDirectoryName(bool isWindows, string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            var fileNameLength = fileName.Length;
            char sep = isWindows ? '\\' : '/';
            var lastIndex = fileName.LastIndexOf(sep);
            if (lastIndex < 0) return string.Empty;
            if (lastIndex == 0) return string.Empty;
            if (fileNameLength == 1) return string.Empty;

            if (lastIndex == fileNameLength - 1) return fileName.Substring(0, fileNameLength - 1);
            var ret = fileName.Substring(0, lastIndex);
            return ret;
        }
    }
}
