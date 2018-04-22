using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class RegisterAllocatorTests
    {
        [Test]
        public void ComputeIntervals_Empty()
        {
            var function = new Function(null);
            function.ExpressionTree = new ReturnExpression(null);

            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.ComputeIntervals(function),
                Throws.Nothing);
        }

        [Test]
        public void ComputeIntervals_SimpleBlock()
        {
            var function = new Function(null);
            var local = new LocalVariable(PrimitiveType.Int, "");
            function.ExpressionTree = new SequenceExpression(new List<Expression>()
            {
                new ConstantExpression(2L, null, local),
                new ReturnExpression(new LocalLoadExpression(local, null)),
            });

            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.ComputeIntervals(function),
                Throws.Nothing);

            Assert.That(local.IntervalStart, Is.EqualTo(0));
            Assert.That(local.IntervalEnd, Is.EqualTo(1));
        }

        [Test]
        public void ComputeIntervals_SeveralLocals()
        {
            var function = new Function(null);
            var local1 = new LocalVariable(PrimitiveType.Int, "");
            var local2 = new LocalVariable(PrimitiveType.Int, "");
            function.ExpressionTree = new SequenceExpression(new List<Expression>()
            {
                new ConstantExpression(2L, null, local1), // 0
                new ConstantExpression(3L, null, local2), // 1
                new ReturnExpression(new BinaryExpression(InternalFunctions.Functions["+"], // 4
                    new LocalLoadExpression(local1, null), // 2
                    new LocalLoadExpression(local2, null))), // 3
            });

            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.ComputeIntervals(function),
                Throws.Nothing);

            Assert.That(local1.IntervalStart, Is.EqualTo(0));
            Assert.That(local1.IntervalEnd, Is.EqualTo(2));

            Assert.That(local2.IntervalStart, Is.EqualTo(1));
            Assert.That(local2.IntervalEnd, Is.EqualTo(3));
        }

        [Test]
        public void ComputeIntervals_Parameter()
        {
            var function = new Function(null);
            var local1 = new LocalVariable(PrimitiveType.Bool, "");
            local1.IsParameter = true;
            var local2 = new LocalVariable(PrimitiveType.Bool, "");
            local2.IsParameter = true;
            function.ExpressionTree = new SequenceExpression(new List<Expression>()
            {
                new IfExpression(new LocalLoadExpression(local1, null), // 0
                    new ReturnExpression(new LocalLoadExpression(local2, null)), // 2(1)
                    new ReturnExpression(new LocalLoadExpression(local1, null))) // 4(3)
            });

            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.ComputeIntervals(function),
                Throws.Nothing);

            // Parameter lifetimes start at -1 so that they get the first stack positions
            Assert.That(local1.IntervalStart, Is.EqualTo(-1));
            Assert.That(local1.IntervalEnd, Is.EqualTo(3));

            Assert.That(local2.IntervalStart, Is.EqualTo(-1));
            Assert.That(local2.IntervalEnd, Is.EqualTo(1));
        }

        [Test]
        public void ComputeIntervals_MiscExpressions()
        {
            var function = new Function(null);
            function.ResultValue = new LocalVariable(PrimitiveType.Bool, "stupid_hack");
            var local = new LocalVariable(PrimitiveType.Bool, "");
            function.ExpressionTree = new SequenceExpression(new List<Expression>()
            {
                new WhileExpression(new LocalLoadExpression(local, null), // 0
                    new SequenceExpression(new List<Expression>()
                    {
                        new PrintExpression(new List<Expression>() // Still 2, does not store
                        {
                            new UnaryExpression(InternalFunctions.Functions["-"], // 2
                                new LocalLoadExpression(local, null)) // 1
                        }),
                        new FailFastExpression(), // Still 1, does not store
                        new FunctionCallExpression(function, new List<Expression>() { // 4
                            new ConstantExpression(false, null) }, false, null) // 3
                        {
                            Store = local
                        }
                    })),
            });

            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.ComputeIntervals(function),
                Throws.Nothing);

            Assert.That(local.IntervalStart, Is.EqualTo(0));
            Assert.That(local.IntervalEnd, Is.EqualTo(4));
        }

        [Test]
        public void AssignRegisters_EmptyFunction()
        {
            Assert.That(() => RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(
                new List<LocalVariable>(), out _),
                Throws.Nothing);
        }

        [Test]
        public void AssignRegisters_EmptyInterval()
        {
            var local = new LocalVariable(PrimitiveType.Real, "");
            local.StorageLocation = 0;
            var locals = new List<LocalVariable>() { local };

            RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(locals, out var stackSize);

            // The trivial backend would assign this to -1 if allowed to
            Assert.That(local.StorageLocation, Is.EqualTo(0));
            Assert.That(stackSize, Is.EqualTo(0));
        }

        [Test]
        public void AssignRegisters_EmptyIntervalButParameter()
        {
            var local = new LocalVariable(PrimitiveType.Real, "");
            local.IsParameter = true;
            var locals = new List<LocalVariable>() { local };

            RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(locals, out var stackSize);

            // Assigned to stack because it is a parameter
            Assert.That(local.StorageLocation, Is.EqualTo(0));
            Assert.That(stackSize, Is.EqualTo(1));
        }

        [Test]
        public void AssignRegisters_TwoOverlappingVariables()
        {
            var local1 = new LocalVariable(PrimitiveType.Int, "")
            {
                IntervalStart = 1,
                IntervalEnd = 3
            };
            var local2 = new LocalVariable(PrimitiveType.Bool, "")
            {
                IntervalStart = 2,
                IntervalEnd = 4
            };
            var locals = new List<LocalVariable>()
            {
                local1,
                local2
            };

            RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(locals, out var stackSize);

            // First local allocated to register, second one to stack
            Assert.That(local1.StorageLocation, Is.EqualTo(-1));
            Assert.That(local2.StorageLocation, Is.EqualTo(0));
            Assert.That(stackSize, Is.EqualTo(1));
        }

        [Test]
        public void AssignRegisters_TwoOverlappingVariables2()
        {
            var local1 = new LocalVariable(PrimitiveType.Int, "")
            {
                IntervalStart = 1,
                IntervalEnd = 5
            };
            var local2 = new LocalVariable(PrimitiveType.Bool, "")
            {
                IntervalStart = 2,
                IntervalEnd = 4
            };
            var locals = new List<LocalVariable>()
            {
                local1,
                local2
            };

            RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(locals, out var stackSize);

            // First local spilled to stack because it is alive longer
            Assert.That(local1.StorageLocation, Is.EqualTo(0));
            Assert.That(local2.StorageLocation, Is.EqualTo(-1));
            Assert.That(stackSize, Is.EqualTo(1));
        }

        [Test]
        public void AssignRegisters_TwoNonoverlappingVariables()
        {
            var local1 = new LocalVariable(PrimitiveType.Int, "")
            {
                IntervalStart = 1,
                IntervalEnd = 3
            };
            var local2 = new LocalVariable(PrimitiveType.Bool, "")
            {
                IntervalStart = 3,
                IntervalEnd = 4
            };
            var locals = new List<LocalVariable>()
            {
                local1,
                local2
            };

            RegisterAllocator<TrivialRegisterBackend>.AssignRegisters(locals, out var stackSize);

            // Both locals allocated on the single register
            Assert.That(local1.StorageLocation, Is.EqualTo(-1));
            Assert.That(local2.StorageLocation, Is.EqualTo(-1));
            Assert.That(stackSize, Is.EqualTo(0));
        }

        /// <summary>
        /// A system with a single register.
        /// </summary>
        private class TrivialRegisterBackend : RegisterBackend
        {
            bool _registerInUse;
            int _stackPosition;

            public override int GetLocation(PrimitiveType type, bool isParameter, out bool onStack)
            {
                if (isParameter || _registerInUse)
                {
                    _stackPosition++;
                    onStack = true;
                    return _stackPosition - 1;
                }
                else
                {
                    _registerInUse = true;
                    onStack = false;
                    return -1;
                }
            }

            public override void ReturnLocation(int location)
            {
                if (location == -1)
                    _registerInUse = false;
            }
        }
    }
}
