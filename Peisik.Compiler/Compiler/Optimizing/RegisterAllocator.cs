using System;
using System.Collections.Generic;
using System.Linq;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Implements a simple linear scan register allocator.
    /// </summary>
    internal class RegisterAllocator<Backend>
        where Backend : RegisterBackend, new()
    {
        /// <summary>
        /// Allocates registers for the function.
        /// There should be no previous allocation information, or the allocation may fail.
        /// </summary>
        /// <returns>
        /// The number of stack variables.
        /// </returns>
        public static int Allocate(Function function)
        {
            ComputeIntervals(function);
            AssignRegisters(function.Locals, out var stackSize);
            return stackSize;
        }

        internal static void AssignRegisters(List<LocalVariable> locals, out int stackSize)
        {
            var backend = new Backend();
            var maxLocation = -1;

            // This list contains active intervals sorted by decreasing interval end.
            // Decreasing so that we remove items from the end rather than start.
            var active = new List<LocalVariable>();

            // Go through the live intervals in order of increasing start position
            foreach (var interval in locals.OrderBy(local => local.IntervalStart))
            {
                // Expire old intervals and return their registers
                for (var i = active.Count - 1; i >= 0; i--)
                {
                    if (active[i].IntervalEnd <= interval.IntervalStart)
                    {
                        backend.ReturnLocation(active[i].StorageLocation);
                        active.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }
                }

                // If this interval is empty and non-parameter, just don't allocate it
                if (interval.IntervalStart == interval.IntervalEnd
                    && !interval.IsParameter)
                {
                    continue;
                }

                // Get a register for this interval
                var location = backend.GetLocation(interval.Type, interval.IsParameter, out var onStack);
                maxLocation = Math.Max(maxLocation, location);
                interval.StorageLocation = location;

                if (onStack && !interval.IsParameter)
                {
                    // If we got a stack position, decide whether to spill this interval
                    // or the last enregistered one in the active list.
                    // The original algorithm would only have enregistered intervals in the
                    // active list, but we want to reuse stack slots too.
                    //
                    // It should be impossible for this loop not to find an enregistered
                    // variable, because in that case we would have gotten a register from GetLocation.
                    // That would be a bug in the register backend.
                    for (var i = active.Count - 1; i >= 0; i--)
                    {
                        if (!active[i].OnStack)
                        {
                            if (active[i].IntervalEnd > interval.IntervalEnd)
                            {
                                // Spill the longer interval and give this its location
                                interval.StorageLocation = active[i].StorageLocation;
                                active[i].StorageLocation = location;
                                break;
                            }
                            else
                            {
                                // This should be left on stack
                                break;
                            }
                        }
                    }
                }

                // Add this interval to the active list and sort the list
                active.Add(interval);
                active.Sort((LocalVariable left, LocalVariable right) =>
                    right.IntervalEnd.CompareTo(left.IntervalEnd));
            }

            stackSize = Math.Max(maxLocation + 1, 0);
        }

        /// <summary>
        /// Computes live intervals for local variables of the function.
        /// </summary>
        internal static void ComputeIntervals(Function function)
        {
            _ = VisitTreeNode(function.ExpressionTree, 0);
        }

        /// <summary>
        /// Computes liveness by visiting the expression tree recursively in depth-first order.
        /// </summary>
        private static int VisitTreeNode(Expression node, int currentPosition)
        {
            switch (node)
            {
                case null:
                    return currentPosition;
                case BinaryExpression binary:
                    currentPosition = VisitTreeNode(binary.Left, currentPosition);
                    currentPosition = VisitTreeNode(binary.Right, currentPosition);
                    // The x64 backend requires the child nodes to stay alive until the expression is evaluated
                    SetLiveness(binary.Left.Store, currentPosition);
                    SetLiveness(binary.Right.Store, currentPosition);
                    SetLiveness(binary.Store, currentPosition);
                    return currentPosition + 1;
                case ConstantExpression constant:
                    SetLiveness(constant.Store, currentPosition);
                    return currentPosition + 1;
                case FailFastExpression fail:
                    // FailFast does not use or store anything
                    return currentPosition;
                case FunctionCallExpression call:
                    foreach (var expr in call.Parameters)
                    {
                        currentPosition = VisitTreeNode(expr, currentPosition);
                    }
                    SetLiveness(call.Store, currentPosition);
                    return currentPosition + 1;
                case IfExpression condition:
                    currentPosition = VisitTreeNode(condition.Condition, currentPosition);
                    currentPosition = VisitTreeNode(condition.ThenExpression, currentPosition);
                    return VisitTreeNode(condition.ElseExpression, currentPosition);
                case LocalLoadExpression load:
                    SetLiveness(load.Local, currentPosition);
                    SetLiveness(load.Store, currentPosition);
                    return currentPosition + 1;
                case PrintExpression print:
                    foreach (var expr in print.Expressions)
                    {
                        currentPosition = VisitTreeNode(expr, currentPosition);
                    }
                    return currentPosition;
                case RealConversionExpression real:
                    currentPosition = VisitTreeNode(real.Expression, currentPosition);
                    SetLiveness(real.Expression.Store, currentPosition);
                    SetLiveness(real.Store, currentPosition);
                    return currentPosition + 1;
                case ReturnExpression ret:
                    // If the child expression is complex, the x64 backend needs a register for it
                    // In that case we must add a use
                    currentPosition = VisitTreeNode(ret.Value, currentPosition);
                    SetLiveness(ret.Value.Store, currentPosition);
                    return currentPosition + 1;
                case SequenceExpression sequence:
                    foreach (var expr in sequence.Expressions)
                    {
                        currentPosition = VisitTreeNode(expr, currentPosition);
                    }
                    return currentPosition;
                case UnaryExpression unary:
                    currentPosition = VisitTreeNode(unary.Expression, currentPosition);
                    SetLiveness(unary.Store, currentPosition);
                    return currentPosition + 1;
                case WhileExpression loop:
                    currentPosition = VisitTreeNode(loop.Condition, currentPosition);
                    return VisitTreeNode(loop.Body, currentPosition);
                default:
                    throw new NotImplementedException($"Unknown node type {node}");
            }
        }

        private static void SetLiveness(LocalVariable local, int currentPosition)
        {
            if (local == null)
                return;

            // If the start position has not been set, do so
            // However, parameters start at -1
            if (local.IntervalStart == -1 && !local.IsParameter)
            {
                local.IntervalStart = currentPosition;
            }

            // Update the end position
            local.IntervalEnd = currentPosition;
        }
    }
}
