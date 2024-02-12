using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.Walkers;

/// <summary>
/// Used to walk through the Interpreter.
/// </summary>
/// <param name="session"> Current session </param>
public class InterpreterWalker(Session session) : LuaSyntaxWalker
{
    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        
        if (node.Body.Statements.Count != 4)
        {
            base.VisitWhileStatement(node);
            return;
        }
        
        // Get the first 2 variables,
        // to get more information about our instruction
        /*
            Inst = Instr[InstrPoint]
            Enum = Inst[1]
         */
        if (node.Body.Statements[0] is not AssignmentStatementSyntax firstStatement ||
            node.Body.Statements[1] is not AssignmentStatementSyntax secondStatement)
        {
            base.VisitWhileStatement(node);
            return;
        }
        
        
        var instrTable = (ElementAccessExpressionSyntax)firstStatement.EqualsValues.Values[0];
        var instTable = (ElementAccessExpressionSyntax)secondStatement.EqualsValues.Values[0];
        

        var instName = instTable.Expression.ToString();
        var enumName = ((IdentifierNameSyntax)secondStatement.Variables[0]).Name;
        var instrName = instrTable.Expression.ToString();
        var instrPointerName = instrTable.KeyExpression.ToString();
        var enumPointerName = instTable.KeyExpression.ToString();
        
        
        if (session.IsDebug)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Found Inst variable: " + instName);
            Console.WriteLine("Found Instr variable: " + instrName);
            Console.WriteLine("Found Enum variable: " + enumName);
            Console.WriteLine("Found InstrPoint variable: " + instrPointerName);
            Console.WriteLine("Found Enum identifier: " + enumPointerName);
            Console.ResetColor();
            
