namespace Polsys.Peisik.Parser
{
    internal class AssignmentSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; private set; }
        
        public string Target { get; private set; }

        public AssignmentSyntax(TokenPosition position, ExpressionSyntax expression, string targetName)
            : base(position)
        {
            Expression = expression;
            Target = targetName;
        }
    }
}
