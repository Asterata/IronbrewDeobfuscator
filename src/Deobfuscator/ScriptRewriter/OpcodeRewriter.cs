using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;

public class OpcodeRewriter(Session session) : LuaSyntaxRewriter
{
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (session.InterpreterInformation.ContainsValue(node.Name))
        {
            foreach (var originalName in session.InterpreterInformation.Keys)
            {
                if (session.InterpreterInformation[originalName] == node.Name)
                {
                    // Return the original identifier name
                    return SyntaxFactory.IdentifierName(originalName);
                }
            }
        }
        
        if (session.WrapperInformation.ContainsValue(node.Name))
        {
            foreach (var originalName in session.WrapperInformation.Keys)
            {
                if (session.WrapperInformation[originalName] == node.Name)
                {
                    // Return the original identifier name
                    return SyntaxFactory.IdentifierName(originalName);
                }
            }
        }
        
        
        
        return base.VisitIdentifierName(node);
    }
}