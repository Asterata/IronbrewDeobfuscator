namespace IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;

public enum Opcode
{
    Move,
    LoadK,
    LoadBool,
    LoadNil,
    GetUpval,
    GetGlobal,
    GetTable,
    SetGlobal,
    SetUpval,
    SetTable,
    NewTable,
    Self,
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Pow,
    Unm,
    Not,
    Len,
    Concat,
    Jmp,
    Eq,
    Lt,
    Le,
    Test,
    TestSet,
    Call,
    TailCall,
    Return,
    ForLoop,
    ForPrep,
    TForLoop,
    SetList,
    Close,
    Closure,
    Vararg,

    // Custom Opcodes
    NewStack,
    PushStack,
    SetFenv,
    SetTop,
    SuperOpcode,

    
    Nop,
    Unmatched
    
}