using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;

public class Operation
{
    public SubOpcode SubOpcode = SubOpcode.None;
    public Opcode Opcode = Opcode.Unmatched;
    public ConstantMask ConstantMask = ConstantMask.None;
    public OperationType OperationType = OperationType.ABC;
    public StatementListSyntax? OperationBody;
    public int Enum;
    
    public bool IsSuperOpcode = false;
    public SuperOpcode SuperOpcode = new();
    
    public string DebugInformation = "";

    public override string ToString()
    {
        return $"{Opcode} {SubOpcode} {ConstantMask}";
    }

    
    
}