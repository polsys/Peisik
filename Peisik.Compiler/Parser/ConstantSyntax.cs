using System;

namespace Polsys.Peisik.Parser
{
    internal class ConstantSyntax : SyntaxNode
    {
        public string Name { get; private set; }
        public PrimitiveType Type { get; private set; }
        public Visibility Visibility { get; private set; }

        public object Value { get { return _value; } }

        private object _value;

        public bool GetBoolValue()
        {
            if (Type != PrimitiveType.Bool)
                throw new InvalidOperationException("This is not an boolean constant.");

            return (bool)_value;
        }

        public long GetIntValue()
        {
            if (Type != PrimitiveType.Int)
                throw new InvalidOperationException("This is not an integer constant.");

            return (long)_value;
        }

        public double GetRealValue()
        {
            if (Type != PrimitiveType.Real)
                throw new InvalidOperationException("This is not an real number constant.");

            return (double)_value;
        }

        #region Constructors and static methods

        private ConstantSyntax(TokenPosition position, PrimitiveType type, Visibility visibility, string name, object value)
            : base(position)
        {
            Name = name;
            Type = type;
            Visibility = visibility;
            _value = value;
        }

        public static ConstantSyntax CreateBoolConstant(string name, bool value, Visibility visibility, TokenPosition position)
        {
            return new ConstantSyntax(position, PrimitiveType.Bool, visibility, name, value);
        }

        public static ConstantSyntax CreateIntConstant(string name, long value, Visibility visibility, TokenPosition position)
        {
            return new ConstantSyntax(position, PrimitiveType.Int, visibility, name, value);
        }

        public static ConstantSyntax CreateRealConstant(string name, double value, Visibility visibility, TokenPosition position)
        {
            return new ConstantSyntax(position, PrimitiveType.Real, visibility, name, value);
        }

        #endregion
    }
}
