using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class ExpressionTests
    {
        [TestCase(42L, PrimitiveType.Int)]
        [TestCase(1.0d, PrimitiveType.Real)]
        [TestCase(true, PrimitiveType.Bool)]
        public void ConstantExpression_Type(object value, PrimitiveType expectedType)
        {
            var expr = new ConstantExpression(value, null);
            Assert.That(expr.Type, Is.EqualTo(expectedType));
        }

        [Test]
        public void LocalLoadExpression_Type()
        {
            var local = new LocalVariable(PrimitiveType.Bool, "");
            var load = new LocalLoadExpression(local, null);

            Assert.That(load.Type, Is.EqualTo(PrimitiveType.Bool));
        }

        [Test]
        public void FunctionCallExpression_Type()
        {
            var syntax = new Peisik.Parser.FunctionSyntax(default(TokenPosition),
                PrimitiveType.Int, Peisik.Parser.Visibility.Private, "Function");
            var callee = Function.InitializeFromSyntax(syntax, null, "");
            var call = new FunctionCallExpression(callee, new List<Expression>(), false, null);

            Assert.That(call.Type, Is.EqualTo(PrimitiveType.Int));
        }

        [Test]
        public void UnaryExpression_Type()
        {
            var constant = new ConstantExpression(2.1, null);
            var unary = new UnaryExpression(InternalFunctions.Functions["math.ceil"], constant);

            Assert.That(unary.Type, Is.EqualTo(PrimitiveType.Int));
        }

        [Test]
        public void GetGuaranteesReturn_ReturnExpression()
        {
            var expr = new ReturnExpression(new ConstantExpression(0L, null));

            Assert.That(expr.GetGuaranteesReturn(), Is.True);
        }

        [Test]
        public void GetGuaranteesReturn_IfExpression_True()
        {
            var condition = new ConstantExpression(true, null);
            var thenExpr = new ReturnExpression(new ConstantExpression(0L, null));
            var elseExpr = new ReturnExpression(new ConstantExpression(1L, null));
            var expr = new IfExpression(condition, thenExpr, elseExpr);

            Assert.That(expr.GetGuaranteesReturn(), Is.True);
        }

        [Test]
        public void GetGuaranteesReturn_IfExpression_NoElse()
        {
            var condition = new ConstantExpression(true, null);
            var thenExpr = new ReturnExpression(new ConstantExpression(0L, null));
            var expr = new IfExpression(condition, thenExpr, null);

            Assert.That(expr.GetGuaranteesReturn(), Is.False);
        }

        [Test]
        public void GetGuaranteesReturn_IfExpression_False1()
        {
            var condition = new ConstantExpression(true, null);
            var thenExpr = new ConstantExpression(0L, null);
            var elseExpr = new ReturnExpression(new ConstantExpression(1L, null));
            var expr = new IfExpression(condition, thenExpr, elseExpr);

            Assert.That(expr.GetGuaranteesReturn(), Is.False);
        }

        [Test]
        public void GetGuaranteesReturn_IfExpression_False2()
        {
            var condition = new ConstantExpression(true, null);
            var thenExpr = new ReturnExpression(new ConstantExpression(0L, null));
            var elseExpr = new ConstantExpression(1L, null);
            var expr = new IfExpression(condition, thenExpr, elseExpr);

            Assert.That(expr.GetGuaranteesReturn(), Is.False);
        }

        [Test]
        public void GetGuaranteesReturn_SequenceExpression_False()
        {
            var first = new ConstantExpression(1L, null);
            var second = new ConstantExpression(0L, null);
            var expr = new SequenceExpression();
            expr.Expressions.Add(first);
            expr.Expressions.Add(second);

            Assert.That(expr.GetGuaranteesReturn(), Is.False);
        }

        [Test]
        public void GetGuaranteesReturn_SequenceExpression_True1()
        {
            var first = new ConstantExpression(1L, null);
            var second = new ReturnExpression(new ConstantExpression(0L, null));
            var expr = new SequenceExpression();
            expr.Expressions.Add(first);
            expr.Expressions.Add(second);

            Assert.That(expr.GetGuaranteesReturn(), Is.True);
        }

        [Test]
        public void GetGuaranteesReturn_SequenceExpression_True2()
        {
            var first = new ReturnExpression(new ConstantExpression(1L, null));
            var second = new ReturnExpression(new ConstantExpression(0L, null));
            var expr = new SequenceExpression();
            expr.Expressions.Add(first);
            expr.Expressions.Add(second);

            Assert.That(expr.GetGuaranteesReturn(), Is.True);
        }

        [Test]
        public void GetGuaranteesReturn_WhileExpression_AlwaysFalse()
        {
            // This is just a safeguard for my idiocy.
            // Even if the loop body was guaranteed to return, it wouldn't matter as the condition may be false.

            var condition = new ConstantExpression(true, null);
            var loop = new ReturnExpression(new ConstantExpression(0L, null));
            var expr = new WhileExpression(condition, loop);

            Assert.That(expr.GetGuaranteesReturn(), Is.False);
        }
    }
}
