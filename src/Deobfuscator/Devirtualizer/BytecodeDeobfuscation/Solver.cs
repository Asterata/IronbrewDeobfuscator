using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
//using IronbrewDeobfuscator.Deobfuscator.Devirtualizer.ControlFlowGraph;

namespace IronbrewDeobfuscator.Deobfuscator.Devirtualizer.BytecodeDeobfuscation;


/* TODO LIST
 - [+] Implement a quick code for the bytecode deobfuscator (aka. the solver)
 - [+] Implement a control flow graph
 - [+] Track node parents
 - [ ] Track node edges
 - [ ] Rewrite the control flow graph
 */
public class Solver(Session session) // Ugh. This is a quick code and this will be rewirtten in the future
{
    public void SolveTestFlip(Chunk chunk)
    {
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            if (instr.isDead) // Dont even try to solve dead instructions, because they should not be solved
                continue;
            if (instr.Opcode is not (Opcode.Eq or Opcode.Lt or Opcode.Le or Opcode.Test))
                continue;
            
            var nextInstr = chunk.Instructions[index + 1];
            var nextInstr2 = chunk.Instructions[index + 2];
            if (nextInstr.Opcode is not Opcode.Jmp && nextInstr2.Opcode is not Opcode.Jmp)
                continue;
            
            if (instr.JumpReference != nextInstr.JumpReference)
                continue;
            
            switch (instr.Opcode)
            {
                case Opcode.Eq or Opcode.Lt or Opcode.Le:
                    instr.SubOpcode = instr.SubOpcode == SubOpcode.A0 ? SubOpcode.A1 : SubOpcode.A0;
                    break;
                case Opcode.Test:
                    instr.SubOpcode = instr.SubOpcode == SubOpcode.C0 ? SubOpcode.C1 : SubOpcode.C0;
                    break;
            }

            instr.JumpReference = nextInstr.JumpReference;
            foreach (var parent in nextInstr.JumpReferenceParents)
            {
                if (parent != instr)
                    parent.JumpReference = instr;
            }
            chunk.Instructions.Remove(nextInstr);
            
        }
        chunk.UpdateMappings();
        
