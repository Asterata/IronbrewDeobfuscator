using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator;

/// <summary>
/// Information about the current deobfuscation session is stored here.
/// </summary>
public class Session(SyntaxNode script, bool maxCflowDeobfuscafion)
{
    // Debug Information
    public bool IsDebug { get; set; }

    public string CurrentState { get; set; } = "start";
    
    // Nodes
    public LocalFunctionDeclarationStatementSyntax? WrapperFunction { get; set; }
    public SyntaxNode? InterpreterNode { get; set; }
    public SyntaxNode Script { get; } = script;
    public SyntaxNode? RewrittenScript { get; set; }
    
    // Checks
    public bool IsInterpreterRewritten { get; set; }
    
    // Informations
    public Dictionary<string, int> ChunkInformation { get; } = [];
    public Dictionary<string, string> WrapperInformation { get; } = [];
    public Dictionary<string, string> InterpreterInformation { get; } = [];
    public Dictionary<string, int> InstructionInformation { get; } = [];
    public Dictionary<int, Operation> BinaryTreeInformation { get; set; } = [];
    
    
    // Variables
    public string WrapperFunctionName { get; set; } = "NONE";
    public string StackName { get; set; } = "NONE";
    public string VarargName { get; set; } = "NONE";
    
    
    // Settings
    public bool EnableMaxCflowDeobfuscation = maxCflowDeobfuscafion;

}