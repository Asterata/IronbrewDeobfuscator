using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.Walkers;

public class WrapperWalker(Session session) : LuaSyntaxWalker
{
    public override void VisitLocalFunctionDeclarationStatement(LocalFunctionDeclarationStatementSyntax node)
    {
        session.CurrentState = "Started WrapperWalker";
        if (node.Parameters.Parameters.Count != 3)
        {
            base.VisitLocalFunctionDeclarationStatement(node);
            return;
        }
        if (node.Body.Statements.Count != 4)
        {
            base.VisitLocalFunctionDeclarationStatement(node);
            return;
        }
        
        // Get the first 3 local variables, and get the numbers inside the Chunk table.
        /*
            local Instr = Chunk[1]
            local Proto = Chunk[2]
            local Params = Chunk[3]
         */

        if (node.Body.Statements[0] is not LocalVariableDeclarationStatementSyntax firstLocal ||
            node.Body.Statements[1] is not LocalVariableDeclarationStatementSyntax secondLocal ||
            node.Body.Statements[2] is not LocalVariableDeclarationStatementSyntax thirdLocal)
        {
            base.VisitLocalFunctionDeclarationStatement(node);
            return;
        }
        
        var instrTable = (ElementAccessExpressionSyntax)firstLocal.EqualsValues!.Values[0];
        var protoTable = (ElementAccessExpressionSyntax)secondLocal.EqualsValues!.Values[0];
        var paramsTable = (ElementAccessExpressionSyntax)thirdLocal.EqualsValues!.Values[0];
        
        var protoName = secondLocal.Names[0].Name;
        
        if (instrTable.KeyExpression is not LiteralExpressionSyntax instrIdentifier ||
            protoTable.KeyExpression is not LiteralExpressionSyntax protoIdentifier ||
            paramsTable.KeyExpression is not LiteralExpressionSyntax paramsIdentifier)
        {
            base.VisitLocalFunctionDeclarationStatement(node);
            return;
        }
        
        // Let's get the Parameters from the wrapper function!
        // local function Wrap(Chunk, Upvalues, Env)
        var chunkParam = node.Parameters.Parameters[0]; // Chunk
        var upvaluesParam = node.Parameters.Parameters[1]; // Upvalues
        var envParam = node.Parameters.Parameters[2]; // Env
        
        if (session.IsDebug)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Found wrapper function: " + node.Name.Name);
            Console.WriteLine("\tFound Instr identifier: " + instrIdentifier);
            Console.WriteLine("\tFound Proto identifier: " + protoIdentifier);
            Console.WriteLine("\tFound Params identifier: " + paramsIdentifier);
            Console.WriteLine("\tFound Chunk variable: " + chunkParam);
            Console.WriteLine("\tFound Proto variable: " + protoName);
            Console.WriteLine("\tFound Upvalues variable: " + upvaluesParam);
            Console.WriteLine("\tFound Env variable: " + envParam);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[STEP] Finished Walking the Wrapper Function!");
            Console.ResetColor();
            
            Console.WriteLine();
        }
        
        // Store the wrapper function and the chunk information.
        session.WrapperFunctionName = node.Name.Name;
        session.WrapperFunction = node;
        session.ChunkInformation.Add("Instr", int.Parse(instrIdentifier.Token.Text));
        session.ChunkInformation.Add("Proto", int.Parse(protoIdentifier.Token.Text));
        session.ChunkInformation.Add("Params", int.Parse(paramsIdentifier.Token.Text));
        session.WrapperInformation.Add("Chunk", chunkParam.ToString());
        session.WrapperInformation.Add("Proto", protoName);
        session.WrapperInformation.Add("Upvalues", upvaluesParam.ToString());
        session.WrapperInformation.Add("Env", envParam.ToString());
        
        session.CurrentState = "Finished WrapperWalker";
        
        base.VisitLocalFunctionDeclarationStatement(node);
    }
}