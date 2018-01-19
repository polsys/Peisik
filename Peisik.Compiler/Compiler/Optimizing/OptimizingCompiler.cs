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
        private List<Function> _functions;

        public OptimizingCompiler(List<ModuleSyntax> modules, Optimization optimizationLevel)
        {
            _modules = modules;
            _optimizationLevel = optimizationLevel;
            _diagnostics = new List<CompilationDiagnostic>();

            _constants = new Dictionary<string, (object, string)>();
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
                // First go through all modules and add constants and functions
                // After this pass all type information is known and functions may be referenced
                foreach (var module in _modules)
                {
                    foreach (var constant in module.Constants)
                    {
                        AddConstant(constant.Name, module.ModuleName, constant.Visibility, constant.Value);
                    }

                    // TODO: Module name
                    foreach (var function in module.Functions)
                    {
                        _functions.Add(Function.InitializeFromSyntax(function, this, module.ModuleName));
                    }
                }

                // Compile each function
                // Actual syntactic analysis is performed in this pass
                foreach (var function in _functions)
                {
                    function.Compile();
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
            if (_constants.TryGetValue((modulePrefix + name).ToLowerInvariant(), out var constant))
            {
                if (string.IsNullOrEmpty(constant.onlyVisibleTo))
                {
                    // The constant is public
                    value = constant.value;
                    return true;
                }
                else
                {
                    if ((constant.onlyVisibleTo == "." && modulePrefix == "") ||
                        (constant.onlyVisibleTo == modulePrefix.ToLowerInvariant()))
                    {
                        // The constant is private within current module
                        value = constant.value;
                        return true;
                    }
                    else
                    {
                        // The constant is private and not visible
                        value = null;
                        return false;
                    }
                }
            }
            else
            {
                // The constant name does not exist
                value = null;
                return false;
            }
        }
    }
}
