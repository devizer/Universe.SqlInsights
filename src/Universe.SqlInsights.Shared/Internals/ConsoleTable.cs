using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Universe.SqlInsights.Shared.Internals
{
    public class ConsoleTable
    {
        private List<List<object>> content = new List<List<object>>();
        List<string> header = new List<string>();
        List<bool> rightAlignment = new List<bool>();

        public ConsoleTable(IEnumerable<string> columns)
        {
            if (columns == null)
                throw new ArgumentNullException("columns");

            foreach (var column in columns)
            {
                rightAlignment.Add(column.StartsWith("-"));
                header.Add(column.TrimStart('-'));
            }
        }


        public void AddRow(List<object> rowCells)
        {
            List<object> row = new List<object>();
            foreach (var v in rowCells)
            {
                if (v is double?)
                {
                    double? d = (double?) v;
                    row.Add(!d.HasValue ? "-" : d.Value.ToString("f2"));
                }
                else if (v is string[])
                {
                    row.Add(v);
                }
                else
                {
                    row.Add(Convert.ToString(v));
                }
            }

            content.Add(row);
        }

        public override string ToString()
        {
            var copy = new List<List<object>>();
            copy.Add(header.Select(x => Convert.ToString(x)).Cast<object>().ToList());
            copy.AddRange(content);
            int cols = copy.Max(x => x.Count);
            List<int> width = Enumerable.Repeat(3, cols).ToList();
            for (int y = 0; y < copy.Count; y++)
            {
                List<object> row = copy[y];
                for (int x = 0; x < row.Count; x++)
                {
                    if (row[x] is string)
                        width[x] = Math.Max(width[x], (row[x] ?? "").ToString().Length);
                    else if (row[x] is string[])
                    {
                        string[] arr = (string[]) row[x];
                        width[x] = Math.Max(width[x], arr.Sum(s => s.ToString().Length) + arr.Length - 1);
                    }
                }
            }
            var sep = width.Select(x => new string('-', x)).Cast<object>().ToList();
            copy.Insert(1, sep);

            StringBuilder ret = new StringBuilder();
            for (int y = 0; y < copy.Count; y++)
            {
                List<object> row = copy[y];
                for (int x = 0; x < cols; x++)
                {
                    if (x > 0) ret.Append(y == 1 ? "+" : "|");
                    var rawCell = x < row.Count ? row[x] : null;
                    bool isPair = rawCell is string[];
                    string v = null;
                    if (!isPair)
                    {
                        v = (x < row.Count ? Convert.ToString(rawCell) : null) ?? "";
                        if (v.Length < width[x])
                        {
                            string pad = new string(' ', -v.Length + width[x]);
                            if (rightAlignment[x] && y > 0)
                                v = pad + v;
                            else
                                v = v + pad;
                        }

                    }
                    else
                    {
                        string v1 = ((string[]) rawCell)[0];
                        string v2 = ((string[]) rawCell)[1];
                        v = v1 + new string(' ', width[x] - v1.Length - v2.Length) + v2;
                        
                    }

                    ret.Append(v);
                }
                ret.AppendLine();
            }

            return ret.ToString();
        }
    }
}