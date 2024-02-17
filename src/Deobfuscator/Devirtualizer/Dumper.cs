using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using NLua;

namespace IronbrewDeobfuscator.Deobfuscator.Devirtualizer;

public class Dumper(Session session)
{
    /// <returns> <see cref="string"/> </returns>
    private string GetDumperScript()
    {
        var dumperScript = @$"
do
    IRONBREW2_DEOBFUSCATOR_CONNECTED = true
    IRONBREW2_DEOBFUSCATOR_CHUNK = {session.WrapperInformation["Chunk"]}

    --print('[INSIDE LUA] Ironbrew Deobfuscator: Connected to the Ironbrew Deobfuscator.\n')
    return --[[ Ironbrew Deobfuscator: Connected to the Ironbrew Deobfuscator. ]]
end
    ";
        
        return dumperScript;
    }
    
    private LuaTable Dump()
    {
        var dumperScript = GetDumperScript();
        
        if (session.InterpreterNode is null)
            throw new InvalidOperationException("Interpreter node is not found.");
        
        var rewritten = new InterpreterRewriter(session, dumperScript).Visit(session.Script);
        
        if (!session.IsInterpreterRewritten)
            throw new InvalidOperationException("Interpreter node is not rewritten.");
        
        session.RewrittenScript = rewritten;
        
        if (session.IsDebug)
            File.WriteAllText("dumperInterpreterOutput.lua", rewritten.NormalizeWhitespace().ToFullString());

        var codeForSandboxing = @"
import = nil
os = nil
io = nil
debug = nil
loadstring = nil
load = nil
coroutine = nil
collectgarbage = nil
setfenv = nil

-- Official Lua Sandboxing
arg = nil
dofile = nil
loadfile = nil
package = nil
require = nil

string.rep = nil
getmetatable = nil
setmetatable = nil
rawequal = nil
rawget = nil
rawset = nil
module = nil
_G = nil
string.dump = nil
math.randomseed = nil
math.random = nil

math.ldexp = function(m,n) return m*(2^n) end
";
        
        var nlua = new Lua();
        nlua.DoString(@$"
{codeForSandboxing}

{rewritten.ToFullString()}
");
        if (nlua["IRONBREW2_DEOBFUSCATOR_CONNECTED"] is false or null)
            throw new InvalidOperationException("Ironbrew Deobfuscator: Connection to the code failed.");
        
        if (session.IsDebug)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[STEP] Ironbrew Deobfuscator: Connection to the code succeeded.");
            Console.ResetColor();
            Console.WriteLine();
        }
        
        if (nlua["IRONBREW2_DEOBFUSCATOR_CHUNK"] is null)
            throw new InvalidOperationException("Ironbrew Deobfuscator: Chunk is not found.");

        return (LuaTable)nlua["IRONBREW2_DEOBFUSCATOR_CHUNK"];
    }
    
    /// <summary>
    /// Generates and returns a chunk from the dumped information.
    /// </summary>
    /// <returns> <see cref="Chunk"/> </returns>
    public Chunk GetDumpedChunk()
    {
        var dumpedInformation = Dump();
        var chunk = new Chunk
        {
            _functionId = "KbMjqNI7fyI8lYWRaawl4cqq1Ogojrp/IwOEnFWV5qY="
        };

        GetInstructions(chunk, dumpedInformation);
        GetPrototypes(chunk, dumpedInformation);
        GetParameters(chunk, dumpedInformation);
        
        chunk.UpdateMappings();

        foreach (var instr in chunk.Instructions)
        {
            instr.Ib2SetupReferences();
        }

        return chunk;
    }
    
    private void GetInstructions(Chunk chunk, LuaTable dumpedInformation)
    {
        var superOpcodeCount = 0;
        Operation? supOperation = null;
        foreach (var instruction in (LuaTable)dumpedInformation[session.ChunkInformation["Instr"]])
        {
            var instructionLuaTable = (LuaTable)((KeyValuePair<object, object>)instruction).Value;
            
            if (superOpcodeCount > 1)
            {
                var superOperation = supOperation!.SuperOpcode.Operations[0];
                var newSuperInstr = GenerateInstruction(chunk, superOperation, instructionLuaTable);
                supOperation.SuperOpcode.Operations.RemoveAt(0);
                chunk.Instructions.Add(newSuperInstr);
                superOpcodeCount--;
                continue;
            }
            
            var opcEnum = instructionLuaTable[session.InstructionInformation["Enum"]];
            var operation = session.BinaryTreeInformation[(int)(long)opcEnum];
            
            if (operation.IsSuperOpcode)
            {
                var superOpcode = operation.SuperOpcode;
                var superCount = superOpcode.Operations.Count;
                supOperation = operation;
                superOpcodeCount = superCount;
                
                var superOperation = superOpcode.Operations[0];
                var newSuperInstr = GenerateInstruction(chunk, superOperation, instructionLuaTable);
                superOpcode.Operations.RemoveAt(0);
                chunk.Instructions.Add(newSuperInstr);
                continue;
            }
            // turn ICollection into dictionary
            var instr = GenerateInstruction(chunk, operation, instructionLuaTable);
            
            chunk.Instructions.Add(instr);
        }

    }
    
