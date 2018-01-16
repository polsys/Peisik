using System;
using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class CodeGeneratorPeisikTests : CompilerTestBase
    {
        private CompiledProgram CompileProgram(string source)
        {
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.FromSyntax(syntax.Functions[0], compiler);
            var codeGen = new CodeGeneratorPeisik();

            codeGen.CompileFunction(function);
            return codeGen.Result;
        }

        [Test]
        public void ConstantReturning()
        {
            var source = @"
public int Main()
begin
  return 5
end";
            var program = CompileProgram(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_5
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void ConstantReturning_WithTemp()
        {
            // The temporary variable should be optimized away even with Optimization.None
            var source = @"
public int Main()
begin
  int result 5
  return result
end";
            var program = CompileProgram(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_5
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }
    }
}
