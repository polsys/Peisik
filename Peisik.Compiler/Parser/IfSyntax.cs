namespace Polsys.Peisik.Parser
{
    internal class IfSyntax : StatementSyntax
    {
        public ExpressionSyntax Condition { get; private set; }
        public BlockSyntax ElseBlock { get; private set; }
        public BlockSyntax ThenBlock { get; private set; }

        public IfSyntax(TokenPosition position, ExpressionSyntax condition, BlockSyntax thenBlock, BlockSyntax elseBlock)
            : base(position)
        {
            Condition = condition;
            ElseBlock = elseBlock;
            ThenBlock = thenBlock;
        }
    }
}
