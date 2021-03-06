﻿using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class BinaryExpressionTests
    {
        [TestCase("and", true, true, true)]
        [TestCase("and", true, false, false)]
        [TestCase("or", false, false, false)]
        [TestCase("or", true, false, true)]
        [TestCase("xor", true, true, false)]
        [TestCase("xor", true, false, true)]
        public void ConstantFolding_TwoBools(string function, bool left, bool right, bool expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase("+", 2L, 3L, 5L)]
        [TestCase("-", 2L, 3L, -1L)]
        [TestCase("*", 2L, 3L, 6L)]
        [TestCase("%", 7L, 3L, 1L)]
        [TestCase("%", 7L, -3L, 1L)]
        [TestCase("%", -7L, 3L, 2L)]
        [TestCase("and", 0b1100L, 0b1010L, 0b1000L)]
        [TestCase("or", 0b1100L, 0b1010L, 0b1110L)]
        [TestCase("xor", 0b1100L, 0b1010L, 0b0110L)]
        public void ConstantFolding_TwoInts(string function, long left, long right, long expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase("+", 2.0, 3.0, 5.0)]
        [TestCase("-", 2.0, 3.0, -1.0)]
        [TestCase("*", 2.0, 3.0, 6.0)]
        public void ConstantFolding_TwoReals(string function, double left, double right, double expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected).Within(0.00001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase("+", 2L, 3.0, 5.0)]
        [TestCase("-", 2L, 3.0, -1.0)]
        [TestCase("*", 2L, 3.0, 6.0)]
        public void ConstantFolding_MixedTypes(string function, long left, double right, double expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected).Within(0.00001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase(4L, 3L, 4.0 / 3.0)]
        [TestCase(4.0, 3.0, 4.0 / 3.0)]
        public void ConstantFolding_Division(object left, object right, double expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["/"],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected).Within(0.00001));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase(4L, 3L, 1)]
        [TestCase(4.0, 3.0, 1)]
        [TestCase(-4L, 3L, -1)]
        [TestCase(-4.0, 3.0, -1)]
        public void ConstantFolding_FloorDivision(object left, object right, long expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions["//"],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Int, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [TestCase("/", 1L, 0L)]
        [TestCase("/", 1L, 0.0)]
        [TestCase("/", 1.0, 0.0)]
        [TestCase("//", 1L, 0L)]
        [TestCase("//", 1L, 0.0)]
        [TestCase("//", 1.0, 0.0)]
        [TestCase("%", 1L, 0L)]
        public void ConstantFolding_Division_ByZero(string function, object left, object zero)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(zero, null));
            var local = new LocalVariable(PrimitiveType.Real, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            // Folding not performed
            Assert.That(folded, Is.InstanceOf<BinaryExpression>());
        }

        [TestCase("==", true, false, false)]
        [TestCase("!=", true, false, true)]
        [TestCase("==", 1L, 2L, false)]
        [TestCase("!=", 1L, 2L, true)]
        [TestCase("<", 1L, 2L, true)]
        [TestCase("<=", 1L, 2L, true)]
        [TestCase(">", 1L, 2L, false)]
        [TestCase(">=", 1L, 2L, false)]
        [TestCase("==", 1.0, 2.0, false)]
        [TestCase("!=", 1.0, 2.0, true)]
        [TestCase("<", 1.0, 2.0, true)]
        [TestCase("<=", 1.0, 2.0, true)]
        [TestCase(">", 1.0, 2.0, false)]
        [TestCase(">=", 1.0, 2.0, false)]
        [TestCase("==", 1L, 2.0, false)]
        [TestCase("!=", 1L, 2.0, true)]
        [TestCase("<", 1L, 2.0, true)]
        [TestCase("<=", 1L, 2.0, true)]
        [TestCase(">", 1L, 2.0, false)]
        [TestCase(">=", 1L, 2.0, false)]
        public void ConstantFolding_Comparisons(string function, object left, object right, bool expected)
        {
            var expr = new BinaryExpression(InternalFunctions.Functions[function],
                new ConstantExpression(left, null), new ConstantExpression(right, null));
            var local = new LocalVariable(PrimitiveType.Bool, "");
            expr.Store = local;
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(expected));
            Assert.That(folded.Store, Is.SameAs(local));
        }

        [Test]
        public void ConstantFolding_FoldsParamToNonFoldable()
        {
            var left = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(1L, null), new ConstantExpression(2L, null));
            var right = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(3L, null), new ConstantExpression(4L, null));
            var expr = new BinaryExpression(InternalFunctions.Functions["math.pow"], left, right);
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<BinaryExpression>());
            Assert.That(((BinaryExpression)folded).Left, Is.InstanceOf<ConstantExpression>());
            Assert.That(((BinaryExpression)folded).Right, Is.InstanceOf<ConstantExpression>());
        }

        [Test]
        public void ConstantFolding_Recurses()
        {
            var left = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(1L, null), new ConstantExpression(2L, null));
            var right = new BinaryExpression(InternalFunctions.Functions["+"],
                new ConstantExpression(3L, null), new ConstantExpression(4L, null));
            var expr = new BinaryExpression(InternalFunctions.Functions["+"], left, right);
            var folded = expr.Fold(null);

            Assert.That(folded, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)folded).Value, Is.EqualTo(10));
        }
    }
}
