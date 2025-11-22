using System.Collections.Generic;
using System.Linq;

namespace CppPointerVisualizer.Models
{
    public class MemoryState
    {
        public List<MemoryObject> Objects { get; set; } = new();
        public Dictionary<string, string> AddressMap { get; set; } = new();

        public MemoryObject? GetObjectByName(string name)
        {
            return Objects.FirstOrDefault(o => o.Name == name);
        }

        public MemoryObject? GetObjectByAddress(string address)
        {
            return Objects.FirstOrDefault(o => o.Address == address);
        }
    }
}
