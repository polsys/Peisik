namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Maintains active register information.
    /// </summary>
    internal abstract class RegisterBackend
    {
        /// <summary>
        /// Returns a free storage location for a variable.
        /// </summary>
        /// <param name="type">The type of the variable.</param>
        /// <param name="isParameter">
        /// If true, the storage for this variable will be allocated on stack.
        /// The parameters are assumed to be processed in order.
        /// </param>
        /// <param name="onStack">
        /// Set to true if the resulting location is on stack instead of register.
        /// </param>
        public abstract int GetLocation(PrimitiveType type, bool isParameter, out bool onStack);

        /// <summary>
        /// Returns the specified storage location to the unused pool.
        /// </summary>
        public abstract void ReturnLocation(int location);
    }
}
