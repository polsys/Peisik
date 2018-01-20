﻿using System.Collections.Generic;
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
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            return function;
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
        public void LocalAssignment()
        {
            var source = @"
public void Main()
begin
  real local 4.0
  local = 5.0
end";
            var function = SingleFunctionFromSyntax(source);

            Assert.That(function.Locals, Has.Exactly(2).Items);
            var local = function.Locals[1];
            Assert.That(local.Name, Does.StartWith("local"));
            Assert.That(local.Type, Is.EqualTo(PrimitiveType.Real));

            // The expression tree should be
            // (root)
            //   |-- Constant -> local
            //   |-- Constant -> local
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(2).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[0].Store, Is.SameAs(local));

            Assert.That(sequence.Expressions[1], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[1].Store, Is.SameAs(local));
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

        [Test]
        public void ConstantReturning()
        {
            var source = @"
private int Result 42 # Actually added to the compilation in code

public int Main()
begin
  return Result
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            compiler.AddConstant("Result", "", Visibility.Private, 42L);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- Return ConstantExpression(42)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)ret.Value).Value, Is.EqualTo(42L));
        }
    }
}
