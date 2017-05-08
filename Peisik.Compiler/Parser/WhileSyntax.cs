namespace Polsys.Peisik.Parser
{
    internal class WhileSyntax : StatementSyntax
    {
        public BlockSyntax CodeBlock { get; private set; }

        public ExpressionSyntax Condition { get; private set; }

        public WhileSyntax(TokenPosition position, ExpressionSyntax condition, BlockSyntax block)
            : base(position)
        {
            CodeBlock = block;
            Condition = condition;
        }
    }
}
