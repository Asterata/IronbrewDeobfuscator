using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;

public class InterpreterRewriter(Session session, string dumperScript) : LuaSyntaxRewriter
{
    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        if (node != session.InterpreterNode)
            return base.VisitWhileStatement(node);
        
        session.IsInterpreterRewritten = true;
        return SyntaxFactory.ParseStatement(dumperScript);
    }
}