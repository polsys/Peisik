using System;
using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class FunctionTests : CompilerTestBase
    {
        [Test]
        public void EmptyFunction_NoParams()
        {
            var syntax = new FunctionSyntax(new TokenPosition(), PrimitiveType.Void, Visibility.Public, "Main");
            syntax.SetBlock(new BlockSyntax(new TokenPosition()));
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>(), Optimization.None);

            var function = Function.FromSyntax(syntax, compiler);

            Assert.That(function.ResultValue, Is.Not.Null);
            Assert.That(function.ResultValue.Type, Is.EqualTo(PrimitiveType.Void));
            Assert.That(function.ResultValue.AssignmentCount, Is.Zero);
            Assert.That(function.ResultValue.UseCount, Is.Zero);
            Assert.That(function.ResultValue.Name, Is.EqualTo("$result"));
        }

        [Test]
        public void EmptyFunction_Params()
        {
            var syntax = new FunctionSyntax(new TokenPosition(), PrimitiveType.Void, Visibility.Public, "Main");
            syntax.SetBlock(new BlockSyntax(new TokenPosition()));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Int, "intParam"));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Bool, "boolParam"));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Real, "realParam"));
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>(), Optimization.None);

            var function = Function.FromSyntax(syntax, compiler);

            Assert.That(function.Locals, Has.Exactly(4).Items);
            Assert.That(function.Locals[0].Name, Is.EqualTo("$result"));
            Assert.That(function.Locals[0].Type, Is.EqualTo(PrimitiveType.Void));
            Assert.That(function.Locals[1].Name, Is.EqualTo("intparam"));
            Assert.That(function.Locals[1].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(function.Locals[2].Name, Is.EqualTo("boolparam"));
            Assert.That(function.Locals[2].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(function.Locals[3].Name, Is.EqualTo("realparam"));
            Assert.That(function.Locals[3].Type, Is.EqualTo(PrimitiveType.Real));
        }

        [Test]
        public void ConstantReturning()
        {
            var source = @"
public int Main()
begin
  return 5
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);

            var function = Function.FromSyntax(syntax.Functions[0], compiler);

            // The expression tree should be
            // (root)
            //   |-- Return $result
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)ret.Value).Value, Is.EqualTo(5L));
        }

        [Test]
        public void LocalReturning()
        {
            var source = @"
public int Main()
begin
  int LOCAL 5
  return local
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);

            var function = Function.FromSyntax(syntax.Functions[0], compiler);

            Assert.That(function.Locals, Has.Exactly(2).Items);
            var local = function.Locals[1];
            Assert.That(local.Name, Does.StartWith("local"));
            Assert.That(local.Type, Is.EqualTo(PrimitiveType.Int));

            // The expression tree should be
            // (root)
            //   |-- Constant -> local
            //   |-- Return local
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(2).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[0].Store, Is.SameAs(local));

            Assert.That(sequence.Expressions[1], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[1];
            Assert.That(ret.Value, Is.InstanceOf<LocalLoadExpression>());
            Assert.That(((LocalLoadExpression)ret.Value).Local, Is.SameAs(local));
        }
        
        [Test]
        public void Error_LocalAlreadyThere()
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
    }
}
