namespace Polsys.Peisik.Parser
{
    internal class ReturnSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; private set; }
        
        public ReturnSyntax(TokenPosition position, ExpressionSyntax expression)
            : base(position)
        {
            Expression = expression;
        }
    }
}
