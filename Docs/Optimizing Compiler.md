# The optimizing compiler

The `Peisik.Compiler.Optimizing.OptimizingCompiler` class implements a new, optimizing compiler. This is a complete replacement for `SemanticCompiler`, though the latter is left in the source code as a reference. The optimization level can be controlled through a constructor parameter. This compiler also offers better warnings.

While the initial version only emits Peisik bytecode, the design should be extensible to produce machine code. The optimizations are quite general; this is nowhere near a _real_ compiler. Tradeoffs for simplicity are common.

## Overview
1. The parsed syntax trees are imported as they would be in `SemanticCompiler`.
2. The syntax tree for each function is transformed into a compiler internal expression tree. The semantic analysis is performed in this phase.
3. Various optimization transforms are performed on the tree. Functions are compiled depth-first so that each function has access to all its callees' trees.
4. Local variable lifetimes are computed and variables assigned to local slots/registers.
5. The final expression tree is emitted as bytecode/machine code.
6. The output is stored.


## Optimizations (planned)
- Constant folding
- Dead code elimination
- Function inlining
- Common subexpression elimination / value numbering (limited)
- Variable slot allocation (reduces unused slots)
- Some numeric optimizations (maybe)
