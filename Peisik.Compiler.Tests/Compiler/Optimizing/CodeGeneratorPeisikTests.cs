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
        public void FunctionCall_ConstantParameters()
        {
            var source = @"
public int GetValue(int a, int b)
begin
  return 42
end

public int Main()
begin
  return GetValue(1, 2)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            // Parameters are always evaluated from left to right
            var disasm = @"Int main() [0 locals]
PushConst   $literal_1
PushConst   $literal_2
Call        getvalue
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_ComplexParameters()
        {
            var source = @"
public int GetFirstValue(int a)
begin
  return a
end

public int GetValue(int a, int b)
begin
  return 42
end

public int Main()
begin
  return GetValue(GetFirstValue(1), 2)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            // Parameters are always evaluated from left to right
            var disasm = @"Int main() [0 locals]
PushConst   $literal_1
Call        getfirstvalue
PushConst   $literal_2
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
        public void InternalCall_Unary()
        {
            var source = @"
public int Main()
begin
  return -(5)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_5
CallI1      Minus
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void InternalCall_Binary()
        {
            var source = @"
public int Main()
begin
  return -(5, 1)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_5
PushConst   $literal_1
CallI2      Minus
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void InternalCall_FailFast()
        {
            var source = @"
public void Main()
begin
  FailFast()
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
CallI0      FailFast
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void InternalCall_Print()
        {
            var source = @"
public void Main()
begin
  Print(true, 1, 1.0)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
PushConst   $literal_true
PushConst   $literal_1
PushConst   $literal_1r
CallI3      Print
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
        [TestCase(Optimization.None)]
        [TestCase(Optimization.Full)]
        public void ParametersAreCorrectlyPassed(Optimization optimizationLevel)
        {
            var source = @"
public int Function(int a, bool b, real c)
begin
  int result a
  real something c
  # b goes unused
  return result
end

public int Main()
begin
  return Function(1, true, 1.0)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var function = program.Functions[1 - program.MainFunctionIndex];
            Assert.That(function.Locals[0].name, Does.StartWith("a"));
            Assert.That(function.Locals[1].name, Does.StartWith("b"));
            Assert.That(function.Locals[2].name, Does.StartWith("c"));
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
