using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests
{
    internal class CompilerTestBase
    {
        protected (CompiledProgram program, List<CompilationDiagnostic> diagnostics) CompileStringWithDiagnostics(string source)
        {
            using (var reader = new StringReader(source))
            {
                (var syntaxTree, var parserDiags) = ModuleParser.Parse(reader, "Filename", "");
                if (syntaxTree == null)
                    return (null, parserDiags);

                var compiler = new SemanticCompiler(new List<ModuleSyntax>() { syntaxTree });
                return compiler.Compile();
            }
        }

        protected CompiledProgram CompileStringWithoutDiagnostics(string source)
        {
            using (var reader = new StringReader(source))
            {
                (var syntaxTree, var parserDiagnostics) = ModuleParser.Parse(reader, "Filename", "");
                Assert.That(parserDiagnostics, Is.Empty, "There were parser diagnostics.");

                var compiler = new SemanticCompiler(new List<ModuleSyntax>() { syntaxTree });
                (var program, var compilerDiagnostics) = compiler.Compile();
                Assert.That(compilerDiagnostics, Is.Empty, "There were compiler diagnostics.");

                return program;
            }
        }

        protected (CompiledProgram program, List<CompilationDiagnostic> diagnostics)
            CompileOptimizedWithDiagnostics(string source, Optimization optimizationLevel)
        {
            using (var reader = new StringReader(source))
            {
                (var syntaxTree, var parserDiags) = ModuleParser.Parse(reader, "Filename", "");
                if (syntaxTree == null)
                    return (null, parserDiags);

                var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntaxTree }, optimizationLevel);
                return compiler.Compile();
            }
        }

        protected CompiledProgram CompileOptimizedWithoutDiagnostics(string source, Optimization optimizationLevel)
        {
            using (var reader = new StringReader(source))
            {
                (var syntaxTree, var parserDiagnostics) = ModuleParser.Parse(reader, "Filename", "");
                Assert.That(parserDiagnostics, Is.Empty, "There were parser diagnostics.");

                var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntaxTree }, optimizationLevel);
                (var program, var compilerDiagnostics) = compiler.Compile();
                Assert.That(compilerDiagnostics, Is.Empty, "There were compiler diagnostics.");

                return program;
            }
        }

        protected (ModuleSyntax module, List<CompilationDiagnostic> diagnostics) ParseStringWithDiagnostics(string source)
        {
            using (var reader = new StringReader(source))
            {
                return ModuleParser.Parse(reader, "Filename", "");
            }
        }

        protected ModuleSyntax ParseStringWithoutDiagnostics(string source)
        {
            using (var reader = new StringReader(source))
            {
                (var module, var diagnostics) = ModuleParser.Parse(reader, "Filename", "");

                Assert.That(diagnostics, Is.Empty, "There were parser diagnostics.");
                return module;
            }
        }

        protected void VerifyDisassembly(CompiledFunction function, CompiledProgram program, string expected)
        {
            // Normalize the line feed char
            expected = expected.Replace("\r\n", "\n").Trim();
            var actual = BytecodeDisassembler.Disassemble(function, program).Replace("\r\n", "\n").Trim();

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
