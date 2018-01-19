using System;
using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class SequenceExpressionTests : CompilerTestBase
    {
        [Test]
        public void FoldSingleUseLocals_FoldsCorrectly()
        {
            var local = new LocalVariable(PrimitiveType.Int, "local");
            var assign = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 5), null, local);
            var ret = new ReturnExpression(new LocalLoadExpression(local, null));
            var sequence = new SequenceExpression();
            sequence.Expressions.Add(assign);
            sequence.Expressions.Add(ret);

            sequence.FoldSingleUseLocals();

            Assert.That(sequence.Expressions, Has.Exactly(1).Items);
            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)sequence.Expressions[0]).Value, Is.InstanceOf<ConstantExpression>());
            var c = (ConstantExpression)((ReturnExpression)sequence.Expressions[0]).Value;
            Assert.That(c.Value, Is.SameAs(assign.Value));
            Assert.That(c.Store, Is.Null);
            Assert.That(local.AssignmentCount, Is.Zero);
            Assert.That(local.UseCount, Is.Zero);
        }

        [Test]
        public void FoldSingleUseLocals_FoldsMultipleAssignSingleUse()
        {
            var local = new LocalVariable(PrimitiveType.Int, "local")
            {
                AssignmentCount = 1
            };
            var assign = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 5), null, local);
            var ret = new ReturnExpression(new LocalLoadExpression(local, null));
            var sequence = new SequenceExpression();
            sequence.Expressions.Add(assign);
            sequence.Expressions.Add(ret);

            sequence.FoldSingleUseLocals();

            Assert.That(sequence.Expressions, Has.Exactly(1).Items);
            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)sequence.Expressions[0]).Value, Is.InstanceOf<ConstantExpression>());
            var c = (ConstantExpression)((ReturnExpression)sequence.Expressions[0]).Value;
            Assert.That(c.Value, Is.SameAs(assign.Value));
            Assert.That(c.Store, Is.Null);
            Assert.That(local.AssignmentCount, Is.EqualTo(1));
            Assert.That(local.UseCount, Is.Zero);
        }

        [Test]
        public void FoldSingleUseLocals_FoldsChain()
        {
            // int local1 5
            // int local2 local1
            // return local2
            //   =>
            // return 5
            var local1 = new LocalVariable(PrimitiveType.Int, "local1");
            var local2 = new LocalVariable(PrimitiveType.Int, "local2");
            var assign1 = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 5), null, local1);
            var assign2 = new LocalLoadExpression(local1, null, local2);
            var ret = new ReturnExpression(new LocalLoadExpression(local2, null));
            var sequence = new SequenceExpression();
            sequence.Expressions.Add(assign1);
            sequence.Expressions.Add(assign2);
            sequence.Expressions.Add(ret);

            sequence.FoldSingleUseLocals();

            Assert.That(sequence.Expressions, Has.Exactly(1).Items);
            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)sequence.Expressions[0]).Value, Is.InstanceOf<ConstantExpression>());
            var c = (ConstantExpression)((ReturnExpression)sequence.Expressions[0]).Value;
            Assert.That(c.Value, Is.SameAs(assign1.Value));
            Assert.That(c.Store, Is.Null);
            Assert.That(local1.AssignmentCount, Is.Zero);
            Assert.That(local1.UseCount, Is.Zero);
            Assert.That(local2.AssignmentCount, Is.Zero);
            Assert.That(local2.UseCount, Is.Zero);
        }

        [Test]
        public void FoldSingleUseLocals_DoesNotFoldNonAdjacent()
        {
            var local1 = new LocalVariable(PrimitiveType.Int, "local");
            var local2 = new LocalVariable(PrimitiveType.Int, "other");
            var assign = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 5), null, local1);
            var assign2 = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 6), null, local2);
            var ret = new ReturnExpression(new LocalLoadExpression(local1, null));
            var sequence = new SequenceExpression();
            sequence.Expressions.Add(assign);
            sequence.Expressions.Add(assign2);
            sequence.Expressions.Add(ret);

            sequence.FoldSingleUseLocals();

            Assert.That(sequence.Expressions, Has.Exactly(3).Items);
            Assert.That(sequence.Expressions[2], Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)sequence.Expressions[2]).Value, Is.InstanceOf<LocalLoadExpression>());
            Assert.That(local1.AssignmentCount, Is.EqualTo(1));
            Assert.That(local1.UseCount, Is.EqualTo(1));

            Assert.Inconclusive("Allow reordering once expression purity can be determined.");
        }

        [Test]
        public void FoldSingleUseLocals_DoesNotFoldMultipleUses()
        {
            var local = new LocalVariable(PrimitiveType.Int, "local")
            {
                UseCount = 1
            };
            var assign = new ConstantExpression(LiteralSyntax.CreateIntLiteral(new TokenPosition(), 5), null, local);
            var ret = new ReturnExpression(new LocalLoadExpression(local, null));
            var sequence = new SequenceExpression();
            sequence.Expressions.Add(assign);
            sequence.Expressions.Add(ret);

            sequence.FoldSingleUseLocals();

            Assert.That(sequence.Expressions, Has.Exactly(2).Items);
            Assert.That(sequence.Expressions[1], Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)sequence.Expressions[1]).Value, Is.InstanceOf<LocalLoadExpression>());
            Assert.That(local.AssignmentCount, Is.EqualTo(1));
            Assert.That(local.UseCount, Is.EqualTo(2));
        }
    }
}
