# Architectural outline

## Diagnostics
Each error and warning is a diagnostic. Diagnostics consist of a diagnostic code (with an associated pretty message), source code position, and possibly the associated token. The token is sometimes used for passing other info such as type names, which is kinda ugly but works.

The compiler and parser have their own helper methods for emitting diagnostics. In case of an error, the compilation phase is abandoned with a `null` result. The frontend must combine the diagnostics from the two sources.

## Frontend
This is just a simple driver for the compiler. Ideally, the `Peisik.Compiler` assembly should provide a simple public API for controlling the whole compilation.

## Parser
```
Parser/ModuleParser.cs
```
The parser is a hand-written recursive-descent parser. The whole language is designed around parser simplicity. The parser emits a syntax tree composed of all the fun little classes in the `Parser` directory.

The parser performs syntactic checking without bothering with the semantics. The name checking is an exception: duplicate globals (but not locals) are rejected, and fully qualified names are already checked to be properly imported. This is because the semantic compiler handles all the modules as one, and therefore has no knowledge of which modules have which imports.

## Semantic compiler
```
Compiler/SemanticCompiler.cs
```
The semantic compiler goes through each function recursively and emits bytecode for all statements. It also performs type checking. Local names are checked here, though it could be refactored into the parser.

This version of the compiler performs pretty much no optimizations at all.

## Tests
The `Peisik.Compiler.Tests` project contains the unit test suite. These tests should check all the parser and compiler paths (though they are far from complete).

The `PeisikEndToEndTests` project runs the compiled code through the interpreter and checks the results. Currently there are only some basic 'bring-up' tests. The directory also contains a manually runnable performance test suite. A standard library unit test script is included in the performance suite.