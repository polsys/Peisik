using System;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Represents a compile-time constant.
    /// </summary>
    internal class ConstantExpression : Expression
    {
        public object Value;

        public ConstantExpression(object value, OptimizingCompiler compiler, LocalVariable store = null, TokenPosition position = default)
        {
            // Store the type
            switch (value)
            {
                case bool b:
                    Type = PrimitiveType.Bool;
                    break;
                case long l:
                    Type = PrimitiveType.Int;
                    break;
                case double d:
                    Type = PrimitiveType.Real;
                    break;
                default:
                    throw new ArgumentException("Unknown constant type");
            }

            Value = value;
            SetStore(store, compiler, position);
        }

        public ConstantExpression(LiteralSyntax expression, OptimizingCompiler compiler, LocalVariable store = null)
            : this(expression.Value, compiler, store, expression.Position)
        {
        }

        public override Expression Fold(OptimizingCompiler compiler)
        {
            return this;
        }

        protected override void SetStore(LocalVariable newStore, OptimizingCompiler compiler, TokenPosition position = default)
        {
            base.SetStore(newStore, compiler, position);
        }
    }
}
