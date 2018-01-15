using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// A stack that maps identifiers to local variables.
    /// </summary>
    internal class LocalVariableContext
    {
        Stack<Dictionary<string, LocalVariable>> _stack;
        Function _function;

        public LocalVariableContext(Function function)
        {
            _function = function;
            _stack = new Stack<Dictionary<string, LocalVariable>>();
            _stack.Push(new Dictionary<string, LocalVariable>());
        }

        /// <summary>
        /// Adds a new local variable into the current block.
        /// If the name already exists, this logs an error.
        /// </summary>
        public LocalVariable AddLocal(string name, PrimitiveType type, TokenPosition position)
        {
            var nameInLower = name.ToLowerInvariant();

            if (TryGetLocal(nameInLower) != null)
                _function.Compiler.LogError(DiagnosticCode.NameAlreadyDefined, position, name);

            var local = _function.AddVariable(nameInLower, type);
            _stack.Peek().Add(nameInLower, local);
            return local;
        }

        /// <summary>
        /// Returns the local with the specified name.
        /// If the name does not exist, this logs an error.
        /// </summary>
        public LocalVariable GetLocal(string name, TokenPosition position)
        {
            var local = TryGetLocal(name.ToLowerInvariant());
            if (local == null)
                _function.Compiler.LogError(DiagnosticCode.NameNotFound, position, name);
            return local;
        }

        private LocalVariable TryGetLocal(string nameInLower)
        {
            foreach (var frame in _stack)
            {
                if (frame.TryGetValue(nameInLower, out var local))
                    return local;
            }

            return null;
        }

        /// <summary>
        /// Enters a new block.
        /// </summary>
        public void Push()
        {
            _stack.Push(new Dictionary<string, LocalVariable>());
        }

        /// <summary>
        /// Exits a block and removes all local names defined in that block.
        /// </summary>
        public void Pop()
        {
            _stack.Pop();
        }
    }
}
