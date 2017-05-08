using System;
using NUnit.Framework;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests.Parser
{
    partial class ModuleParserTests
    {
        [Test]
        public void If_Else()
        {
            var source = @"private int F()
begin
  if true
  begin
    return 2
  end
  else
  begin
    return -2
  end
end";
            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<IfSyntax>());
            var syntax = (IfSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(syntax.Condition, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)syntax.Condition).Value, Is.EqualTo(true));
            Assert.That(syntax.ThenBlock, Is.Not.Null);
            Assert.That(syntax.ThenBlock.Statements, Has.Exactly(1).Items);
            Assert.That(syntax.ElseBlock, Is.Not.Null);
            Assert.That(syntax.ElseBlock.Statements, Has.Exactly(1).Items);
        }

        [Test]
        public void If_NoElse()
        {
            var source = @"private int F()
begin
  if false
  begin
    return 2
  end
end";
            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<IfSyntax>());
            var syntax = (IfSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(syntax.Condition, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)syntax.Condition).Value, Is.EqualTo(false));
            Assert.That(syntax.ThenBlock, Is.Not.Null);
            Assert.That(syntax.ThenBlock.Statements, Has.Exactly(1).Items);
            Assert.That(syntax.ElseBlock, Is.Not.Null);
            Assert.That(syntax.ElseBlock.Statements, Has.Exactly(0).Items);
        }

        [Test]
        public void While()
        {
            var source = @"private void W()
begin
  while true
  begin
    print(2)
  end
end";
            var module = ParseStringWithoutDiagnostics(source);
            
            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<WhileSyntax>());
            var syntax = (WhileSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(syntax.Condition, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)syntax.Condition).Value, Is.EqualTo(true));
            Assert.That(syntax.CodeBlock, Is.Not.Null);
            Assert.That(syntax.CodeBlock.Statements, Has.Exactly(1).Items);
        }

        [Test]
        public void While_BlockRequired()
        {
            var source = @"private void W()
begin
  while true
    print(2)
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedBegin));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(5));
        }

        [Test]
        public void While_EndOfLineRequired()
        {
            var source = @"private void W()
begin
  while true begin
    print(2)
  end
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedEndOfLine));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(14));
        }
    }
}
