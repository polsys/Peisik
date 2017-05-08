using System;
using NUnit.Framework;
using Polsys.Peisik.Compiler;

namespace Polsys.Peisik.Tests.Compiler
{
    partial class CompilerTests : CompilerTestBase
    {
        [Test]
        public void NeverReturns_Linear()
        {
            var source = @"private int Main()
begin
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ReturnNotGuaranteed));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Main"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void NeverReturns_If1()
        {
            var source = @"private bool Condition true

private int Main()
begin
  if Condition
  begin
    return 1
  end
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ReturnNotGuaranteed));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void NeverReturns_If2()
        {
            var source = @"private bool Condition true

private int Main()
begin
  if Condition
  begin
  end
  else
  begin
    return 1
  end
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ReturnNotGuaranteed));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void UnreachableCode_Linear()
        {
            var source = @"private int Main()
begin
  return 100
  return 200
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.UnreachableCode));
            Assert.That(diagnostics[0].IsError, Is.False);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void UnreachableCode_NotFiredMultipleTimes()
        {
            var source = @"private int Main()
begin
  return 100
  return 200
  return 300
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.UnreachableCode));
            Assert.That(diagnostics[0].IsError, Is.False);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void UnreachableCode_If()
        {
            var source = @"private bool Condition true

private int Main()
begin
  if Condition
  begin
    return 100
  end
  else
  begin
    return 200
  end
  return 300
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.UnreachableCode));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(13));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }
    }
}
