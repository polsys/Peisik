using System;
using NUnit.Framework;
using Polsys.Peisik.Compiler;

namespace Polsys.Peisik.Tests.Compiler
{
    partial class CompilerTests : CompilerTestBase
    {
        [Test]
        public void Block_InnerLocal()
        {
            var source = @"private void Main()
begin
  int a 0
  if <(a, 1)
  begin
    int b 1
  end
end";
            var program = CompileStringWithoutDiagnostics(source);
        }

        [Test]
        public void Block_LocalInMultipleInnerBlocks_SameType()
        {
            var source = @"private void Main()
begin
  int a 0
  if <(a, 1)
  begin
    int b 1
  end
  else
  begin
    int b 2
  end
end";
            var program = CompileStringWithoutDiagnostics(source);
        }

        [Test]
        public void Block_LocalInMultipleInnerBlocks_DifferentTypes()
        {
            var source = @"private void Main()
begin
  int a 0
  if <(a, 1)
  begin
    int b 1
  end
  else
  begin
    bool b false
  end
end";
            var program = CompileStringWithoutDiagnostics(source);
        }

        [Test]
        public void Block_UsingInnerLocalInOuterScope()
        {
            var source = @"private void Main()
begin
  int a 0
  if <(a, 1)
  begin
    int b 1
  end
  b = 2
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void Block_InnerLocalWithSameName()
        {
            var source = @"private void Main()
begin
  int a 0
  if <(a, 1)
  begin
    int a 1
  end
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(6));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(5));
        }

        [Test]
        public void If_EmptyBody()
        {
            var source = @"private void Main()
begin
  bool yes true

  if yes
  begin
  end
  else
  begin
  end
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Void main() [1 locals]
PushConst   $literal_true
PopLocal    yes
PushLocal   yes
JumpFalse   +1
Return";
            VerifyDisassembly(program.Functions[0], program, dis);
        }

        [Test]
        public void If_FullBody()
        {
            var source = @"private int Main()
begin
  bool yes true
  int result 0

  if yes
  begin
    result = 1
  end
  else
  begin
    result = 2
  end
  return result
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Int main() [2 locals]
PushConst   $literal_true
PopLocal    yes
PushConst   $literal_0
PopLocal    result
PushLocal   yes
JumpFalse   +4
PushConst   $literal_1
PopLocal    result
Jump        +3
PushConst   $literal_2
PopLocal    result
PushLocal   result
Return";
            VerifyDisassembly(program.Functions[0], program, dis);
        }

        [Test]
        public void If_NoElse()
        {
            var source = @"private int Main()
begin
  bool yes true
  int result 0

  if yes
  begin
    result = 1
  end
  return result
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Int main() [2 locals]
PushConst   $literal_true
PopLocal    yes
PushConst   $literal_0
PopLocal    result
PushLocal   yes
JumpFalse   +3
PushConst   $literal_1
PopLocal    result
PushLocal   result
Return";
            VerifyDisassembly(program.Functions[0], program, dis);
        }

        [Test]
        public void If_NonBoolCondition()
        {
            var source = @"private void Main()
begin
  if 2
  begin
  end
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(6));
        }

        [Test]
        public void While_EmptyLoop()
        {
            var source = @"private void Main()
begin
  while true
  begin
  end
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Void main() [0 locals]
PushConst   $literal_true
JumpFalse   +2
Jump        -2
Return";
            VerifyDisassembly(program.Functions[0], program, dis);
        }

        [Test]
        public void While_StatementInLoop()
        {
            var source = @"private bool Main()
begin
  while false
  begin
    return false
  end
  return true
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Bool main() [0 locals]
PushConst   $literal_false
JumpFalse   +4
PushConst   $literal_false
Return
Jump        -4
PushConst   $literal_true
Return";
            VerifyDisassembly(program.Functions[0], program, dis);
        }

        [Test]
        public void While_FunctionCallCondition()
        {
            var source = @"private bool IsTrue(bool a)
begin
  return a
end

private bool Main()
begin
  bool cond true
  while IsTrue(cond)
  begin
    return false
  end
  return true
end";
            var program = CompileStringWithoutDiagnostics(source);
            var dis = @"
Bool main() [1 locals]
PushConst   $literal_true
PopLocal    cond
PushLocal   cond
Call        istrue
JumpFalse   +4
PushConst   $literal_false
Return
Jump        -5
PushConst   $literal_true
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, dis);
        }

        [Test]
        public void While_NonBoolCondition()
        {
            var source = @"private void Main()
begin
  while 2
  begin
  end
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(9));
        }
    }
}
