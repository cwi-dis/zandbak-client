using System;
using System.Linq;
using UnityEngine;

namespace Orchestrator.Util
{
    public class SemanticVersion : IEquatable<SemanticVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string Labels { get; }

        private int AsNumber => Major * 100 + Minor * 10 + Patch;

        public SemanticVersion(string version)
        {
            var components = version.Replace("v", "").Split(".");
            Major = int.Parse(components[0]);
            Minor = int.Parse(components[1]);

            var patchComponents = components[2].Split("-");
            Patch = int.Parse(patchComponents[0]);

            if (patchComponents.Length > 1)
            {
                Labels = string.Join(",", patchComponents.Skip(1));
            }
            else
            {
                Labels = string.Empty;
            }
        }

        public static bool operator ==(SemanticVersion a, SemanticVersion b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(SemanticVersion a, SemanticVersion b)
        {
            return !(a == b);
        }

        public static bool operator >(SemanticVersion a, SemanticVersion b)
        {
            return a.AsNumber > b.AsNumber;
        }

        public static bool operator <(SemanticVersion a, SemanticVersion b)
        {
            return a.AsNumber < b.AsNumber;
        }

        public override bool Equals(object obj)
        {
            return obj != null && GetType() == obj.GetType() && Equals((SemanticVersion)obj);
        }

        public bool Equals(SemanticVersion other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        public override string ToString()
        {
            return Labels.Length == 0 ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Labels}";
        }
    }
}
