namespace Polsys.Peisik.Parser
{
    /// <summary>
    /// Base class for statements.
    /// </summary>
    internal abstract class StatementSyntax : SyntaxNode
    {
        public StatementSyntax(TokenPosition position)
            : base(position)
        {
        }
    }
}