        if (session.EnableMaxCflowDeobfuscation)
        {
            for (var index = 0; index < chunk.Instructions.Count; index++)
            {
                var instr = chunk.Instructions[index];
                if (instr.isDead) // Dont even try to solve dead instructions, because they should not be solved
                    continue;
                if (instr.Opcode is not (Opcode.Eq or Opcode.Lt or Opcode.Le or Opcode.Test))
                    continue;

                var nextInstr = chunk.Instructions[index + 1];
                var nextInstr2 = chunk.Instructions[index + 2];
                if (nextInstr.Opcode is not Opcode.Jmp && nextInstr2.Opcode is not Opcode.Jmp)
                    continue;

                if (instr.B != nextInstr.B)
                    continue;

                switch (instr.Opcode)
                {
                    case Opcode.Eq or Opcode.Lt or Opcode.Le:
                        instr.SubOpcode = instr.SubOpcode == SubOpcode.A0 ? SubOpcode.A1 : SubOpcode.A0;
                        break;
                    case Opcode.Test:
                        instr.SubOpcode = instr.SubOpcode == SubOpcode.C0 ? SubOpcode.C1 : SubOpcode.C0;
                        break;
                }

                instr.JumpReference = nextInstr.JumpReference;
                foreach (var parent in nextInstr.JumpReferenceParents)
                {
                    if (parent != instr)
                        parent.JumpReference = instr;
                }

                chunk.Instructions.Remove(nextInstr);

            }

            chunk.UpdateMappings();

            for (var index = 0; index < chunk.Instructions.Count; index++)
            {
                var instr = chunk.Instructions[index];
                if (!instr.isDead) // Dont even try to solve dead instructions, because they should not be solved
                    continue;
                if (instr.Opcode is not (Opcode.Eq or Opcode.Lt or Opcode.Le or Opcode.Test))
                    continue;

                var nextInstr = chunk.Instructions[index + 1];
                if (nextInstr.Opcode is not Opcode.Jmp)
                    continue;

                if (instr.JumpReference != nextInstr)
                    continue;

                switch (instr.Opcode)
                {
                    case Opcode.Eq or Opcode.Lt or Opcode.Le:
                        instr.SubOpcode = instr.SubOpcode == SubOpcode.A0 ? SubOpcode.A1 : SubOpcode.A0;
                        break;
                    case Opcode.Test:
                        instr.SubOpcode = instr.SubOpcode == SubOpcode.C0 ? SubOpcode.C1 : SubOpcode.C0;
                        break;
                }

                instr.JumpReference = nextInstr.JumpReference;
                foreach (var parent in nextInstr.JumpReferenceParents)
                {
                    if (parent != instr)
                        parent.JumpReference = instr;
                }

                chunk.Instructions.Remove(nextInstr);

            }

            chunk.UpdateMappings();
        }
        
        
    }
    
    public void SolveTestSpam(Chunk chunk)
    {
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            
            switch (instr.SubOpcode)
            {
                case SubOpcode.A1:
                    if (instr.Opcode is (Opcode.Eq or Opcode.Lt or Opcode.Le))
                    {
                        if (instr.isDead) // Dont even try to solve dead instructions, because should not be solved
                            continue;

                        var jmpA1 = chunk.Instructions[index + 1];
                        if (jmpA1.Opcode is not Opcode.Jmp) // will always be a Jmp, but just to be sure
                            continue;

                        // Check if out instr jumps to a dead one
                        if (!instr.JumpReference!.isDead)
                            continue;
                        if (!jmpA1.JumpReference!.isDead)
                            continue;

                        // continue tracking the jump references until its not a dead one
                        while (instr.JumpReference!.isDead)
                        {
                            instr.JumpReference =
                                instr.JumpReference.JumpReference; // this is the true branch of the test
                        }

                        // continue tracking the jump references until its not a dead one
                        while (jmpA1.JumpReference!.isDead)
                        {
                            // get the next jump reference
                            var nextJmp = chunk.Instructions[chunk.InstructionMap[jmpA1.JumpReference!] + 1];
                            jmpA1.JumpReference = nextJmp.JumpReference;
                        }

                    }
                    break;
                
                case SubOpcode.A0:
                    if (instr.Opcode is (Opcode.Eq or Opcode.Lt or Opcode.Le))
                    {
                        if (instr.isDead) // Dont even try to solve dead instructions, because should not be solved
                            continue;

                        var jmpA0 = chunk.Instructions[index + 1];
                        if (jmpA0.Opcode is not Opcode.Jmp) // will always be a Jmp, but just to be sure
                            continue;

                        // Check if out instr jumps to a dead one
                        if (!instr.JumpReference!.isDead)
                            continue;
                        if (!jmpA0.JumpReference!.isDead)
                            continue;

                        // continue tracking the jump references until its not a dead one
                        while (jmpA0.JumpReference!.isDead)
                        {
                            jmpA0.JumpReference =
                                jmpA0.JumpReference.JumpReference; // this is the true branch of the test
                        }

                        // continue tracking the jump references until its not a dead one
                        while (instr.JumpReference!.isDead)
                        {
                            // get the next jump reference
                            var nextJmp = chunk.Instructions[chunk.InstructionMap[instr.JumpReference!] + 1];
                            instr.JumpReference = nextJmp.JumpReference;
                        }
                        
                        jmpA0.JumpReference = instr.JumpReference; // copy pasted code problems

                    }

                    break;
                    
                    
                case SubOpcode.C1:
                    if (instr.Opcode is Opcode.Test or Opcode.TestSet)
                    {
                        if (instr.isDead) // Dont even try to solve dead instructions, because should not be solved
                            continue;

                        var jmpC1 = chunk.Instructions[index + 1];
                        if (jmpC1.Opcode is not Opcode.Jmp) // will always be a Jmp, but just to be sure
                            continue;

                        // Check if out instr jumps to a dead one
                        if (!instr.JumpReference!.isDead)
                            continue;
                        if (!jmpC1.JumpReference!.isDead)
                            continue;

                        // continue tracking the jump references until its not a dead one
                        while (instr.JumpReference!.isDead)
                        {
                            instr.JumpReference =
                                instr.JumpReference.JumpReference; // this is the true branch of the test
                        }

                        // continue tracking the jump references until its not a dead one
                        while (jmpC1.JumpReference!.isDead)
                        {
                            // get the next jump reference
                            var nextJmp = chunk.Instructions[chunk.InstructionMap[jmpC1.JumpReference!] + 1];
                            jmpC1.JumpReference = nextJmp.JumpReference;
                        }

                    }
                    break;
                
                case SubOpcode.C0:
                    if (instr.Opcode is Opcode.Test or Opcode.TestSet)
                    {
                        if (instr.isDead) // Dont even try to solve dead instructions, because should not be solved
                            continue;

                        var jmpC0 = chunk.Instructions[index + 1];
                        if (jmpC0.Opcode is not Opcode.Jmp) // will always be a Jmp, but just to be sure
                            continue;

                        // Check if out instr jumps to a dead one
                        if (!instr.JumpReference!.isDead)
                            continue;
                        if (!jmpC0.JumpReference!.isDead)
                            continue;

                        // continue tracking the jump references until its not a dead one
                        while (instr.JumpReference!.isDead)
                        {
                            instr.JumpReference =
                                instr.JumpReference.JumpReference; // this is the true branch of the test
                        }

                        // continue tracking the jump references until its not a dead one
                        while (jmpC0.JumpReference!.isDead)
                        {
                            // get the next jump reference
                            var nextJmp = chunk.Instructions[chunk.InstructionMap[jmpC0.JumpReference!] + 1];
                            jmpC0.JumpReference = nextJmp.JumpReference;
                        }

                    }
                    break;
                default:
                    continue;
            }
            
        }
        RemoveReadInstructions(chunk);
        chunk.UpdateMappings();

    }
    
    public void SolveBounce(Chunk chunk)
    {
        // Remove every Jmp that jumps to another Jmp
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            if (instr.Opcode is not Opcode.Jmp)
                continue;

            var target = instr.JumpReference;
            if (target!.Opcode is not Opcode.Jmp)
                continue;

            instr.JumpReference = target.JumpReference;
            foreach (var parent in target.JumpReferenceParents)
            {
                if (parent != instr)
                    parent.JumpReference = instr;
            }

            chunk.Instructions.Remove(target);

            instr.Ib2UpdateRegisters();
        }
        chunk.UpdateMappings();
        
        // Check if theres any jmps referencing jmps
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            if (instr.Opcode is not Opcode.Jmp)
                continue;

            var target = instr.JumpReference;
            if (target!.Opcode is not Opcode.Jmp)
                continue;

            SolveBounce(chunk);
        }
    }
    
    public void FixCustomOpcodes(Chunk chunk)
    {
        foreach (var instr in chunk.Instructions)
        {
            if (instr.Opcode is Opcode.NewStack or Opcode.SetFenv or Opcode.PushStack or Opcode.SetTop or Opcode.SetFenv)
            {
                instr.Opcode = Opcode.LoadK;
                var constantB = new Constant("This file is deobfuscated by an open-source Ironbrew Deobfuscator. Please check the GitHub repository for more information.");
                chunk.Constants.Add(constantB);
                instr.ConstantReferences["B"] = constantB;
                instr.isKB = true;
                
            }
        }
    }
    
    public void FindGeneratedDeadInstructions(Chunk chunk)
    {
        // Remove dead code
        var returnFound = false;
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            if (instr.Opcode is Opcode.Return)
            {
                returnFound = true;
                continue;
            }
            
            if (returnFound)
                instr.isDead = true;
        }

        chunk.UpdateMappings();
    }
    
    public void RemoveReadInstructions(Chunk chunk)
    {
        // Remove dead code
        for (var index = 0; index < chunk.Instructions.Count; index++)
        {
            var instr = chunk.Instructions[index];
            if (instr.isDead)
            {
                chunk.Instructions.Remove(instr);
                index--;
            }
        }
        chunk.UpdateMappings();
    }
    
    public void AddFingerprint(Chunk chunk)
    {
        var nConstant = new Constant("This file is deobfuscated by an open-source Ironbrew Deobfuscator. Please check the GitHub repository for more information.");
        chunk.Constants.Add(nConstant);
        var nInstr = new Instruction(chunk,Opcode.LoadK,0,chunk.Constants.Count - 1,0);
        chunk.Instructions.Insert(0, nInstr);
    }
}