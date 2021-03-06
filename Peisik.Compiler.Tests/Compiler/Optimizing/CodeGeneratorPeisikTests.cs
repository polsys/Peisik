﻿using System;
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

            codeGen.CompileFunction(function, Optimization.None);
            return codeGen.GetProgram();
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
        public void FunctionCall_AssignsResult()
        {
            var source = @"
public int GetValue()
begin
  return 42
end

public int Main()
begin
  int local GetValue()
  if ==(local, 42)
  begin
    return local
  end
  return 0
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [1 locals]
Call        getvalue
PopLocal    local$1
PushLocal   local$1
PushConst   $literal_42
CallI2      Equal
JumpFalse   +3
PushLocal   local$1
Return
PushConst   $literal_0
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
        public void FunctionIndices_InCorrectOrder()
        {
            // There was a bug where function order and indices did not match
            // As a result, the below program would call DoSomething()
            var source = @"
public int Main()
begin
  DoNothing()
  return 5
end

public void DoSomething()
begin
end

public void DoNothing()
begin
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
        public void InternalCall_Unary_Store()
        {
            var source = @"
public int Main()
begin
  int a -(5)
  print(a)
  return a
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [1 locals]
PushConst   $literal_5
CallI1      Minus
PopLocal    a$1
PushLocal   a$1
CallI1      Print
PushLocal   a$1
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
        public void InternalCall_Binary_Store()
        {
            var source = @"
public int Main()
begin
  int a -(5, 1)
  print(a)
  return a
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [1 locals]
PushConst   $literal_5
PushConst   $literal_1
CallI2      Minus
PopLocal    a$1
PushLocal   a$1
CallI1      Print
PushLocal   a$1
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
        public void If_EmptyBody()
        {
            var source = @"
private void Main()
begin
  if true
  begin
  end
  else
  begin
  end
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var dis = @"
Void main() [0 locals]
PushConst   $literal_true
JumpFalse   +1
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }

        [Test]
        public void If_FullBody()
        {
            var source = @"
private int Main()
begin
  int result 0

  if true
  begin
    result = 1
  end
  else
  begin
    result = 2
  end
  return result
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var dis = @"
Int main() [1 locals]
PushConst   $literal_0
PopLocal    result$1
PushConst   $literal_true
JumpFalse   +4
PushConst   $literal_1
PopLocal    result$1
Jump        +3
PushConst   $literal_2
PopLocal    result$1
PushLocal   result$1
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }

        [Test]
        public void If_NoElse()
        {
            var source = @"
private int Main()
begin
  int result 0

  if true
  begin
    result = 1
  end
  return result
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            Assert.That(program, Is.Not.Null);
            var dis = @"
Int main() [1 locals]
PushConst   $literal_0
PopLocal    result$1
PushConst   $literal_true
JumpFalse   +3
PushConst   $literal_1
PopLocal    result$1
PushLocal   result$1
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
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
            var program = CompileOptimizedWithoutDiagnostics(source, optimizationLevel);

            Assert.That(program, Is.Not.Null);
            var function = program.Functions[1 - program.MainFunctionIndex];

            // Register allocation may combine stack slots for a and result
            Assert.That(function.Locals[0].name, Does.StartWith("a").Or.StartWith("result"));
            Assert.That(function.Locals[1].name, Does.StartWith("b"));
            Assert.That(function.Locals[2].name, Does.StartWith("c"));

            Assert.That(function.ParameterTypes, Has.Exactly(3).Items);
        }

        [Test]
        public void RegisterAllocation_LocalSlotsCombined()
        {
            var source = @"
public void Main()
begin
  # Two uses for each so that FoldSingleUseLocals does not fire
  int i 2
  i = +(i,1)
  print(i)
  int j 3
  j = +(j, 1)
  print(j)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.RegisterAllocation);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [1 locals]
PushConst   $literal_2
PopLocal    j$2
PushLocal   j$2
PushConst   $literal_1
CallI2      Plus
PopLocal    j$2
PushLocal   j$2
CallI1      Print
PushConst   $literal_3
PopLocal    j$2
PushLocal   j$2
PushConst   $literal_1
CallI2      Plus
PopLocal    j$2
PushLocal   j$2
CallI1      Print
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void RegisterAllocation_LocalSlotForEachType()
        {
            var source = @"
public void Main()
begin
  # Two uses for each so that FoldSingleUseLocals does not fire
  bool b true
  print(b)
  print(b)
  int i 1
  print(i)
  print(i)
  real r 2.0
  print(r)
  print(r)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.RegisterAllocation);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [3 locals]
PushConst   $literal_true
PopLocal    b$1
PushLocal   b$1
CallI1      Print
PushLocal   b$1
CallI1      Print
PushConst   $literal_1
PopLocal    i$2
PushLocal   i$2
CallI1      Print
PushLocal   i$2
CallI1      Print
PushConst   $literal_2r
PopLocal    r$3
PushLocal   r$3
CallI1      Print
PushLocal   r$3
CallI1      Print
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
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

        [Test]
        public void While_SimpleLoop()
        {
            var source = @"
private bool Main()
begin
  while false
  begin
    return false
  end
  return true
end";
            var program = CompileSingleFunction(source);

            var dis = @"
Bool main() [0 locals]
PushConst   $literal_false
JumpFalse   +4
PushConst   $literal_false
Return
Jump        -4
PushConst   $literal_true
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }

        [Test]
        public void While_RealisticLoop()
        {
            var source = @"
private int Main()
begin
  int i 0
  while <(i, 5)
  begin
    i = +(i, 1)
    print(i)
  end
  return i
end";
            var program = CompileSingleFunction(source);

            var dis = @"
Int main() [1 locals]
PushConst   $literal_0
PopLocal    i$1
PushLocal   i$1
PushConst   $literal_5
CallI2      Less
JumpFalse   +8
PushLocal   i$1
PushConst   $literal_1
CallI2      Plus
PopLocal    i$1
PushLocal   i$1
CallI1      Print
Jump        -10
PushLocal   i$1
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }

        [Test]
        public void While_EmptyLoop_Folded()
        {
            var source = @"
private bool Main()
begin
  while ==(0.123, Math.Cos(0.456))
  begin
  end
  return true
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.ConstantFolding);

            var dis = @"
Bool main() [0 locals]
PushConst   $literal_0.123r
PushConst   $literal_0.456r
CallI1      MathCos
CallI2      Equal
JumpFalse   +2
Jump        -5
PushConst   $literal_true
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }
    }
}
