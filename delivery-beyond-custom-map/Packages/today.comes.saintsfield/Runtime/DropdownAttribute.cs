using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SaintsField
{
    [Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Method | System.AttributeTargets.Parameter)]
    public class DropdownAttribute: PathedDropdownAttribute
    {
        public DropdownAttribute(string funcName = null, EUnique unique = EUnique.None): base(funcName, unique)
        {
        }

        public DropdownAttribute(EUnique unique) : base(unique) {}
    }
}
