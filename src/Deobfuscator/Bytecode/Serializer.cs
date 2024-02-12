using System.Text;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;
using IronbrewDeobfuscator.Deobfuscator.Bytecode.IR.Opcodes;

namespace IronbrewDeobfuscator.Deobfuscator.Bytecode;


public class Serializer(Chunk mainChunk)
{
	private readonly Encoding _luaEncoding = Encoding.GetEncoding(28591);
	private readonly List<byte> _res = [];
	public byte[] Serialize()
	{

		void WriteByte(byte b) => _res.Add(b);

		void WriteBytes(IEnumerable<byte> bs) => _res.AddRange(bs);
		
		void WriteInt(int i) => WriteBytes(BitConverter.GetBytes(i));

		void WriteUInt(uint i) => WriteBytes(BitConverter.GetBytes(i));

		void WriteNum(double d) => WriteBytes(BitConverter.GetBytes(d));
				
		void WriteString(string str)
		{
			var bytes = _luaEncoding.GetBytes(str);
				
			WriteInt(bytes.Length + 1);
			WriteBytes(bytes);
			WriteByte(0);
		}

		void WriteChunk(Chunk chunk)
		{
			if (chunk._functionId != "")
				WriteString(chunk._functionId);
			else
				WriteInt(0);

			WriteInt(0);
			WriteInt(0);
			WriteByte(chunk.UpvalueCount);
			WriteByte(chunk.ParameterCount);
			WriteByte(chunk.VarargFlag);
			WriteByte(chunk.StackSize);
				
			chunk.UpdateMappings();
				
			WriteInt(chunk.Instructions.Count);
			foreach (var i in chunk.Instructions)
			{
				i.Ib2UpdateRegisters();
				i.TurnIntoLua51();
					
				ref var a = ref i.A;
				ref var b = ref i.B;
				ref var c = ref i.C;

				uint result = 0;

				result |= (uint) i.Opcode;
				result |= ((uint)a << 6);
				
				i.InstructionType = InstructionMappings[i.Opcode];

				switch (i.InstructionType)
				{
					case InstructionType.ABx:
						result |= ((uint)b << (6 + 8));
						break;
						
					case InstructionType.AsBx:
						b += 131071;
						result |= ((uint)b << (6 + 8));
						break;
						
					case InstructionType.ABC:
						result |= ((uint)c << (6     + 8));
						result |= ((uint)b << (6 + 8 + 9));
						break;
				}

				WriteUInt(result);
			}

			WriteInt(chunk.Constants.Count);
			foreach (var constant in chunk.Constants)
			{
				switch (constant.Type)
				{
					case ConstantType.Nil:
						WriteByte(0);
						break;
							
					case ConstantType.Boolean:
						WriteByte(1);
						WriteByte((byte) ((bool) constant.Data ? 1 : 0));
						break;
						
					case ConstantType.Number:
						WriteByte(3);
						WriteNum(constant.Data);
						break;
						
					case ConstantType.String:
						WriteByte(4);
						WriteString(constant.Data);
						break;
				}
			}
				
			WriteInt(chunk.Functions.Count);
			foreach (var sChunk in chunk.Functions)
				WriteChunk(sChunk);
				
			WriteInt(0);
			WriteInt(0);
			WriteInt(0);
			
		}
			
		WriteByte(27);
		WriteBytes(_luaEncoding.GetBytes("Lua"));
		WriteByte(0x51);
		WriteByte(0);
		WriteByte(1);
		WriteByte(4);
		WriteByte(4);
		WriteByte(4);
		WriteByte(8);
		WriteByte(0);

		WriteChunk(mainChunk);

		return _res.ToArray(); 
	}

	private static readonly Dictionary<Opcode, InstructionType> InstructionMappings = new()
	{
		{ Opcode.Move, InstructionType.ABC },
		{ Opcode.LoadK, InstructionType.ABx },
		{ Opcode.LoadBool, InstructionType.ABC },
		{ Opcode.LoadNil, InstructionType.ABC },
		{ Opcode.GetUpval, InstructionType.ABC },
		{ Opcode.GetGlobal, InstructionType.ABx },
		{ Opcode.GetTable, InstructionType.ABC },
		{ Opcode.SetGlobal, InstructionType.ABx },
		{ Opcode.SetUpval, InstructionType.ABC },
		{ Opcode.SetTable, InstructionType.ABC },
		{ Opcode.NewTable, InstructionType.ABC },
		{ Opcode.Self, InstructionType.ABC },
		{ Opcode.Add, InstructionType.ABC },
		{ Opcode.Sub, InstructionType.ABC },
		{ Opcode.Mul, InstructionType.ABC },
		{ Opcode.Div, InstructionType.ABC },
		{ Opcode.Mod, InstructionType.ABC },
		{ Opcode.Pow, InstructionType.ABC },
		{ Opcode.Unm, InstructionType.ABC },
		{ Opcode.Not, InstructionType.ABC },
		{ Opcode.Len, InstructionType.ABC },
		{ Opcode.Concat, InstructionType.ABC },
		{ Opcode.Jmp, InstructionType.AsBx },
		{ Opcode.Eq, InstructionType.ABC },
		{ Opcode.Lt, InstructionType.ABC },
		{ Opcode.Le, InstructionType.ABC },
		{ Opcode.Test, InstructionType.ABC },
		{ Opcode.TestSet, InstructionType.ABC },
		{ Opcode.Call, InstructionType.ABC },
		{ Opcode.TailCall, InstructionType.ABC },
		{ Opcode.Return, InstructionType.ABC },
		{ Opcode.ForLoop, InstructionType.AsBx },
		{ Opcode.ForPrep, InstructionType.AsBx },
		{ Opcode.TForLoop, InstructionType.ABC },
		{ Opcode.SetList, InstructionType.ABC },
		{ Opcode.Close, InstructionType.ABC },
		{ Opcode.Closure, InstructionType.ABx },
		{ Opcode.Vararg, InstructionType.ABC }
	};
}