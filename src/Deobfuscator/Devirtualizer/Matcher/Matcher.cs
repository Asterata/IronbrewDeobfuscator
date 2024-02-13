using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using IronbrewDeobfuscator.Deobfuscator.ScriptRewriter;
using IronbrewDeobfuscator.Deobfuscator.Utils;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace IronbrewDeobfuscator.Deobfuscator.Devirtualizer.Matcher;

/// <summary>
/// Main class for matching the operations.
/// </summary>
/// <param name="session"></param>
public class Matcher(Session session)
{
    private readonly List<string> _fingerprints =
    [
        "Stack",
        "Upvalues",
        "Env"
    ];

    // Before you ask, this is NOT a string based matching. This is a fingerprint based matching.
    /// <summary>
    /// Static list of ironbrew2 opcodes.
    /// </summary>
    private readonly List<(string, Opcode, SubOpcode, ConstantMask, OperationType)> _ib2OpcodeList =
    [
        // Move
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]]", Opcode.Move, SubOpcode.None, ConstantMask.None, OperationType.AB),
        
        // Loadk
        ("Stack[Inst[OP_A]] = Inst[OP_B]", Opcode.LoadK, SubOpcode.None, ConstantMask.KB, OperationType.AB),
        
        // LoadBool
        ("Stack[Inst[OP_A]] = (Inst[OP_B] ~= 0);", Opcode.LoadBool, SubOpcode.C0, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = (Inst[OP_B] ~= 0); InstrPoint = InstrPoint + 1;", Opcode.LoadBool, SubOpcode.C1, ConstantMask.None, OperationType.ABC),
    
        // LoadNil
        ("for Idx = Inst[OP_A], Inst[OP_B] do Stack[Idx] = nil; end;", Opcode.LoadNil, SubOpcode.None, ConstantMask.None, OperationType.ABC),
    
        // GetUpval
        ("Stack[Inst[OP_A]] = Upvalues[Inst[OP_B]];", Opcode.GetUpval, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // GetGlobal
        ("Stack[Inst[OP_A]] = Env[Inst[OP_B]];", Opcode.GetGlobal, SubOpcode.None, ConstantMask.KB, OperationType.AB),
        
        // GetTable
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]][Stack[Inst[OP_C]]];", Opcode.GetTable, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]][Inst[OP_C]];", Opcode.GetTable, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        
        // SetGlobal
        ("Env[Inst[OP_B]] = Stack[Inst[OP_A]];", Opcode.SetGlobal, SubOpcode.None, ConstantMask.KB, OperationType.AB),
        
        // SetUpval
        ("Upvalues[Inst[OP_B]] = Stack[Inst[OP_A]];", Opcode.SetUpval, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // SetTable
        ("Stack[Inst[OP_A]][Stack[Inst[OP_B]]] = Stack[Inst[OP_C]];", Opcode.SetTable, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]][Inst[OP_B]] = Stack[Inst[OP_C]];", Opcode.SetTable, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]][Stack[Inst[OP_B]]] = Inst[OP_C];", Opcode.SetTable, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]][Inst[OP_B]] = Inst[OP_C];", Opcode.SetTable, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // NewTable
        ("Stack[Inst[OP_A]] = {};", Opcode.NewTable, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Self
        ("local A = Inst[OP_A]; local B = Stack[Inst[OP_B]]; Stack[A+1] = B; Stack[A] = B[Stack[Inst[OP_C]]];", Opcode.Self, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; local B = Stack[Inst[OP_B]]; Stack[A+1] = B; Stack[A] = B[Inst[OP_C]];", Opcode.Self, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        
        // Add
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] + Stack[Inst[OP_C]];", Opcode.Add, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] + Stack[Inst[OP_C]];", Opcode.Add, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] + Inst[OP_C];", Opcode.Add, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] + Inst[OP_C];", Opcode.Add, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Sub
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] - Stack[Inst[OP_C]];", Opcode.Sub, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] - Stack[Inst[OP_C]];", Opcode.Sub, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] - Inst[OP_C];", Opcode.Sub, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] - Inst[OP_C];", Opcode.Sub, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Mul
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] * Stack[Inst[OP_C]];", Opcode.Mul, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] * Stack[Inst[OP_C]];", Opcode.Mul, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] * Inst[OP_C];", Opcode.Mul, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] * Inst[OP_C];", Opcode.Mul, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Div
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] / Stack[Inst[OP_C]];", Opcode.Div, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] / Stack[Inst[OP_C]];", Opcode.Div, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] / Inst[OP_C];", Opcode.Div, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] / Inst[OP_C];", Opcode.Div, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Mod
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] % Stack[Inst[OP_C]];", Opcode.Mod, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] % Stack[Inst[OP_C]];", Opcode.Mod, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] % Inst[OP_C];", Opcode.Mod, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] % Inst[OP_C];", Opcode.Mod, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Pow
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] ^ Stack[Inst[OP_C]];", Opcode.Pow, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] ^ Stack[Inst[OP_C]];", Opcode.Pow, SubOpcode.None, ConstantMask.KB, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Stack[Inst[OP_B]] ^ Inst[OP_C];", Opcode.Pow, SubOpcode.None, ConstantMask.KC, OperationType.ABC),
        ("Stack[Inst[OP_A]] = Inst[OP_B] ^ Inst[OP_C];", Opcode.Pow, SubOpcode.None, ConstantMask.KBC, OperationType.ABC),
        
        // Unm
        ("Stack[Inst[OP_A]] = -Stack[Inst[OP_B]];", Opcode.Unm, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Not
        ("Stack[Inst[OP_A]] = not Stack[Inst[OP_B]];", Opcode.Not, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Len
        ("Stack[Inst[OP_A]] = #Stack[Inst[OP_B]];", Opcode.Len, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Concat
        ("local B = Inst[OP_B]; local K = Stack[B] for Idx = B+1, Inst[OP_C] do K = K .. Stack[Idx]; end; Stack[Inst[OP_A]] = K;", Opcode.Concat, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Jmp
        ("InstrPoint = Inst[OP_B]", Opcode.Jmp, SubOpcode.None, ConstantMask.None, OperationType.AB),
        
        // Eq
        ("if (Stack[Inst[OP_A]] == Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A0, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] == Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A0, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] == Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A0, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] == Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A0, ConstantMask.KAC, OperationType.ABC),
        
        // Lt
        ("if (Stack[Inst[OP_A]] < Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Lt, SubOpcode.A0, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] < Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Lt, SubOpcode.A0, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] < Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Lt, SubOpcode.A0, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] < Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Lt, SubOpcode.A0, ConstantMask.KAC, OperationType.ABC),
        
        // Le
        ("if (Stack[Inst[OP_A]] <= Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Le, SubOpcode.A0, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] <= Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Le, SubOpcode.A0, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] <= Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Le, SubOpcode.A0, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] <= Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Le, SubOpcode.A0, ConstantMask.KAC, OperationType.ABC),
        
        // Ne
        ("if (Stack[Inst[OP_A]] ~= Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A1, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] ~= Stack[Inst[OP_C]]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A1, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] ~= Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A1, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] ~= Inst[OP_C]) then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Eq, SubOpcode.A1, ConstantMask.KAC, OperationType.ABC),
        
        // Ge
        ("if (Stack[Inst[OP_A]] < Stack[Inst[OP_C]]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Lt, SubOpcode.A1, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] < Stack[Inst[OP_C]]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Lt, SubOpcode.A1, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] < Inst[OP_C]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Lt, SubOpcode.A1, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] < Inst[OP_C]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Lt, SubOpcode.A1, ConstantMask.KAC, OperationType.ABC),
    
        // Gt
        ("if (Stack[Inst[OP_A]] <= Stack[Inst[OP_C]]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Le, SubOpcode.A1, ConstantMask.None, OperationType.ABC),
        ("if (Inst[OP_A] <= Stack[Inst[OP_C]]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Le, SubOpcode.A1, ConstantMask.KA, OperationType.ABC),
        ("if (Stack[Inst[OP_A]] <= Inst[OP_C]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Le, SubOpcode.A1, ConstantMask.KC, OperationType.ABC),
        ("if (Inst[OP_A] <= Inst[OP_C]) then InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.Le, SubOpcode.A1, ConstantMask.KAC, OperationType.ABC),
        
        // Test
        ("if Stack[Inst[OP_A]] then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Test, SubOpcode.C0, ConstantMask.None, OperationType.AB),
        ("if not Stack[Inst[OP_A]] then InstrPoint = InstrPoint + 1; else InstrPoint = Inst[OP_B]; end;", Opcode.Test, SubOpcode.C1, ConstantMask.None, OperationType.AB),
        
        // TestSet
        ("local B = Stack[Inst[OP_C]]; if B then InstrPoint = InstrPoint+1; else Stack[Inst[OP_A]] = B; InstrPoint = Inst[OP_B]; end;", Opcode.TestSet, SubOpcode.C0, ConstantMask.None, OperationType.ABC),
        ("local B = Stack[Inst[OP_C]]; if not B then InstrPoint = InstrPoint+1; else Stack[Inst[OP_A]] = B; InstrPoint = Inst[OP_B]; end;", Opcode.TestSet, SubOpcode.C1, ConstantMask.None, OperationType.ABC),
        
        // Call, oh god...
        ("local A = Inst[OP_A]; local Results = { Stack[A](Unpack(Stack, A + 1, Inst[OP_B])) }; local Edx = 0; for Idx = A, Inst[OP_C] do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        ("local A = Inst[OP_A]; local Results = { Stack[A](Unpack(Stack, A + 1, Top)) }; local Edx = 0; for Idx = A, Inst[OP_C] do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B0, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; local Results = { Stack[A]() }; local Limit = Inst[OP_C]; local Edx = 0; for Idx = A, Limit do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B1, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; local Results = { Stack[A](Stack[A + 1]) }; local Edx = 0; for Idx = A, Inst[OP_C] do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B2, ConstantMask.None, OperationType.ABC),
        
        ("local A = Inst[OP_A]; local Results, Limit = _R(Stack[A](Unpack(Stack, A + 1, Inst[OP_B]))) Top = Limit + A - 1 local Edx = 0; for Idx = A, Top do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.C0, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A](Unpack(Stack, A + 1, Inst[OP_B]));", Opcode.Call, SubOpcode.C1, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A] = Stack[A](Unpack(Stack, A + 1, Inst[OP_B])) ", Opcode.Call, SubOpcode.C2, ConstantMask.None, OperationType.ABC),
        
        ("local A = Inst[OP_A]; local Results, Limit = _R(Stack[A](Unpack(Stack, A + 1, Top))) Top = Limit + A - 1 local Edx = 0; for Idx = A, Top do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B0C0, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A](Unpack(Stack, A + 1, Top))", Opcode.Call, SubOpcode.B0C1, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A] = Stack[A](Unpack(Stack, A + 1, Top))", Opcode.Call, SubOpcode.B0C2, ConstantMask.None, OperationType.ABC),
        
        ("local A = Inst[OP_A]; local Results, Limit = _R(Stack[A]()) Top = Limit + A - 1 local Edx = 0; for Idx = A, Top do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B1C0, ConstantMask.None, OperationType.ABC),
        ("Stack[Inst[OP_A]]();", Opcode.Call, SubOpcode.B1C1, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A] Stack[A] = Stack[A]()", Opcode.Call, SubOpcode.B1C2, ConstantMask.None, OperationType.ABC),
        
        ("local A = Inst[OP_A]; local Results, Limit = _R(Stack[A](Stack[A + 1])) Top = Limit + A - 1 local Edx = 0; for Idx = A, Top do Edx = Edx + 1; Stack[Idx] = Results[Edx]; end;", Opcode.Call, SubOpcode.B2C0, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A](Stack[A + 1])", Opcode.Call, SubOpcode.B2C1, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Stack[A] = Stack[A](Stack[A + 1]) ", Opcode.Call, SubOpcode.B2C2, ConstantMask.None, OperationType.ABC),
        
        
        // TailCall
        ("local A = Inst[OP_A]; do return Stack[A](Unpack(Stack, A + 1, Inst[OP_B])) end;", Opcode.TailCall, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; do return Stack[A](Unpack(Stack, A + 1, Top)) end;", Opcode.TailCall, SubOpcode.B0, ConstantMask.None, OperationType.ABC),
        ("do return Stack[Inst[OP_A]](); end;", Opcode.TailCall, SubOpcode.B1, ConstantMask.None, OperationType.ABC),
        
        // Return
        ("local A = Inst[OP_A]; do return Unpack(Stack, A, A + Inst[OP_B]) end;", Opcode.Return, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; do return Unpack(Stack, A, Top) end;", Opcode.Return, SubOpcode.B0, ConstantMask.None, OperationType.ABC),
        ("do return end;", Opcode.Return, SubOpcode.B1, ConstantMask.None, OperationType.ABC),
        ("do return Stack[Inst[OP_A]] end", Opcode.Return, SubOpcode.B2, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; do return Stack[A], Stack[A + 1] end", Opcode.Return, SubOpcode.B3, ConstantMask.None, OperationType.ABC),
        
        // ForLoop
        ("local A = Inst[OP_A]; local Step = Stack[A + 2]; local Index = Stack[A] + Step; Stack[A] = Index; if (Step > 0) then if (Index <= Stack[A+1]) then InstrPoint = Inst[OP_B]; Stack[A+3] = Index; end elseif (Index >= Stack[A+1]) then InstrPoint = Inst[OP_B]; Stack[A+3] = Index; end", Opcode.ForLoop, SubOpcode.None, ConstantMask.None, OperationType.AB),
        
        
        // ForPrep
        ("local A = Inst[OP_A]; local Index = Stack[A] local Step = Stack[A + 2]; if (Step > 0) then if (Index > Stack[A+1]) then InstrPoint = Inst[OP_B]; else Stack[A+3] = Index; end elseif (Index < Stack[A+1]) then InstrPoint = Inst[OP_B]; else Stack[A+3] = Index; end", Opcode.ForPrep, SubOpcode.None, ConstantMask.None, OperationType.AB),
        
        // TForLoop
        ("local A = Inst[OP_A]; local C = Inst[OP_C]; local CB = A + 2 local Result = {Stack[A](Stack[A + 1],Stack[CB])}; for Idx = 1, C do Stack[CB + Idx] = Result[Idx]; end; local R = Result[1] if R then Stack[CB] = R InstrPoint = Inst[OP_B]; else InstrPoint = InstrPoint + 1; end;", Opcode.TForLoop, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // SetList
        ("local A = Inst[OP_A]; local T = Stack[A]; for Idx = A + 1, Inst[OP_B] do Insert(T, Stack[Idx]) end;", Opcode.SetList, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; local T = Stack[A]; for Idx = A + 1, Top do Insert(T, Stack[Idx]) end;", Opcode.SetList, SubOpcode.B0, ConstantMask.None, OperationType.ABC),
        ("InstrPoint = InstrPoint + 1 local A = Inst[OP_A]; local T = Stack[A]; for Idx = A + 1, Inst[OP_B] do Insert(T, Stack[Idx]) end;", Opcode.SetList, SubOpcode.C0, ConstantMask.None, OperationType.ABC),
        
        // Close
        ("local A = Inst[OP_A]; local Cls={}; for Idx=1,#Lupvals do local List = Lupvals[Idx]; for Idz=0,#List do local Upv = List[Idz]; local NStack = Upv[1]; local Pos=Upv[2]; if NStack == Stack and Pos >= A then Cls[Pos] = NStack[Pos]; Upv[1] = Cls; end; end; end;", Opcode.Close, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // Closure
        ("local NewProto=Proto[Inst[OP_B]]; local NewUvals; local Indexes={}; NewUvals=Setmetatable({}, {__index=function(_,Key) local Val=Indexes[Key]; return Val[1][Val[2]]; end, __newindex=function(_,Key,Value)local Val=Indexes[Key] Val[1][Val[2]]=Value; end; }); for Idx=1,Inst[OP_C] do InstrPoint=InstrPoint+1; local Mvm=Instr[InstrPoint];if Mvm[OP_ENUM]==OP_MOVE then Indexes[Idx-1]={Stack,Mvm[OP_B]}; else Indexes[Idx-1]={Upvalues,Mvm[OP_B]}; end; Lupvals[#Lupvals+1]=Indexes; end; Stack[Inst[OP_A]] = Wrap(NewProto,NewUvals,Env);", Opcode.Closure, SubOpcode.None, ConstantMask.None, OperationType.AB),
        ("Stack[Inst[OP_A]] = Wrap(Proto[Inst[OP_B]],nil,Env);", Opcode.Closure, SubOpcode.NoUpvalues, ConstantMask.None, OperationType.AB),
        
        // Vararg
        ("local A = Inst[OP_A]; local B=Inst[OP_B]; for Idx = A,B do Stack[Idx] = Vararg[Idx-A]; end;", Opcode.Vararg, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        ("local A = Inst[OP_A]; Top = A+Varargsz-1; for Idx = A,Top do local VA = Vararg[Idx-A]; Stack[Idx] = VA; end;", Opcode.Vararg, SubOpcode.B0, ConstantMask.None, OperationType.ABC),
        
        // Custom IB2 Opcodes
        // NewStack
        ("Stack = {}; for Idx = 0, PCount do if Idx < Params then Stack[Idx] = Args[Idx + 1]; else break end; end;", Opcode.NewStack, SubOpcode.None, ConstantMask.None, OperationType.AB),
        
        // SetFenv
        ("Env = Stack[Inst[OP_A]]", Opcode.SetFenv, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // PushStack
        ("Stack[Inst[OP_A]] = Stack", Opcode.PushStack, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
        // SetTop
        ("Top = Inst[OP_A];", Opcode.SetTop, SubOpcode.None, ConstantMask.None, OperationType.ABC),
        
    ];
    
    /// <summary>
    /// Used to match the simple operations.
    /// </summary>
    public void MatchSimpleOperations()
    {
        var dOperations = session.BinaryTreeInformation;
        var operationEnum = dOperations.Keys.ToList();

        foreach (var opcodeEnum  in operationEnum)
        {
            var operation = dOperations[opcodeEnum];
            operation.OperationBody = new OpcodeRewriter(session).Visit(operation.OperationBody!) as StatementListSyntax;
            var operationCode = operation.OperationBody;
            if (operationCode == null)
                operation.Opcode = Opcode.Nop;
            
            // Match the simple operations.
            if (SearchIb2OpcodeList(operationCode!.DescendantNodes().ToList(), operation)) 
                continue; // If it matched, continue to the next one.
            
            if (session.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"[WARNING] Enum {opcodeEnum} did not match any of the Ironbrew2 Opcodes. Might be a super opcode. Skipping...");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Used to match the super operations.
    /// </summary>
    public void MatchSuperOperations()
    {
        var superOpcodeFingerprint = SyntaxFactory
        .ParseCompilationUnit("InstrPoint = InstrPoint + 1; Inst = Instr[InstrPoint];").Statements;
    
        // Hopefully the only ones left unmatched are the super opcodes.
        var unmatchedOperationsDictionary = session.BinaryTreeInformation.Where(x => x.Value.Opcode == Opcode.Unmatched).ToDictionary(x => x.Key, x => x.Value);
        var unmatchedOperationKeys = unmatchedOperationsDictionary.Keys.ToList();
        
        foreach (var opcodeEnum in unmatchedOperationKeys)
        {
            var operation = unmatchedOperationsDictionary[opcodeEnum];
            operation.IsSuperOpcode = true;
            operation.Opcode = Opcode.SuperOpcode;
            operation.OperationBody = new OpcodeRewriter(session).Visit(operation.OperationBody!) as StatementListSyntax;
            operation.OperationBody = new SuperOpcodeRewriter().Visit(operation.OperationBody!) as StatementListSyntax;
            
            var operationCode = operation.OperationBody;
            if (operationCode == null)
                continue;
            
            var operationCodeNodes = operationCode.DescendantNodes().ToList();
            var superOpcodeFingerprintSyntaxNodes = superOpcodeFingerprint.DescendantNodes().ToList();
            
            // Split the operationCodeSyntaxNodes using the superOpcodeFingerprintSyntaxNodes pattern. 
            var splittedList = operationCodeNodes.SplitListByPattern<SyntaxNode>(superOpcodeFingerprintSyntaxNodes).ToList();
            
            // Now we will match the smaller parts using _ib2OpcodeList.
            foreach (var operationCodeSyntaxNodes in splittedList)
            {
                if (SearchIb2OpcodeList(operationCodeSyntaxNodes, operation, true)) 
                    continue;
                
                if (session.IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] Operation did not match any of the Ironbrew2 Opcodes.\n");
                    Console.ResetColor();
                    Console.WriteLine($"SuperOpcode:\n{operation.OperationBody!.NormalizeWhitespace().ToFullString()}\n\n");
                    throw new Exception($"Operation:\n{operationCodeSyntaxNodes[0].NormalizeWhitespace().ToFullString()}");
                }
            }
            
            if (session.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"[FIXED] Super Opcode {opcodeEnum} was a Super Opcode. No need to worry!");
                Console.ResetColor();
            }
            
        }
        
    }
    

    private bool SearchIb2OpcodeList(IReadOnlyList<SyntaxNode> operationCodeSyntaxNodes, Operation operation, bool isSuperOperation = false)
    {
        foreach (var (opcodeString, opcode, subOpcode, constantMask, operationType) in _ib2OpcodeList)
        {
            var opcodeStringSyntax = SyntaxFactory.ParseCompilationUnit(opcodeString).Statements;

            // Match every syntax.
            var opcodeStringSyntaxNodes = opcodeStringSyntax.DescendantNodes().ToList();

            // Optimization :P
            if (opcodeStringSyntaxNodes.Count != operationCodeSyntaxNodes.Count)
                continue;

            // Matching time!
            if (!MatchDescendantNodes(opcodeStringSyntaxNodes, operationCodeSyntaxNodes, opcode, operation))
                continue; // If it didnt match, continue to the next one.
                    
            var newOperation = GenerateOperationWithNoBody(opcode, subOpcode, constantMask, operationType);
            if (isSuperOperation)
            {
                operation.SuperOpcode.Operations.Add(newOperation);
            }
            else
            {
                operation.Opcode = opcode;
                operation.SubOpcode = subOpcode;
                operation.ConstantMask = constantMask;
                operation.OperationType = operationType;
            }
            
            return true;
        }

        return false;
    }

    
    private bool MatchDescendantNodes(IReadOnlyList<SyntaxNode> opcodeStringSyntaxNodes, IReadOnlyList<SyntaxNode> operationCodeSyntaxNodes, Opcode opcode = default, Operation? operation = default)
    {
        var matchList = new List<bool>(); // Important for debugging any problems, if any.
        for (var i = 0; i < opcodeStringSyntaxNodes.Count; i++)
        {
            var opcodeNode = opcodeStringSyntaxNodes[i];
            var operationNode = operationCodeSyntaxNodes[i];

            // We will match the registers here.
            // OP_A, OP_B, OP_C
            if (operationNode is LiteralExpressionSyntax { Parent: ElementAccessExpressionSyntax } &&
                opcodeNode is IdentifierNameSyntax
                {
                    Name: ("OP_A" or "OP_B" or "OP_C")
                } opcodeIdentifier)
            {
                // Add the register to the InstructionInformation.
                session.InstructionInformation.TryAdd(opcodeIdentifier.Name,
                    int.Parse(operationNode.ToString()));

                if (session.IsDebug)
                {
                    if (int.Parse(operationNode.ToString()) !=
                        session.InstructionInformation[opcodeIdentifier.Name])
                    {
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("[WARNING] OP_A, OP_B, OP_C does not match the value in the InstructionInformation.");
                        Console.WriteLine($"Default -| {opcodeIdentifier.Name}: {session.InstructionInformation["OP_A"]}");
                        Console.WriteLine($"{opcode} -| {opcodeIdentifier.Name}: {int.Parse(operationNode.ToString())}");
                        Console.ResetColor();
                    }
                }
                
                matchList.Add(true);
                continue;
            }
            

            // Fingerprint Matching.
            // This part is the most important part of the matcher.
            if (operationNode is IdentifierNameSyntax operationIdentifier &&
                opcodeNode is IdentifierNameSyntax opcodeStringIdentifier)
            {
                // Check if their Fingerprint contains the operationIdentifier and opcodeStringIdentifier.
                if (_fingerprints.Contains(operationIdentifier.Name) &&
                    _fingerprints.Contains(opcodeStringIdentifier.Name))
                {
                    if (operationIdentifier.Name == opcodeStringIdentifier.Name)
                    {
                        matchList.Add(true);
                        continue;
                    }

                    if (operationIdentifier.Name != opcodeStringIdentifier.Name)
                    {
                        matchList.Add(false);
                        break;
                    }
                }

                matchList.Add(true);
                continue;
            }

            if (opcodeNode.Kind() != operationNode.Kind())
            {
                matchList.Add(false);
                break;
            }

            matchList.Add(true);
        }

        if (matchList.All(x => x))
            return true;
        
        
        if (operation != null)
            operation.DebugInformation = "opcodeStringSyntaxNodes and operationCodeSyntaxNodes does not match.";
        return false;

    }


    private static Operation GenerateOperationWithNoBody(Opcode opcode, SubOpcode subOpcode, ConstantMask constantMask, OperationType operationType)
    {
        return new Operation
        {
            Opcode = opcode,
            SubOpcode = subOpcode,
            ConstantMask = constantMask,
            OperationBody = default,
            OperationType = operationType
        };
    }

}