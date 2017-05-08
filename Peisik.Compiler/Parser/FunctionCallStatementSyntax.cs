namespace Polsys.Peisik.Parser
{
    internal class FunctionCallStatementSyntax : StatementSyntax
    {
        public FunctionCallSyntax Expression { get; private set; }
        
        public FunctionCallStatementSyntax(TokenPosition position, FunctionCallSyntax callExpression)
            : base(position)
        {
            Expression = callExpression;
        }
    }
}
