using IronbrewDeobfuscator.Deobfuscator.Bytecode;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;
using IronbrewDeobfuscator.Deobfuscator.Devirtualizer;
using IronbrewDeobfuscator.Deobfuscator.Devirtualizer.BytecodeDeobfuscation;
using IronbrewDeobfuscator.Deobfuscator.Devirtualizer.Matcher;
using IronbrewDeobfuscator.Deobfuscator.Walkers;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Experimental;
using Loretta.CodeAnalysis.Lua.Experimental.Minifying;

namespace IronbrewDeobfuscator;
public static class Deobfuscation
{
	/// <summary>
	/// Deobfuscate Ironbrew2 script
	/// </summary>
	/// <param name="source"> Source of the ib2 script</param>
	/// <param name="enableMaxCflowDeobfuscation"> Enable MaxCflow deobfuscation</param>
	/// <param name="isDebug">Enable debugging</param>
	/// <returns >IEnumerable byte[]</returns>
	/// <exception cref="InvalidOperationException"></exception>
    public static IEnumerable<byte> HandleIb2Deobfuscation(string source, bool enableMaxCflowDeobfuscation = false, bool isDebug = false)
    {
	    var tree = LuaSyntaxTree.Create(SyntaxFactory.ParseCompilationUnit(source)).Minify(NamingStrategies.Alphabetical, new SequentialSlotAllocator());

		var scriptRoot = tree.GetRoot();
		var session = new Session(scriptRoot, enableMaxCflowDeobfuscation)
		{
			IsDebug = isDebug
		};
		
		// Walking the script to find usefull information
		new WrapperWalker(session).Visit(scriptRoot);
		new InformationWalker(session).Visit(scriptRoot);
		new InterpreterWalker(session).Visit(session.WrapperFunction);
		
		// Check walkers if anything is missing
	    if (session.WrapperFunction == null || session.InterpreterNode == null)
			throw new InvalidOperationException("Wrapper function or interpreter node is missing.");

		// Sort the session.BinaryTreeInformation by key
		session.BinaryTreeInformation = session.BinaryTreeInformation.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

		// Opcode matching
		var matcher = new Matcher(session);
		matcher.MatchSimpleOperations();
		matcher.MatchSuperOperations();

		// Print the binary tree information
		if (session.IsDebug)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Binary Tree Information:");
			foreach (var dict in session.BinaryTreeInformation)
			{
				switch (dict.Value.Opcode)
				{
					case Opcode.SuperOpcode:
						Console.WriteLine($"\tEnum: {dict.Key}, Opcode: {dict.Value.Opcode}");
						foreach (var superOpcode in dict.Value.SuperOpcode.Operations)
							Console.WriteLine(dict.Value.SubOpcode == SubOpcode.None
								? $"\tEnum: {dict.Key}, Opcode: {dict.Value.Opcode}"
								: $"\tEnum: {dict.Key}, Opcode: {dict.Value.Opcode} | SubOpcode: {dict.Value.SubOpcode}");
						break;
					case Opcode.Unmatched:
						throw new InvalidOperationException("Operation is not matched.");
					default:
						Console.WriteLine(dict.Value.SubOpcode == SubOpcode.None
							? $"\tEnum: {dict.Key}, Opcode: {dict.Value.Opcode}"
							: $"\tEnum: {dict.Key}, Opcode: {dict.Value.Opcode} | SubOpcode: {dict.Value.SubOpcode}");
						break;
				}
			}
			Console.ResetColor();
			Console.WriteLine();
		}

		// Check the InstructionInformation
		var contains = session.InstructionInformation.Keys.ToList().Contains("OP_C");
		if (!contains)
		{
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine("[FIXED] OP_C cannot be found, OP_C has been set to 4. No need to worry!");
			Console.ResetColor();
	
			Console.WriteLine();
			session.InstructionInformation.Add("OP_C", 4);
		}

		// Dumping + Generating IR
		var dumper = new Dumper(session);
		var mainChunk = dumper.GetDumpedChunk();

		// Bytecode deobfuscation
		var solver = new Solver(session);
		solver.FindGeneratedDeadInstructions(mainChunk);

		if (session.IsDebug)
		{
			var information = PrintFunctionDebug(mainChunk);
			File.WriteAllText("normal-bytecode-listing.lua", information);
		}
		
		if (session.EnableMaxCflowDeobfuscation)
		{
			if (session.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[DEBUG] Trying to deobfuscate the bytecode with IB_MAX_CFLOW() enabled.");
				Console.ResetColor();
				Console.WriteLine();
			}
			solver.SolveBounce(mainChunk);
			solver.SolveTestFlip(mainChunk);
			solver.SolveTestSpam(mainChunk);
			solver.FixCustomOpcodes(mainChunk);
			solver.AddFingerprint(mainChunk);
		}
		else
		{
			if (session.IsDebug)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[DEBUG] Trying to deobfuscate bytecode normally.");
				Console.ResetColor();
				Console.WriteLine();
			}
			solver.SolveTestFlip(mainChunk);
			solver.FixCustomOpcodes(mainChunk);
			solver.AddFingerprint(mainChunk);
		}

		if (session.IsDebug)
		{
			var information = PrintFunctionDebug(mainChunk);
			File.WriteAllText("deobfuscated-ib2-bytecode-listing.lua", information);
		}

		// Serialize the output to .luac
		var serialized = new Serializer(mainChunk).Serialize();
		
		if (session.IsDebug)
		{
			var information = PrintFunctionDebug(mainChunk);
			File.WriteAllText("lua51-bytecode-listing.lua", information);
		}
		

		if (session.IsDebug)
			File.WriteAllText("debug-dumper.lua", session.RewrittenScript!.NormalizeWhitespace().ToFullString());
		
		
		
		return serialized;


		// Functions
		static string PrintFunctionDebug(Chunk chunk)
		{
			var chunkStr = "------------";
			foreach (var t in chunk.Functions)
			{
				chunkStr += PrintFunctionDebug(t);
			}

			Console.WriteLine(chunk.ToString());

			chunkStr += chunk.ToString();
			chunkStr = chunkStr.Replace("\n\n", "\n");

			return chunkStr + "\n\n";
		}
		
    }
    
}