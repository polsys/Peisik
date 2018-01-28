using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Polsys.Peisik;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;
using Polsys.Peisik.Parser;

namespace Polsys.PeisikFrontend
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse the command line arguments
            var disassembly = false;
            var legacyCompiler = false;
            var optimize = false;
            var timing = false;
            var showHelp = args.Length == 0;
            var modules = new List<string>();
            foreach (var arg in args)
            {
                var argInLower = arg.ToLowerInvariant();
                if (argInLower == "--disasm")
                {
                    disassembly = true;
                }
                else if (argInLower == "--help")
                {
                    showHelp = true;
                }
                else if (argInLower == "--legacy")
                {
                    legacyCompiler = true;
                }
                else if (argInLower == "--optimize" || argInLower == "-o")
                {
                    optimize = true;
                }
                else if (argInLower == "--timing")
                {
                    timing = true;
                }
                else
                {
                    modules.Add(arg);
                }
            }

            if (showHelp)
            {
                Console.WriteLine("The Peisik compiler");
                Console.WriteLine("Usage: peisikc [modules] [parameters]");
                Console.WriteLine("Possible parameters:");
                Console.WriteLine(" --disasm   Print bytecode disassembly for each module.");
                Console.WriteLine(" --help     Show this help.");
                Console.WriteLine(" --legacy   Use the legacy non-optimizing compiler.");
                Console.WriteLine(" --optimize Optimize code. (No effect when used with --legacy.)");
                Console.WriteLine("  -o");
                Console.WriteLine(" --timing   Print compilation times.");
                return;
            }

            // Compile each module
            var success = 0;
            var totalTime = Stopwatch.StartNew();
            foreach (var moduleName in modules)
            {
                var moduleNameWithExt = moduleName;
                if (string.IsNullOrEmpty(Path.GetExtension(moduleName)))
                    moduleNameWithExt = moduleName + ".peisik";

                var moduleTime = Stopwatch.StartNew();
                var module = CompileModule(moduleNameWithExt, legacyCompiler, optimize);
                moduleTime.Stop();
                if (timing)
                {
                    Console.WriteLine($"-- Time for this module: {moduleTime.Elapsed.TotalSeconds} s");
                }

                if (module != null)
                {
                    success++;
                    if (disassembly)
                        DisassembleModule(module);
                }
            }
            totalTime.Stop();

            Console.WriteLine($"-- Done: {success} succeeded, {modules.Count - success} failed.");
            if (timing)
            {
                Console.WriteLine($"-- Total time for all modules: {totalTime.Elapsed.TotalSeconds} s");
            }
        }

        private static CompiledProgram CompileModule(string filename, bool legacyCompiler, bool optimize)
        {
            Console.WriteLine("-- Compiling module " + filename);

            try
            {
                using (var source = new StreamReader(filename))
                {
                    // Parse the main module
                    (var mainModule, var parserDiags) = ModuleParser.Parse(source, filename, "");
                    PrintDiagnostics(parserDiags);
                    if (mainModule == null)
                        return null;

                    // Parse all its dependencies
                    var modules = new Dictionary<string, ModuleSyntax>();
                    var moduleDependencies = new List<string>();
                    moduleDependencies.AddRange(mainModule.ModuleDependencies);

                    for (int i = 0; i < moduleDependencies.Count; i++)
                    {
                        if (modules.ContainsKey(moduleDependencies[i]))
                            continue;

                        var modFileName = moduleDependencies[i] + ".peisik";
                        using (var modFile = new StreamReader(modFileName))
                        {
                            (var mod, var diags) = ModuleParser.Parse(modFile, modFileName, moduleDependencies[i]);
                            PrintDiagnostics(diags);
                            if (mod == null)
                                return null;

                            modules.Add(moduleDependencies[i], mod);
                            moduleDependencies.AddRange(mod.ModuleDependencies);
                        }
                    }

                    // Then compile everything
                    var finalModules = new List<ModuleSyntax>(modules.Count + 1);
                    finalModules.Add(mainModule);
                    finalModules.AddRange(modules.Values);
                    CompiledProgram program = legacyCompiler ? CompileLegacy(finalModules) : CompileOptimized(finalModules, optimize);
                    if (program == null)
                        return null;

                    var outputPath = Path.GetFileNameWithoutExtension(filename) + ".cpeisik";
                    using (var writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
                    {
                        program.Serialize(writer);
                    }
                    return program;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private static CompiledProgram CompileLegacy(List<ModuleSyntax> finalModules)
        {
            var compiler = new SemanticCompiler(finalModules);
            (var program, var compilerDiags) = compiler.Compile();

            PrintDiagnostics(compilerDiags);
            return program;
        }

        private static CompiledProgram CompileOptimized(List<ModuleSyntax> finalModules, bool optimize)
        {
            var optimizations = optimize ? Optimization.Full : Optimization.None;
            var compiler = new OptimizingCompiler(finalModules, optimizations);
            (var program, var compilerDiags) = compiler.Compile();

            PrintDiagnostics(compilerDiags);
            return program;
        }

        private static void DisassembleModule(CompiledProgram module)
        {
            foreach (var function in module.Functions)
            {
                Console.WriteLine(BytecodeDisassembler.Disassemble(function, module));
            }
        }

        private static void PrintDiagnostics(List<CompilationDiagnostic> diagnostics)
        {
            foreach (var diag in diagnostics)
            {
                if (diag.IsError)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[ERR]  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("[WARN] ");
                    Console.ResetColor();
                }
                Console.WriteLine($"{diag.Position.Filename}:{diag.Position.LineNumber}:{diag.Position.Column}");
                Console.WriteLine($"       {diag.GetDescription()}");
            }
        }
    }
}
