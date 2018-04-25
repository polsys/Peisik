using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class UnaryExpressionTests
    {
        [Test]
        public void ConstantFolding_Minus_Int()
        {
            var expr = new UnaryExpression(InternalFunctions.Functions["-"],
                new ConstantExpression(12L, null));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(-12L));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Minus_Real()
        {
            var expr = new UnaryExpression(InternalFunctions.Functions["-"],
                new ConstantExpression(12.0, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(-12.0).Within(0.0001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Not_Bool()
        {
            var expr = new UnaryExpression(InternalFunctions.Functions["not"],
                new ConstantExpression(true, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(false));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Not_Int()
        {
            var expr = new UnaryExpression(InternalFunctions.Functions["not"],
                new ConstantExpression(12L, null));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(~12L));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Recurses()
        {
            var expr = new UnaryExpression(InternalFunctions.Functions["-"],
                new UnaryExpression(InternalFunctions.Functions["-"],
                    new ConstantExpression(12L, null)));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(12L));
            Assert.That(folded.Store, Is.SameAs(local));
        }
    }
}