    private void GetPrototypes(Chunk chunk, LuaTable dumpedInformation)
    {
        var prototypes = (LuaTable)dumpedInformation[session.ChunkInformation["Proto"]];
        foreach (var prototype in prototypes.Values)
        {
            var prototypeLuaTable = (LuaTable)prototype;
            var newChunk = new Chunk();
            GetInstructions(newChunk, prototypeLuaTable);
            GetPrototypes(newChunk, prototypeLuaTable);
            GetParameters(newChunk, prototypeLuaTable);
            
            newChunk.UpdateMappings();
            foreach (var instr in newChunk.Instructions)
            {
                instr.Ib2SetupReferences();
            }
            
            chunk.Functions.Add(newChunk);
        }

    }
    
    
    private void GetParameters(Chunk chunk, LuaTable dumpedInformation)
    {
        var parameters = dumpedInformation[session.ChunkInformation["Params"]];
        chunk.ParameterCount = (byte)(long)parameters;
    }
    
    private Instruction GenerateInstruction(Chunk chunk, Operation operation, LuaTable instructionLuaTable)
    {
        var newInstr = new Instruction(chunk, operation.Opcode, 0, 0, 0);
        switch (operation.OperationType)
        {
            case OperationType.ABC:
            {
                switch (operation.ConstantMask)
                {
                    case ConstantMask.KA:
                        var constantA1 = new Constant(instructionLuaTable[session.InstructionInformation["OP_A"]]);
                        newInstr.A = TryAddConstant(chunk, constantA1);
                        newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        newInstr.C = (int)(long)instructionLuaTable[session.InstructionInformation["OP_C"]];
                        newInstr.ConstantReferences.Add("A", constantA1);
                        newInstr.isKA = true;
                        break;
                    case ConstantMask.KB:
                        var constantB2 = new Constant(instructionLuaTable[session.InstructionInformation["OP_B"]]);
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        newInstr.B = TryAddConstant(chunk, constantB2);
                        newInstr.C = (int)(long)instructionLuaTable[session.InstructionInformation["OP_C"]];
                        newInstr.ConstantReferences.Add("B", constantB2);
                        newInstr.isKB = true;
                        break;
                    case ConstantMask.KC:
                        var constantC3 = new Constant(instructionLuaTable[session.InstructionInformation["OP_C"]]);
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        newInstr.C = TryAddConstant(chunk, constantC3);
                        newInstr.ConstantReferences.Add("C", constantC3);
                        newInstr.isKC = true;
                        
                        break;
                    case ConstantMask.KAC:
                        var constantA4 = new Constant(instructionLuaTable[session.InstructionInformation["OP_B"]]);
                        var constantC4 = new Constant(instructionLuaTable[session.InstructionInformation["OP_C"]]);
                        newInstr.A = TryAddConstant(chunk, constantA4);
                        newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        newInstr.C = TryAddConstant(chunk, constantC4);
                        newInstr.ConstantReferences.Add("A", constantA4);
                        newInstr.ConstantReferences.Add("C", constantC4);
                        newInstr.isKA = true;
                        newInstr.isKC = true;
                        break;
                    case ConstantMask.KBC:
                        var constantB5 = new Constant(instructionLuaTable[session.InstructionInformation["OP_B"]]);
                        var constantC5 = new Constant(instructionLuaTable[session.InstructionInformation["OP_C"]]);
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        newInstr.B = TryAddConstant(chunk, constantB5);
                        newInstr.C = TryAddConstant(chunk, constantC5);
                        newInstr.ConstantReferences.Add("B", constantB5);
                        newInstr.ConstantReferences.Add("C", constantC5);
                        newInstr.isKB = true;
                        newInstr.isKC = true;
                        break;
                    case ConstantMask.None:
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        if (operation.Opcode is Opcode.Eq or Opcode.Lt or Opcode.Le or Opcode.TForLoop or Opcode.ForLoop or Opcode.ForPrep or Opcode.Test or Opcode.TestSet)
                            newInstr.B = (int)(double)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        else
                            newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        newInstr.C = (int)(long)instructionLuaTable[session.InstructionInformation["OP_C"]];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            }
            case OperationType.AB:
            {
                switch (operation.ConstantMask)
                {
                    case ConstantMask.KA:
                        var constantA = new Constant(instructionLuaTable[session.InstructionInformation["OP_A"]]);
                        newInstr.A = TryAddConstant(chunk, constantA);
                        newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        newInstr.ConstantReferences.Add("A", constantA);
                        newInstr.isKA = true;
                        break;
                    case ConstantMask.KB:
                        var constantB = new Constant(instructionLuaTable[session.InstructionInformation["OP_B"]]);
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        newInstr.B = TryAddConstant(chunk, constantB);
                        newInstr.ConstantReferences.Add("B", constantB);
                        newInstr.isKB = true;
                        break;
                    case ConstantMask.None:
                        newInstr.A = (int)(long)instructionLuaTable[session.InstructionInformation["OP_A"]];
                        
                        if (operation.Opcode is Opcode.Jmp or Opcode.ForLoop or Opcode.ForPrep || operation.Opcode is Opcode.Closure && operation.SubOpcode == SubOpcode.None)
                            newInstr.B = (int)(double)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        else
                            newInstr.B = (int)(long)instructionLuaTable[session.InstructionInformation["OP_B"]];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            }
        }
        
        newInstr.SubOpcode = operation.SubOpcode;

        return newInstr;
    }

    private static int TryAddConstant(Chunk chunk, Constant constant)
    {
        if (chunk.Constants.Contains(constant))
            return chunk.Constants.IndexOf(constant);
        
        chunk.Constants.Add(constant);
        return chunk.Constants.Count - 1;
    }
}