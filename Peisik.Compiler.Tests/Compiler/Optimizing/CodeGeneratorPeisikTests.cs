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
        private CompiledProgram CompileSingleFunction(string source)
        {
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();
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
            var program = CompileSingleFunction(source);

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
            var program = CompileSingleFunction(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_5
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_DiscardsResult()
        {
            var source = @"
public int GetValue()
begin
  return 42
end

public int Main()
begin
  GetValue()
  return 5
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
Call        getvalue
PopDiscard
PushConst   $literal_5
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_ReturnsResult()
        {
            var source = @"
public int GetValue()
begin
  return 42
end

public int Main()
begin
  return GetValue()
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
Call        getvalue
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_Void()
        {
            var source = @"
public void DoNothing()
begin
end

public int Main()
begin
  DoNothing()
  return 5
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
Call        donothing
PushConst   $literal_5
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void MainFunctionIndex_IsSet()
        {
            var source = @"
private void DoNothing()
begin
end

private void Main()
begin
end

private int Something()
begin
  return 2
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program.Functions[program.MainFunctionIndex].FullName, Is.EqualTo("main"));
        }

        [Test]
        public void VoidReturning()
        {
            var source = @"
public void Main()
begin
end";
            var program = CompileSingleFunction(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }
    }
}
