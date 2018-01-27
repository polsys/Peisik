using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    partial class ErrorTests : CompilerTestBase
    {
        [Test]
        public void AssignmentToConst()
        {
            var source = @"
private int Value 4

public void Main()
begin
  Value = 4
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.MayNotAssignToConst));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Value"));
        }

        [Test]
        public void FunctionCall_NotEnoughParameters()
        {
            var source = @"
private void Function(int a, bool b)
begin
end

public void Main()
begin
  Function(1)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NotEnoughParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("1"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
        }

        [Test]
        public void FunctionCall_TooManyParameters()
        {
            var source = @"
private void Function(int a, bool b)
begin
end

public void Main()
begin
  Function(1, true, 3.0)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.TooManyParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("3"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
        }

        [Test]
        public void FunctionCall_WrongType()
        {
            var source = @"
private void Function(int a, bool b)
begin
end

public void Main()
begin
  Function(1, 3.0)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Real"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
        }

        [Test]
        public void InternalCall_NotEnoughParams()
        {
            var source = @"
public void Main()
begin
  +(1)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NotEnoughParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("1"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
        }

        [Test]
        public void InternalCall_TooManyParameters()
        {
            var source = @"
public void Main()
begin
  +(1,2,3)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.TooManyParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("3"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
        }

        [Test]
        public void InternalCall_AnyNumeric_NonNumeric()
        {
            var source = @"
public void Main()
begin
  +(1, true)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int|Real"));
        }

        [Test]
        public void InternalCall_Int_NonInteger()
        {
            var source = @"
public void Main()
begin
  %(1, 3.0)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Real"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void InternalCall_SameType_NotSame()
        {
            var source = @"
public void Main()
begin
  ==(1, 3.0)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ParamsMustBeSameType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Real"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void InternalCall_BoolOrInt_NotEither()
        {
            var source = @"
public void Main()
begin
  xor(true, 3.0)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Real"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool|Int"));
        }

        [Test]
        public void InternalCall_BoolOrInt_NotSame()
        {
            var source = @"
public void Main()
begin
  xor(true, 3)
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ParamsMustBeSameType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
        }

        [Test]
        public void InternalCall_AnyType_VoidNotAllowed()
        {
            var source = @"
private void Func()
begin
end

public void Main()
begin
  print(Func())
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Void"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool|Int|Real"));
        }

        [Test]
        public void If_NonBoolCondition()
        {
            var source = @"
public void Main()
begin
  if 2
  begin
  end
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
        }

        [Test]
        public void LocalAlreadyDefined()
        {
            var source = @"
public int Main()
begin
  int Local 5
  bool loCal true
  return local
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
        }

        [Test]
        public void MismatchingTypesInAssignment()
        {
            var source = @"
public int Main()
begin
  int local 5
  local = true
  return local
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void MismatchingTypesInLocalDecl()
        {
            var source = @"
public int Main()
begin
  int local true
  return local
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void MismatchingTypesInLocalDecl2()
        {
            var source = @"
public int Main()
begin
  bool local true
  int local2 local
  return local2
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void MismatchingTypesInReturn()
        {
            var source = @"
public int Main()
begin
  return true
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
        }

        [Test]
        public void NameNotFound_Local()
        {
            var source = @"
public void Main()
begin
  LOCAL = 5
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("LOCAL"));
        }

        [Test]
        public void NameNotFound_Function()
        {
            var source = @"
public int Main()
begin
  return Something()
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Something"));
        }
    }
}
