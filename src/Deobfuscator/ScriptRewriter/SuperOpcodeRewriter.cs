using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;

public class SuperOpcodeRewriter : LuaSyntaxRewriter
{
    private readonly List<string> _localNames = [];


    public override SyntaxNode? VisitLocalVariableDeclarationStatement(LocalVariableDeclarationStatementSyntax node)
    {
        if (!_localNames.Contains(node.Names[0].Name))
        {
            _localNames.Add(node.Names[0].Name);
            
        }
        
        if (_localNames.Contains(node.Names[0].Name))
            return null;
        
        return base.VisitLocalVariableDeclarationStatement(node);
    }

    public override SyntaxNode? VisitAssignmentStatement(AssignmentStatementSyntax node)
    {
        if (node.Variables is [IdentifierNameSyntax identifierName] && _localNames.Contains(identifierName.Name))
            return SyntaxFactory.LocalVariableDeclarationStatement(SyntaxFactory.SeparatedList<LocalDeclarationNameSyntax>().Add(SyntaxFactory.LocalDeclarationName(identifierName)), node.EqualsValues);
        
        return base.VisitAssignmentStatement(node);
    }
}