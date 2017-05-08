using System;
using NUnit.Framework;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests.Parser
{
    partial class ModuleParserTests
    {
        [Test]
        public void VariableDecl_Int()
        {
            var source = @"private void Stupid()
begin
  int a -1234
end";

            var module = ParseStringWithoutDiagnostics(source);
            
            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());
            var decl = (VariableDeclarationSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(decl.InitialValue, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)decl.InitialValue).Value, Is.EqualTo(-1234L));
            Assert.That(decl.Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(decl.Name, Is.EqualTo("a"));
        }

        [Test]
        public void VariableDecl_InvalidName()
        {
            var source = @"private void Stupid()
begin
  int 3 -1234
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(7));
        }

        [Test]
        public void VariableDecl_Void()
        {
            var source = @"private void Stupid()
begin
  void a
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.VoidMayOnlyBeUsedForReturn));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void FunctionCall_WithVariable()
        {
            var source = @"private void FunctionCall()
begin
  int one 1
  print(one)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(2).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());
            Assert.That(module.Functions[0].CodeBlock.Statements[1], Is.InstanceOf<FunctionCallStatementSyntax>());
            var call = (FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[1];
            Assert.That(call.Expression.FunctionName, Is.EqualTo("print"));

            Assert.That(call.Expression.Parameters, Has.Exactly(1).Items);
            Assert.That(call.Expression.Parameters[0], Is.InstanceOf<IdentifierSyntax>());
            Assert.That(((IdentifierSyntax)call.Expression.Parameters[0]).Name, Is.EqualTo("one"));
        }

        [Test]
        public void FunctionCall_WithLiteral()
        {
            var source = @"private void FunctionCall()
begin
  print(1)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);
            
            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<FunctionCallStatementSyntax>());
            var call = (FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(call.Expression.FunctionName, Is.EqualTo("print"));
            Assert.That(call.Expression.Parameters, Has.Exactly(1).Items);
            Assert.That(call.Expression.Parameters[0], Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)call.Expression.Parameters[0]).Value, Is.InstanceOf<long>().And.EqualTo(1));
        }

        [Test]
        public void FunctionCall_WithRealLiteral()
        {
            var source = @"private void FunctionCall()
begin
  print(4.0)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<FunctionCallStatementSyntax>());
            var call = (FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[0];
            Assert.That(call.Expression.FunctionName, Is.EqualTo("print"));
            Assert.That(call.Expression.Parameters, Has.Exactly(1).Items);
            Assert.That(call.Expression.Parameters[0], Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)call.Expression.Parameters[0]).Value, Is.InstanceOf<double>().And.EqualTo(4.0));
        }

        [Test]
        public void FunctionCall_WithTwoVariablesAndLiteral()
        {
            var source = @"private void FunctionCall()
begin
  int one 1
  int minustwo -2
  print(one, minustwo, 3)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(3).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());
            Assert.That(module.Functions[0].CodeBlock.Statements[1], Is.InstanceOf<VariableDeclarationSyntax>());
            Assert.That(module.Functions[0].CodeBlock.Statements[2], Is.InstanceOf<FunctionCallStatementSyntax>());

            var call = ((FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[2]).Expression;
            Assert.That(call.FunctionName, Is.EqualTo("print"));
            Assert.That(call.Parameters, Has.Exactly(3).Items);
            Assert.That(call.Parameters[0], Is.InstanceOf<IdentifierSyntax>());
            Assert.That(((IdentifierSyntax)call.Parameters[0]).Name, Is.EqualTo("one"));
            Assert.That(call.Parameters[1], Is.InstanceOf<IdentifierSyntax>());
            Assert.That(((IdentifierSyntax)call.Parameters[1]).Name, Is.EqualTo("minustwo"));
            Assert.That(call.Parameters[2], Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)call.Parameters[2]).Value, Is.EqualTo(3));
        }

        [Test]
        public void FunctionCall_WithFunctionCall()
        {
            var source = @"private void FunctionCall()
begin
  print(double(2))
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);
            
            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<FunctionCallStatementSyntax>());
            var call = ((FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[0]).Expression;
            Assert.That(call.FunctionName, Is.EqualTo("print"));
            Assert.That(call.Parameters, Has.Exactly(1).Items);
            Assert.That(call.Parameters[0], Is.InstanceOf<FunctionCallSyntax>());
            Assert.That(((FunctionCallSyntax)call.Parameters[0]).FunctionName, Is.EqualTo("double"));
            Assert.That(((FunctionCallSyntax)call.Parameters[0]).Parameters, Has.Exactly(1).Items);
        }

        [Test]
        public void FunctionCall_InvalidFunctionName()
        {
            var source = @"private void FunctionCall()
begin
  123(2)
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void FunctionCall_InternalFunction()
        {
            var source = @"private void AddUp()
begin
  +(2, 4)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<FunctionCallStatementSyntax>());
        }

        [Test]
        public void FunctionCall_InvalidParameterLiteral()
        {
            var source = @"private void FunctionCall()
begin
  print(begin)
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(9));
        }

        [Test]
        public void FunctionCall_Namespace()
        {
            var source = @"private void Root()
begin
  Math.Sqrt(2)
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<FunctionCallStatementSyntax>());
            Assert.That(((FunctionCallStatementSyntax)module.Functions[0].CodeBlock.Statements[0]).Expression.FunctionName,
                Is.EqualTo("Math.Sqrt"));
        }

        [Test]
        public void FunctionCall_NamespaceError1()
        {
            var source = @"private void RootFail()
begin
  Math.(2)
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void FunctionCall_NamespaceError2()
        {
            var source = @"private void RootFail()
begin
  .Sqrt(2)
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void Return_Literal()
        {
            var source = @"private int Random()
begin
  # Chosen by a fair dice roll, guaranteed to be random
  return 4
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<ReturnSyntax>());
            var ret = ((ReturnSyntax)module.Functions[0].CodeBlock.Statements[0]);
            Assert.That(ret.Expression, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)ret.Expression).Value, Is.EqualTo(4));
        }

        [Test]
        public void Return_LiteralBool()
        {
            var source = @"private bool AreWeThereYet()
begin
  return false
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<ReturnSyntax>());
            var ret = ((ReturnSyntax)module.Functions[0].CodeBlock.Statements[0]);
            Assert.That(ret.Expression, Is.InstanceOf<LiteralSyntax>());
            Assert.That(((LiteralSyntax)ret.Expression).Value, Is.EqualTo(false));
        }

        [Test]
        public void Return_FunctionCall()
        {
            var source = @"private int Alea()
begin
  return IactaEst()
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<ReturnSyntax>());
            var ret = ((ReturnSyntax)module.Functions[0].CodeBlock.Statements[0]);
            Assert.That(ret.Expression, Is.InstanceOf<FunctionCallSyntax>());
            Assert.That(((FunctionCallSyntax)ret.Expression).FunctionName, Is.EqualTo("IactaEst"));
        }

        [Test]
        public void Return_Void()
        {
            var source = @"private void Main()
begin
  return void
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<ReturnSyntax>());
            var ret = ((ReturnSyntax)module.Functions[0].CodeBlock.Statements[0]);
            Assert.That(ret.Expression, Is.Null);
        }

        [Test]
        public void Assignment()
        {
            var source = @"private void Main()
begin
  a = 10
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
            Assert.That(module.Functions[0].CodeBlock.Statements, Has.Exactly(1).Items);

            Assert.That(module.Functions[0].CodeBlock.Statements[0], Is.InstanceOf<AssignmentSyntax>());
            var statement = ((AssignmentSyntax)module.Functions[0].CodeBlock.Statements[0]);
            Assert.That(statement.Expression, Is.InstanceOf<LiteralSyntax>());
            Assert.That(statement.Target, Is.EqualTo("a"));
        }

        [Test]
        public void FutureReservedKeyword()
        {
            var source = @"private void Fail()
begin
  int for 0
end";

            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(7));
        }
    }
}
