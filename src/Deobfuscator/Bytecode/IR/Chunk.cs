using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;

namespace IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;

public class Chunk
{
    public string _functionId = "f_"+Guid.NewGuid().ToString()[..8];
    public List<Instruction> Instructions = [];
    public List<Constant> Constants = [];
    public List<Chunk> Functions = [];
    public Dictionary<Instruction, int> InstructionMap = new();
    public Dictionary<Constant, int> ConstantMap = new();
    public Dictionary<Chunk, int> FunctionMap = new();
    
    public byte UpvalueCount;
    public byte ParameterCount;
    public byte VarargFlag;
    public byte StackSize;
		
    public void UpdateMappings()
    {
        InstructionMap.Clear();
        ConstantMap.Clear();
        FunctionMap.Clear();

        for (var i = 0; i < Instructions.Count; i++)
        {
            InstructionMap.Add(Instructions[i], i);
            Instructions[i].PC = i;
        }

        for (var i = 0; i < Constants.Count; i++)
            ConstantMap.Add(Constants[i], i);

        for (var i = 0; i < Functions.Count; i++)
            FunctionMap.Add(Functions[i], i);
    }

    public override string ToString()
    {
        var str = "function [" + _functionId + "]";
        var parameters = "(";
        for (var i = 0; i < ParameterCount; i++)
        {
            parameters += $"p{i}, ";
        }
        parameters += VarargFlag == 1 ? "..." : "";
        str += parameters + ")\n";
        
        str += "\n\tConstants:\n";
        for (var index = 0; index < Constants.Count; index++)
        {
            var constant = Constants[index];
            

            var start = $"[{index}] ";
            var constStart = (start+constant.Type).PadRight(15, ' ');
            var constValue = $"Value: {constant.Data.ToString()}".PadRight(8, ' ');
            var constString = $"\t{constStart} {constValue}";
            str += constString + "\n";
        }
        
        str += "\n\tInstructions:\n";
        for (var index = 0; index < Instructions.Count; index++)
        {
            var instruction = Instructions[index];
            

            var start = $"[{index}] Opcode: ";
            var instrStart = (start+instruction.Opcode).PadRight(30, ' ');
            var instrA = $"A: {instruction.A}".PadRight(8, ' ');
            var instrB = $"B: {instruction.B}".PadRight(8, ' ');
            var instrC = $"C: {instruction.C}".PadRight(8, ' ');
            var instrString = $"\t{instrStart} {instrA} {instrB} {instrC}";

            switch (instruction.Opcode)
            {
                case Opcode.Closure:
                    instrString += $"        | loads: function [{Functions[instruction.B]._functionId}]";
                    break;
                case Opcode.Jmp or Opcode.ForPrep or Opcode.ForLoop:
                    instrString += $"        | jumps to: {InstructionMap[instruction.JumpReference!]}";
                    break;
                case Opcode.Eq or Opcode.Lt or Opcode.Le:
                    instrString += $"        | False branch start: -> {InstructionMap[instruction.JumpReference!]}";
                    break;
            }
            if (instruction.SubOpcode is SubOpcode.None)
                str += instrString + "\n";
            else
                str += instrString + $"        | SubOpcode: {instruction.SubOpcode}" + "\n";
        }
        

        return str + "\nend\n";
    }
}