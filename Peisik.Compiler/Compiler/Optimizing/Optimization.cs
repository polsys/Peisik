using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents the different optimizations available.
    /// </summary>
    [Flags]
    enum Optimization
    {
        /// <summary>
        /// No optimizations should be performed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Constant expressions should be evaluated at compile-time, if possible.
        /// </summary>
        ConstantFolding = 1,
        /// <summary>
        /// All available optimizations should be performed.
        /// </summary>
        Full = ConstantFolding
    }
}
