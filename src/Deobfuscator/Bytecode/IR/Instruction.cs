using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;

namespace IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;

public class Instruction(Chunk chunk, Opcode opcode, int a, int b, int c)
{ 
    public Chunk Chunk = chunk;
    public Opcode Opcode = opcode;
    public SubOpcode SubOpcode = SubOpcode.None;
    public InstructionType InstructionType = InstructionType.ABC;

    public int A = a;
    public int B = b;
    public int C = c;
    public int PC = -1;

    // References
    public Instruction? JumpReference;
    public List<Instruction> JumpReferenceParents = [];
    public Chunk? FunctionReference;
    public Dictionary<string, Constant> ConstantReferences = new();

    public bool isKA;
    public bool isKB;
    public bool isKC;

    public bool isDead = false;

    
    public void TurnIntoLua51()
    {
        switch (Opcode)
        {
            case Opcode.Eq or Opcode.Lt or Opcode.Le:
                B = A;
                break;
            
            case Opcode.TestSet:
                B = C;
                break;
            
            case Opcode.Jmp or Opcode.ForLoop or Opcode.ForPrep or Opcode.TForLoop or Opcode.Test or Opcode.TestSet:
                B -= PC + 1;
                break;
        }
        
        switch (SubOpcode)
        {
            case SubOpcode.A0:
                A = 0;
                break;
            case SubOpcode.A1:
                A = 1;
                break;
            case SubOpcode.B0:
                B = 0;
                break;
            case SubOpcode.B1:
                B = 1;
                break;
            case SubOpcode.B2:
                B = 2;
                break;
            case SubOpcode.B3:
                B = 3;
                break;
            case SubOpcode.C0:
                C = 0;
                break;
            case SubOpcode.C1:
                C = 1;
                break;
            case SubOpcode.C2:
                C = 2;
                break;
            case SubOpcode.B0C0:
                B = 0;
                C = 0;
                break;
            case SubOpcode.B0C1:
                B = 0;
                C = 1;
                break;
            case SubOpcode.B0C2:
                B = 0;
                C = 2;
                break;
            case SubOpcode.B1C0:
                B = 1;
                C = 0;
                break;
            case SubOpcode.B1C1:
                B = 1;
                C = 1;
                break;
            case SubOpcode.B1C2:
                B = 1;
                C = 2;
                break;
            case SubOpcode.B2C0:
                B = 2;
                C = 0;
                break;
            case SubOpcode.B2C1:
                B = 2;
                C = 1;
                break;
            case SubOpcode.B2C2:
                B = 2;
                C = 2;
                break;
            case SubOpcode.NoUpvalues:
                break;
            case SubOpcode.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (Opcode) // removing the mutations
        {
            case Opcode.Call:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B -= A - 1;
                        C -= A - 2;
                        break;
                    case SubOpcode.B2:
                        C -= A - 2;
                        break;
                    case SubOpcode.B0:
                        C -= A - 2;
                        break;
                    case SubOpcode.B1:
                        C -= A - 2;
                        break;
                    case SubOpcode.C0:
                        B -= A - 1;
                        break;
                    case SubOpcode.B2C0:
                        B -= A - 1;
                        break;
                    case SubOpcode.C1:
                        B -= A - 1;
                        break;
                    case SubOpcode.B2C1:
                        break;
                    case SubOpcode.B0C0:
                        break;
                    case SubOpcode.B0C1:
                        break;
                    case SubOpcode.B1C0:
                        break;
                    case SubOpcode.B1C1:
                        break;
                    case SubOpcode.C2:
                        B -= A - 1;
                        break;
                    case SubOpcode.B2C2:
                        break;
                    case SubOpcode.B0C2:
                        break;
                    case SubOpcode.B1C2:
                        B -= A - 1;
                        break;
                }
                break;
            }
            case Opcode.TailCall:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B = B - A + 1;
                        break;
                    case SubOpcode.B0:
                        break;
                    case SubOpcode.B1:
                        break;
                }
                break;
            }
            case Opcode.Return:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B += 2;
                        break;
                    case SubOpcode.B2:
                        break;
                    case SubOpcode.B3:
                        break;
                    case SubOpcode.B0:
                        break;
                    case SubOpcode.B1:
                        break;
                }
                break;
            }
            case Opcode.SetList:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B -= A;
                        break;
                    case SubOpcode.B0:
                        break;
                    case SubOpcode.C0:
                        break;
                }
                break;
            }
            case Opcode.Closure:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        C = 0;
                        break;
                    case SubOpcode.NoUpvalues:
                        C = 0;
                        break;
                }
                break;
            }
            case Opcode.Vararg:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B = B + A + 1;
                        break;
                    case SubOpcode.B0:
                        break;
                }
                break;
            }
            case Opcode.TForLoop:
            {
                switch (SubOpcode)
                {
                    case SubOpcode.None:
                        B = 0;
                        break;
                }
                break;
            }
        }
        
        if (isKA)
        {
            B = Chunk.ConstantMap[ConstantReferences["A"]] + 256;
        }
        if (isKB)
        {
            if (Opcode is not (Opcode.LoadK or Opcode.SetGlobal or Opcode.GetGlobal))
                B = Chunk.ConstantMap[ConstantReferences["B"]] + 256;
        }
        if (isKC)
        {
            if (Opcode is not (Opcode.LoadK or Opcode.SetGlobal or Opcode.GetGlobal))
                C = Chunk.ConstantMap[ConstantReferences["C"]] + 256;
        }
            
    }
    
    public void UpdateRegisters()
    {
        switch (Opcode)
        {
            case Opcode.Jmp or Opcode.ForLoop or Opcode.ForPrep:
                B = Chunk.InstructionMap[JumpReference!] - PC - 1;
                break;
            case Opcode.Closure:
                FunctionReference = Chunk.Functions[B];
                break;
        }
    }
    
    public void Ib2UpdateRegisters()
    {
        switch (Opcode)
        {
            case Opcode.Eq or Opcode.Lt or Opcode.Le or Opcode.Test or Opcode.TestSet:
                B = Chunk.InstructionMap[JumpReference!];
                break;
            case Opcode.Jmp or Opcode.ForLoop or Opcode.ForPrep:
                B = Chunk.InstructionMap[JumpReference!];
                break;
            case Opcode.Closure:
                FunctionReference = Chunk.Functions[B];
                break;
        }
    }

    public void Ib2SetupReferences()
    {
        switch (Opcode)
        {
            case Opcode.Eq or Opcode.Lt or Opcode.Le:
                JumpReference = Chunk.Instructions[B];
                AddIntoJumpReferenceParents();
                break;
            case Opcode.Jmp or Opcode.ForLoop or Opcode.ForPrep:
                JumpReference = Chunk.Instructions[B];
                AddIntoJumpReferenceParents();
                break;
            case Opcode.Closure:
                FunctionReference = Chunk.Functions[B];
                break;
        }
    }
    
    private void AddIntoJumpReferenceParents()
    {
        if (JumpReference is null)
            return;
        
        if (JumpReference.JumpReferenceParents.Contains(this))
            return;
        
        JumpReference.JumpReferenceParents.Add(this);
    }
    
    
    public override string ToString()
    {
        return $"{Opcode} {A} {B} {C}";
    }
    
}