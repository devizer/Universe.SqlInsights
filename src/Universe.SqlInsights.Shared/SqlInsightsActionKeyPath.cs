using System;
using System.Linq;
using System.Threading;

namespace Universe.SqlInsights.Shared
{
    public class SqlInsightsActionKeyPath 
    {
        private readonly Lazy<int> _HashCode;
        private readonly Lazy<string> _ToString;
        public string[] Path { get; set;  }

        public SqlInsightsActionKeyPath()
        {
            _ToString = new Lazy<string>(() =>
            {
                const string arrow = " \x2192 ";
                return Path == null ? "" : string.Join(arrow, Path);
            }, LazyThreadSafetyMode.None);
            
            _HashCode = new Lazy<int>(() =>
            {
                if (Path == null) return 0;
                int ret = 0;
                unchecked
                {
                    foreach (var p in Path)
                        ret = ret * 397 ^ (p?.GetHashCode() ?? 0);
                }

                return ret;

            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public SqlInsightsActionKeyPath(params string[] path) : this()
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            for(int i=0, l=path.Length; i<l; i++)
                if (path[i] == null) throw new ArgumentException($"path's element #{i} is null", nameof(path));
        }

        public SqlInsightsActionKeyPath Child(string childName)
        {
            if (childName == null) throw new ArgumentNullException(nameof(childName));
            return new SqlInsightsActionKeyPath(Path.Concat(new[] {childName}).ToArray());
        }

        public override string ToString() => _ToString.Value;

        protected bool Equals(SqlInsightsActionKeyPath other)
        {
            if (_HashCode.Value != other._HashCode.Value) return false;
            
            var len = Path.Length;
            var lenOther = other.Path.Length;
            if (len != lenOther) return false;
            for(int i=0; i<len; i++)
                if (!Path[i].Equals(other.Path[i], StringComparison.OrdinalIgnoreCase))
                    return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SqlInsightsActionKeyPath) obj);
        }

        public override int GetHashCode()
        {
            return _HashCode.Value;
        }

    }
}