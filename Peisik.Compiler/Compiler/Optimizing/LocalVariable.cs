namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a function local variable.
    /// </summary>
    internal class LocalVariable
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        public PrimitiveType Type;

        /// <summary>
        /// The current value number of this variable.
        /// This value will change during compilation as assignments to this variable are processed.
        /// </summary>
        internal int Version;

        /// <summary>
        /// The user-defined name of this variable.
        /// </summary>
        public string Name;

        /// <summary>
        /// The number of assignments to this variable.
        /// </summary>
        internal int AssignmentCount;

        /// <summary>
        /// The number of uses of this variable.
        /// </summary>
        internal int UseCount;

        /// <summary>
        /// TODO: Will be replaced with an abstraction for storage location.
        /// </summary>
        internal int LocalIndex;

        public override string ToString()
        {
            return Name + "#" + Version.ToString();
        }

        public LocalVariable(PrimitiveType type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}