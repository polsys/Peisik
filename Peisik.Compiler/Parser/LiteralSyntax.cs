namespace Polsys.Peisik.Parser
{
    internal class LiteralSyntax : ExpressionSyntax
    {
        public PrimitiveType Type { get; private set; }

        public object Value { get; private set; }

        private LiteralSyntax(TokenPosition position, PrimitiveType type, object value)
            : base(position)
        {
            Type = type;
            Value = value;
        }

        public static LiteralSyntax CreateBoolLiteral(TokenPosition position, bool value)
        {
            return new LiteralSyntax(position, PrimitiveType.Bool, value);
        }

        public static LiteralSyntax CreateIntLiteral(TokenPosition position, long value)
        {
            return new LiteralSyntax(position, PrimitiveType.Int, value);
        }

        public static LiteralSyntax CreateRealLiteral(TokenPosition position, double value)
        {
            return new LiteralSyntax(position, PrimitiveType.Real, value);
        }
    }
}
