using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.Walkers;

public class InformationWalker(Session session) : LuaSyntaxWalker
{
    public override void VisitReturnStatement(ReturnStatementSyntax node)
    {
        session.CurrentState = "Started InformationWalker [ReturnStatement]";
        
        // return function(...)
        if (node.Expressions is not [AnonymousFunctionExpressionSyntax {
                Parameters.Parameters: [VarArgParameterSyntax] }] )
        {
            base.VisitReturnStatement(node);
            return;
        }

        var anonFunction = (AnonymousFunctionExpressionSyntax)node.Expressions[0];
        var anonFunctionBody = anonFunction.Body;
        var bodyNodes = anonFunctionBody.DescendantNodes();

        // Lets get some information!
        // I would make everything static but,
        // i want this project to be more valuable information for new people.
        
        /*
        for Idx = 0, PCount do
            if (Idx >= Params) then
                Vararg[Idx - Params] = Args[Idx + 1]
            else
                Stk[Idx] = Args[Idx + 1]
            end
        end
         */
        // Get the first for loop from the nodes,
        // so we can get the Stack table in order to match the Opcodes.
        var forLoop = bodyNodes.OfType<NumericForStatementSyntax>().FirstOrDefault();
        var ifStatement = (IfStatementSyntax)forLoop!.Body.Statements[0];
        var stackStatement = (AssignmentStatementSyntax)ifStatement.ElseClause!.ElseBody.Statements[0];
        // Stk[Idx] = Args[Idx + 1]
        var stackTable = (ElementAccessExpressionSyntax)stackStatement.Variables[0];
        var stackString = stackTable.Expression.ToString();
        
        if (session.IsDebug)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Found Stack variable: " + stackString);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.ResetColor();
        }
        session.WrapperInformation.Add("Stack", stackString);
        session.StackName = stackString;
        
        session.CurrentState = "Finished InformationWalker [ReturnStatement]";
        base.VisitReturnStatement(node);
    }

    public override void VisitLocalFunctionDeclarationStatement(LocalFunctionDeclarationStatementSyntax node)
    {
        session.CurrentState = "Started InformationWalker [LocalFunctionDeclarationStatement]";
        if (node.Parameters.Parameters is not [VarArgParameterSyntax])
        {
            base.VisitLocalFunctionDeclarationStatement(node);
            return;
        }
        
        if (session.IsDebug)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Found Select variable: " + node.Name.Name);
            Console.ResetColor();
        }
        
        session.WrapperInformation.Add("Select", node.Name.Name);
        
        session.CurrentState = "Finished InformationWalker [LocalFunctionDeclarationStatement]";
        
        base.VisitLocalFunctionDeclarationStatement(node);
    }
}