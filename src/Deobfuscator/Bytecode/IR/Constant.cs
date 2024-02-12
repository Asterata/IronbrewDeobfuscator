namespace IronbrewDeobfuscator.Deobfuscator.Bytecode.IR;

public class Constant
{
    
    public ConstantType Type;
    public dynamic Data;
    
    public Constant(dynamic data)
    {
        Data = data;
        switch (data)
        {
            case string:
                Type = ConstantType.String;
                break;
            case int:
            case double:
                Type = ConstantType.Number;
                break;
            case bool:
                Type = ConstantType.Boolean;
                break;
            case long:
                Type = ConstantType.Number;
                break;
            case null:
                Type = ConstantType.Nil;
                break;
            default:
                throw new Exception("Invalid constant type");
        }
    }
    
    public override string ToString()
    {
        return $"{Data.ToString()} ({Type})";
    }
}