            Console.WriteLine();
        }
        
        session.InterpreterInformation.Add("Inst", instName);
        session.InterpreterInformation.Add("Instr", instrName);
        session.InterpreterInformation.Add("Enum", enumName);
        session.InterpreterInformation.Add("InstrPoint", instrPointerName);
        session.InstructionInformation.Add("Enum", int.Parse(enumPointerName));
        
        session.InterpreterNode = node;
        
        new BinaryTreeWalker(session).Visit(node.Body.Statements[2]);
        
        base.VisitWhileStatement(node);
    }

    /// <summary>
    /// Used to walk through the binary tree.
    /// </summary>
    /// <param name="session"> Current session </param>
    public class BinaryTreeWalker(Session session) : LuaSyntaxWalker
    {
        /// <summary>
        /// Check if the statement list is a binary tree branch.
        /// </summary>
        /// <param name="statementList"></param>
        /// <returns></returns>
        private bool IsBinaryTreeBranch(StatementListSyntax statementList) =>
            statementList.Statements.Count != 0 &&
            statementList.Statements[0] is IfStatementSyntax
              {
                  Condition: BinaryExpressionSyntax
                  {
                      Left: IdentifierNameSyntax ident,
                      Right: LiteralExpressionSyntax
                  }
              } &&
              ident.Name == session.InterpreterInformation["Enum"];


        // Walks through the if statements to get the Opcode Enum and the bodies.
        public override void VisitIfStatement(IfStatementSyntax node)
        {
            // if Enum <= 9 then
            if (node.Condition is BinaryExpressionSyntax
                { Left: IdentifierNameSyntax syntax,
                    Right: LiteralExpressionSyntax { Token.Value: double opcodeValue }
                } binary && syntax.Name == session.InterpreterInformation["Enum"])
            {
                // IF
                var opcodeEnum = (int)opcodeValue;

                switch (binary.OperatorToken.Kind())
                {
                    case SyntaxKind.EqualsEqualsToken: // ==
                    case SyntaxKind.LessThanEqualsToken: // <=
                    {
                        if (!IsBinaryTreeBranch(node.Body)) 
                        {
                            session.BinaryTreeInformation.Add(opcodeEnum, 
                                new Operation
                                {
                                    Opcode = Opcode.Unmatched,
                                    OperationBody = node.Body
                                });
                        }

                        // If theres no elseif clauses, we can add the else clause to the binary tree.
                        if (node.ElseIfClauses.Count == 0)
                        {
                            if (node.ElseClause is {} elseClause && !IsBinaryTreeBranch(elseClause.ElseBody)) 
                            {
                                session.BinaryTreeInformation.Add(opcodeEnum + 1, 
                                    new Operation
                                    {
                                        Opcode = Opcode.Unmatched,
                                        OperationBody = elseClause.ElseBody
                                    });
                            }
                        }
                        // If theres elseif clauses,
                        // we need to check the biggest enum and add the else clause to the binary tree.
                        else
                        {
                            var biggestEnum = opcodeEnum;
                            SyntaxToken elseIfToken = default;
                            foreach (var elseIfClause in node.ElseIfClauses)
                            {
                                if (elseIfClause.Condition is not BinaryExpressionSyntax
                                    {
                                        Left: IdentifierNameSyntax,
                                        Right: LiteralExpressionSyntax { Token.Value: double opcodeValue2 }
                                    } binary2 || syntax.Name != session.InterpreterInformation["Enum"]) continue;
                                if (opcodeValue2 > biggestEnum)
                                {
                                    biggestEnum = (int)opcodeValue2;
                                    elseIfToken = binary2.OperatorToken;
                                }
                            }
                            if (node.ElseClause is {} elseClause && !IsBinaryTreeBranch(elseClause.ElseBody)) 
                            {
                                switch (elseIfToken.Kind())
                                {
                                    case SyntaxKind.EqualsEqualsToken: // ==
                                    case SyntaxKind.LessThanEqualsToken: // <=
                                    {
                                        if (!IsBinaryTreeBranch(elseClause.ElseBody)) 
                                        {
                                            session.BinaryTreeInformation.Add(biggestEnum+1, 
                                                new Operation
                                                {
                                                    Opcode = Opcode.Unmatched,
                                                    OperationBody = elseClause.ElseBody
                                                });
                                        }
                        
                                        break;
                                    }
                                    case SyntaxKind.GreaterThanToken: // >
                                    {
                                        if (!IsBinaryTreeBranch(elseClause.ElseBody)) 
                                        {
                                            session.BinaryTreeInformation.Add(biggestEnum, 
                                                new Operation
                                                {
                                                    Opcode = Opcode.Unmatched,
                                                    OperationBody = elseClause.ElseBody
                                                });
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    
                    case SyntaxKind.GreaterThanToken: // >
                    {
                        if (!IsBinaryTreeBranch(node.Body)) 
                        {
                            session.BinaryTreeInformation.Add(opcodeEnum+1, 
                                new Operation
                                {
                                    Opcode = Opcode.Unmatched,
                                    OperationBody = node.Body
                                });
                        }
                        
                        // If theres no elseif clauses, we can add the else clause to the binary tree.
                        if (node.ElseIfClauses.Count == 0)
                        {
                            if (node.ElseClause is {} elseClause && !IsBinaryTreeBranch(elseClause.ElseBody)) 
                            {
                                session.BinaryTreeInformation.Add(opcodeEnum, 
                                    new Operation
                                    {
                                        Opcode = Opcode.Unmatched,
                                        OperationBody = elseClause.ElseBody
                                    });
                            }
                        }
                        // If theres elseif clauses,
                        // we need to check the biggest enum and add the else clause to the binary tree.
                        else
                        {
                            var biggestEnum = opcodeEnum;
                            SyntaxToken elseIfToken = default;
                            foreach (var elseIfClause in node.ElseIfClauses)
                            {
                                if (elseIfClause.Condition is not BinaryExpressionSyntax
                                    {
                                        Left: IdentifierNameSyntax,
                                        Right: LiteralExpressionSyntax { Token.Value: double opcodeValue2 }
                                    } binary2 || syntax.Name != session.InterpreterInformation["Enum"]) continue;
                                if (opcodeValue2 > biggestEnum)
                                {
                                    biggestEnum = (int)opcodeValue2;
                                    elseIfToken = binary2.OperatorToken;
                                }
                            }
                            if (node.ElseClause is {} elseClause && !IsBinaryTreeBranch(elseClause.ElseBody)) 
                            {
                                switch (elseIfToken.Kind())
                                {
                                    case SyntaxKind.EqualsEqualsToken: // ==
                                    case SyntaxKind.LessThanEqualsToken: // <=
                                    {
                                        if (!IsBinaryTreeBranch(elseClause.ElseBody)) 
                                        {
                                            session.BinaryTreeInformation.Add(biggestEnum+1, 
                                                new Operation
                                                {
                                                    Opcode = Opcode.Unmatched,
                                                    OperationBody = elseClause.ElseBody
                                                });
                                        }
                        
                                        break;
                                    }
                                    case SyntaxKind.GreaterThanToken: // >
                                    {
                                        if (!IsBinaryTreeBranch(elseClause.ElseBody)) 
                                        {
                                            session.BinaryTreeInformation.Add(biggestEnum, 
                                                new Operation
                                                {
                                                    Opcode = Opcode.Unmatched,
                                                    OperationBody = elseClause.ElseBody
                                                });
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            
            base.VisitIfStatement(node);
        }
        
        
        
        // Walks through the elseif clauses to get the Opcode Enum and the bodies.
        public override void VisitElseIfClause(ElseIfClauseSyntax node)
        {
            // elseif Enum <= 9 then
            if (node.Condition is BinaryExpressionSyntax
                { Left: IdentifierNameSyntax syntax,
                    Right: LiteralExpressionSyntax { Token.Value: double opcodeValue }
                } binary && syntax.Name == session.InterpreterInformation["Enum"])
            {
                // ELSEIF
                var opcodeEnum = (int)opcodeValue;

                switch (binary.OperatorToken.Kind())
                {
                    case SyntaxKind.EqualsEqualsToken: // ==
                    case SyntaxKind.LessThanEqualsToken: // <=
                    {
                        if (!IsBinaryTreeBranch(node.Body)) 
                        {
                            session.BinaryTreeInformation.Add(opcodeEnum, 
                                new Operation
                                {
                                    Opcode = Opcode.Unmatched,
                                    OperationBody = node.Body
                                });
                        }
                        
                        break;
                    }
                    case SyntaxKind.GreaterThanToken: // >
                    {
                        if (!IsBinaryTreeBranch(node.Body)) 
                        {
                            session.BinaryTreeInformation.Add(opcodeEnum+1, 
                                new Operation
                                {
                                    Opcode = Opcode.Unmatched,
                                    OperationBody = node.Body
                                });
                        }
                        break;
                    }
                }
            }
            
            
            
            base.VisitElseIfClause(node);
        }
    }
}