# The optimizing compiler

The `Peisik.Compiler.Optimizing.OptimizingCompiler` class implements a new, optimizing compiler. This is a complete replacement for `SemanticCompiler`, though the latter is left in the source code as a reference. The optimization level can be controlled through a constructor parameter. This compiler is also extensible to produce better warnings.

While the initial version only emits Peisik bytecode, the design should be extensible to produce machine code. The optimizations are quite general; this is nowhere near a _real_ compiler. Tradeoffs for simplicity are common.

## Overview
1. The source code is parsed as before with `ModuleParser`.
2. Preliminary import: all functions and constants are imported in the compiler, but they are not compiled yet. After this pass type information for constants, function parameters and return values is known. See `Function.InitializeFromSyntax(...)`.
3. Semantic analysis: the syntax tree for each function is transformed into a compiler-understandable expression tree. In this phase, semantic errors are reported. This is the last mandatory pass before code generation. See `Function.Compile(...)`.
4. First optimization pass: constant folding and analysis for inlining. See `Function.AnalyzeAndOptimizePreInlining(...)`.
5. Inlining: TBD.
6. Second optimization pass: TBD. New pass of constant folding as the inlining has allowed new opportunities.
7. Code generation: the expression trees are recursively written as Peisik bytecode. See `CodeGeneratorPeisik.CompileFunction(...)`.


## Optimizations (planned)
- Constant folding
  - Internal functions with constant parameters
  - Simplification of if statements with constant condition
- Dead code elimination
- Function inlining
- Common subexpression elimination / value numbering (maybe, limited)
- Variable slot allocation (maybe, reduces slots in use)
- Some numeric optimizations (maybe)
