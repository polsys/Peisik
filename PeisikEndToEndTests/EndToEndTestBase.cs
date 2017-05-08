using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Parser;

namespace PeisikEndToEndTests
{
    class EndToEndTestBase
    {
        protected string CompileAndRun(string source, string targetFileName, string arguments)
        {
            var program = CompileStringWithoutDiagnostics(source);
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using (var writer = new BinaryWriter(new FileStream(Path.Combine(path, targetFileName), FileMode.Create)))
            {
                program.Serialize(writer);
            }

            var startInfo = new ProcessStartInfo() {
                Arguments = arguments + " " + targetFileName,
                FileName = Path.Combine(path, "peisik.exe"),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = path
            };

            var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private CompiledProgram CompileStringWithoutDiagnostics(string source)
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
    }
}
