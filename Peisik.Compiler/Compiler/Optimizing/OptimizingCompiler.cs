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

        /// <summary>
        /// If onlyVisibleTo is empty, the constant is public.
        /// If onlyVisibleTo is a module prefix, the constant is only visible to that module.
        /// If onlyVisibleTo is ".", the constant is only visible to the main module.
        /// </summary>
        private Dictionary<string, (object value, string onlyVisibleTo)> _constants;
        /// <summary>
        /// See <see cref="_constants"/> comment.
        /// </summary>
        private Dictionary<string, (Function function, string onlyVisibleTo)> _functions;

        public OptimizingCompiler(List<ModuleSyntax> modules, Optimization optimizationLevel)
        {
            _modules = modules;
            _optimizationLevel = optimizationLevel;
            _diagnostics = new List<CompilationDiagnostic>();

            _constants = new Dictionary<string, (object, string)>();
            _functions = new Dictionary<string, (Function function, string onlyVisibleTo)>();
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
                // First go through all modules and add constants and functions
                // After this pass all type information is known and functions may be referenced
                foreach (var module in _modules)
                {
                    foreach (var constant in module.Constants)
                    {
                        AddConstant(constant.Name, module.ModuleName, constant.Visibility, constant.Value);
                    }
                    
                    foreach (var function in module.Functions)
                    {
                        var f = Function.InitializeFromSyntax(function, this, module.ModuleName);
                        var onlyVisibleTo = "";
                        if (function.Visibility == Visibility.Private)
                        {
                            onlyVisibleTo = module.ModuleName.ToLowerInvariant() + ".";
                        }

                        _functions.Add(f.FullName, (f, onlyVisibleTo));
                    }
                }

                // Compile each function
                // Actual syntactic analysis is performed in this pass
                // Also do some preliminary analysis and simple optimizations (if desired)
                foreach ((var function, _) in _functions.Values)
                {
                    function.Compile();
                    function.AnalyzeAndOptimizePreInlining(_optimizationLevel);
                }

                // Ensure that there is a Main() function and it has no parameters
                if (_functions.TryGetValue("main", out var mainFunction))
                {
                    if (mainFunction.function.ParameterTypes.Count > 0)
                    {
                        LogError(DiagnosticCode.MainMayNotHaveParameters, default);
                    }
                }
                else
                {
                    LogError(DiagnosticCode.NoMainFunction, default);
                }

                // Run desired optimizations
                // - inlining
                // - optimizations that are run after inlining

                // Generate code
                var codeGen = new CodeGeneratorPeisik();
                foreach ((var function, _) in _functions.Values)
                {
                    codeGen.CompileFunction(function);
                }

                return (codeGen.GetProgram(), _diagnostics);

            }
            catch (CompilerException)
            {
                // Compilation failed, return failure
                return (null, _diagnostics);
            }
        }

        internal void AddConstant(string name, string moduleName, Visibility visibility, object value)
        {
            var nameInLower = name.ToLowerInvariant();
            if (!string.IsNullOrEmpty(moduleName))
                nameInLower = moduleName.ToLowerInvariant() + "." + nameInLower;

            var visibilityString = "";
            if (visibility != Visibility.Public)
            {
                if (moduleName == "")
                    visibilityString = ".";
                else
                    visibilityString = moduleName.ToLowerInvariant() + ".";
            }

            _constants.Add(nameInLower, (value, visibilityString));
        }

        internal bool TryGetConstant(string name, string modulePrefix, out object value)
        {
            return TryGetSymbol(_constants, name, modulePrefix, out value);
        }

        internal bool TryGetFunction(string name, string modulePrefix, out Function value)
        {
            return TryGetSymbol(_functions, name, modulePrefix, out value);
        }

        private bool TryGetSymbol<T>(Dictionary<string, (T value, string onlyVisibleTo)> dict,
            string name, string modulePrefix, out T value)
        {
            if (dict.TryGetValue((modulePrefix + name).ToLowerInvariant(), out var symbol))
            {
                if (string.IsNullOrEmpty(symbol.onlyVisibleTo))
                {
                    // The symbol is public
                    value = symbol.value;
                    return true;
                }
                else
                {
                    if ((symbol.onlyVisibleTo == "." && modulePrefix == "") ||
                        (symbol.onlyVisibleTo == modulePrefix.ToLowerInvariant()))
                    {
                        // The symbol is private within current module
                        value = symbol.value;
                        return true;
                    }
                    else
                    {
                        // The symbol is private and not visible
                        value = default;
                        return false;
                    }
                }
            }
            else
            {
                // The name does not exist
                value = default;
                return false;
            }
        }
    }
}
