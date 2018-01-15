using System;
using System.Collections.Generic;
using System.Globalization;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Implements an optimizing compiler that replaces <see cref="SemanticCompiler"/>.
    /// The optimization level can be controlled through constructor parameters.
    /// </summary>
    internal class OptimizingCompiler
    {
        private List<ModuleSyntax> _modules;
        private Optimization _optimizationLevel;
        internal List<CompilationDiagnostic> _diagnostics;

        private List<Function> _functions;
        private CompiledProgram _program;

        public OptimizingCompiler(List<ModuleSyntax> modules, Optimization optimizationLevel)
        {
            _modules = modules;
            _optimizationLevel = optimizationLevel;
            _diagnostics = new List<CompilationDiagnostic>();
        }

        internal void LogError(DiagnosticCode error, TokenPosition position, string token = "", string expected = "")
        {
            _diagnostics.Add(new CompilationDiagnostic(error, true, token, expected, position));
            throw new CompilerException();
        }

        internal void LogWarning(DiagnosticCode error, TokenPosition position, string token = "", string expected = "")
        {
            _diagnostics.Add(new CompilationDiagnostic(error, false, token, expected, position));
        }

        public (CompiledProgram program, List<CompilationDiagnostic> diagnostics) Compile()
        {
            try
            {
                // TODO: Constants

                // Create expression tree for each function
                // This also performs semantic analysis
                foreach (var module in _modules)
                {
                    // TODO: Module name
                    foreach (var function in module.Functions)
                    {
                        _functions.Add(Function.FromSyntax(function, this));
                    }
                }

                // Ensure that there is a Main() function

                // Run desired optimizations

                // Generate code

            }
            catch (CompilerException)
            {
                // Compilation failed, return failure
                _program = null;
            }
            return (_program, _diagnostics);
        }
    }
}
