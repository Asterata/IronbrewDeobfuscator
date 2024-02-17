using System.Reflection.Metadata.Ecma335;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;

public class SuperOpcodeRewriter : LuaSyntaxRewriter
{
    private readonly List<string> _localNames = [];

    // TODO FIX OR ADD NEW OPCODES TO FIX THE REWRITER
    public override SyntaxNode? VisitLocalVariableDeclarationStatement(LocalVariableDeclarationStatementSyntax node)
    {
        
        if (node.EqualsValues is not null)
            return base.VisitLocalVariableDeclarationStatement(node);
        
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
        if (node.Variables is [IdentifierNameSyntax identifierName] && _localNames.Contains(identifierName.Name) && node.EqualsValues.Values[0] is not BinaryExpressionSyntax)
            return SyntaxFactory.LocalVariableDeclarationStatement(SyntaxFactory.SeparatedList<LocalDeclarationNameSyntax>().Add(SyntaxFactory.LocalDeclarationName(identifierName)), node.EqualsValues);
        
        return base.VisitAssignmentStatement(node);
    }
}