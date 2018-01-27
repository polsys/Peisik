using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class BinaryExpressionTests
    {
        [Test]
        public void ConstantFolding_Plus_TwoInts()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(2L, null), new ConstantExpression(3L, null));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(5L));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Plus_TwoReals()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(2.0, null), new ConstantExpression(3.0, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(5.0).Within(0.00001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Plus_MixedTypes()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(2L, null), new ConstantExpression(3.0, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(5.0).Within(0.00001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Equal_Bool_True()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["=="],
                new ConstantExpression(false, null), new ConstantExpression(false, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(true));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Equal_Int_False()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["=="],
                new ConstantExpression(2L, null), new ConstantExpression(3L, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(false));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_Equal_Real_True()
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["=="],
                new ConstantExpression(2.0, null), new ConstantExpression(2.0, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(true));
            Assert.That(folded.Store, Is.SameAs(local));
        }
    }
}
