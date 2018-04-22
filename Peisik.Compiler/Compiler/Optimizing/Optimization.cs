using System;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents the different optimizations available.
    /// Some platforms may require or not implement certain optimizations.
    /// In those cases the flag becomes a no-op.
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
        /// On the bytecode backend, the local variable slots should be combined, if possible.
        /// </summary>
        RegisterAllocation = 2,
        /// <summary>
        /// All available optimizations should be performed.
        /// </summary>
        Full = ConstantFolding | RegisterAllocation
    }
}
