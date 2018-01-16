using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    partial class ExpressionTreeTests : CompilerTestBase
    {
        private Function SingleFunctionFromSyntax(string source)
        {
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);

            return Function.FromSyntax(syntax.Functions[0], compiler);
        }

        [Test]
        public void LiteralReturning()
        {
            var source = @"
public int Main()
begin
  return 5
end";
            var function = SingleFunctionFromSyntax(source);

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
            var function = SingleFunctionFromSyntax(source);

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
    }
}
