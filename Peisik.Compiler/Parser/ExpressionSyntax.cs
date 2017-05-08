namespace Polsys.Peisik.Parser
{
    /// <summary>
    /// Base class for expressions.
    /// </summary>
    internal abstract class ExpressionSyntax : SyntaxNode
    {
        public ExpressionSyntax(TokenPosition position)
            : base(position)
        {
        }
    }
}
