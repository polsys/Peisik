using System.Collections.Generic;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler
{
    internal class CompiledFunction
    {
        public List<BytecodeOp> Bytecode { get; set; }

        public string FullName { get; set; }

        public int FunctionTableIndex { get; set; }

        public bool IsCompiled { get; set; }

        public bool IsPrivate { get; set; }

        public List<(string name, PrimitiveType type)> Locals { get; set; }

        public List<PrimitiveType> ParameterTypes { get; set; }

        public PrimitiveType ReturnType { get; set; }

        public FunctionSyntax SyntaxTree { get; set; }

        /// <summary>
        /// If empty, visible to all modules.
        /// If not empty, visible only to the specified module.
        /// </summary>
        public string ModuleName { get; private set; }

        // The entries in this map are added and removed as the scope changes.
        // Do not use outside the compilation.
        internal Dictionary<string, short> _localMap { get; set; }

        public CompiledFunction(FunctionSyntax syntaxTree, string fullName, string moduleName, bool isPrivate, bool isCompiled)
        {
            Bytecode = new List<BytecodeOp>();
            IsCompiled = isCompiled;
            FullName = fullName;
            FunctionTableIndex = -1;
            IsPrivate = isPrivate;
            Locals = new List<(string name, PrimitiveType type)>();
            _localMap = new Dictionary<string, short>();
            ModuleName = moduleName;
            ParameterTypes = new List<PrimitiveType>();
            SyntaxTree = syntaxTree;
        }

        public short AddLocal(string fullName, PrimitiveType type)
        {
            var index = (short)Locals.Count;
            _localMap.Add(fullName, index);
            Locals.Add((fullName, type));

            return index;
        }
    }
}
