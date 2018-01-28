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
    }
}
