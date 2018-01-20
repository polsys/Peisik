﻿using System.Collections.Generic;
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
    }
}