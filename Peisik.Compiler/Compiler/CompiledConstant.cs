using System;

namespace Polsys.Peisik.Compiler
{
    internal class CompiledConstant
    {
        public int ConstantTableIndex { get; set; }

        public string FullName { get; set; }

        public PrimitiveType Type { get; set; }

        public object Value { get; set; }

        /// <summary>
        /// If empty, visible to all modules.
        /// If not empty, visible only to the specified module.
        /// </summary>
        public string VisibleToModules { get; private set; }

        public CompiledConstant(string fullName, string visibleToModules, PrimitiveType type, object value)
        {
            ConstantTableIndex = -1;
            FullName = fullName;
            Type = type;
            Value = value;
            VisibleToModules = visibleToModules;
        }
    }
}
