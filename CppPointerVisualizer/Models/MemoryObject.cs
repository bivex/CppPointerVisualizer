using System.Collections.Generic;

namespace CppPointerVisualizer.Models
{
    public enum MemoryObjectType
    {
        Variable,
        Pointer,
        Reference
    }

    public class MemoryObject
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public object? Value { get; set; }
        public MemoryObjectType ObjectType { get; set; }
        public string Address { get; set; } = "";
        public string? PointsTo { get; set; }
        public bool IsConst { get; set; }
        public bool IsPointerConst { get; set; }
        public int PointerLevel { get; set; } = 0;

        public string GetTypeDescription()
        {
            if (ObjectType == MemoryObjectType.Reference)
            {
                return $"{(IsConst ? "const " : "")}{Type} &";
            }
            else if (ObjectType == MemoryObjectType.Pointer)
            {
                string stars = new string('*', PointerLevel);
                string constPtr = IsPointerConst ? " const" : "";
                return $"{(IsConst ? "const " : "")}{Type}{stars}{constPtr}";
            }
            else
            {
                return $"{(IsConst ? "const " : "")}{Type}";
            }
        }

        public string GetModifiabilityInfo()
        {
            var parts = new List<string>();

            if (ObjectType == MemoryObjectType.Variable)
            {
                if (IsConst)
                    parts.Add("значение НЕ изменяемо");
                else
                    parts.Add("значение изменяемо");
            }
            else if (ObjectType == MemoryObjectType.Pointer)
            {
                if (IsConst)
                    parts.Add("*p НЕ изменяемо");
                else
                    parts.Add("*p изменяемо");

                if (IsPointerConst)
                    parts.Add("p НЕ изменяемо");
                else
                    parts.Add("p изменяемо");
            }
            else if (ObjectType == MemoryObjectType.Reference)
            {
                parts.Add("ref НЕ изменяемо (всегда)");

                if (IsConst)
                    parts.Add("*ref НЕ изменяемо");
                else
                    parts.Add("*ref изменяемо");
            }

            return string.Join(", ", parts);
        }
    }
}
