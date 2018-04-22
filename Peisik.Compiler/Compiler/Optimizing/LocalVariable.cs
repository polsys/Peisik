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
        /// If true, this local is a parameter to the containing function.
        /// </summary>
        internal bool IsParameter;

        /// <summary>
        /// A platform-dependent value for storage location.
        /// </summary>
        /// <remarks>
        /// Initially set to -1 to signal unassigned location.
        /// </remarks>
        internal int StorageLocation = -1;

        /// <summary>
        /// If false, this variable may be spilled to stack.
        /// </summary>
        internal bool OnStack;

        /// <summary>
        /// The start position of the live interval.
        /// </summary>
        /// <remarks>
        /// Initially set to -1 to distinguish from computed intervals.
        /// </remarks>
        internal int IntervalStart = -1;

        /// <summary>
        /// The end position of the live interval.
        /// </summary>
        internal int IntervalEnd = -1;

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