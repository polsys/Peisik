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

        private Dictionary<string, object> _constants;
        private List<Function> _functions;

        public OptimizingCompiler(List<ModuleSyntax> modules, Optimization optimizationLevel)
        {
            _modules = modules;
            _optimizationLevel = optimizationLevel;
            _diagnostics = new List<CompilationDiagnostic>();

            _constants = new Dictionary<string, object>();
            _functions = new List<Function>();
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
                // Create expression tree for each function
                // This also performs semantic analysis
                foreach (var module in _modules)
                {
                    // TODO: Module name
                    foreach (var constant in module.Constants)
                    {
                        AddConstant(constant.Name, constant.Value);
                    }

                    // TODO: Module name
                    foreach (var function in module.Functions)
                    {
                        _functions.Add(Function.FromSyntax(function, this));
                    }
                }

                // Ensure that there is a Main() function

                // Run desired optimizations

                // Generate code
                var codeGen = new CodeGeneratorPeisik();

                foreach (var function in _functions)
                {
                    codeGen.CompileFunction(function);
                }

                return (codeGen.Result, _diagnostics);

            }
            catch (CompilerException)
            {
                // Compilation failed, return failure
                return (null, _diagnostics);
            }
        }

        internal void AddConstant(string name, object value)
        {
            var nameInLower = name.ToLowerInvariant();

            _constants.Add(nameInLower, value);
        }

        internal bool TryGetConstant(string name, out object value)
        {
            return _constants.TryGetValue(name.ToLowerInvariant(), out value);
        }
    }
}
