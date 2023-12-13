class Function
{
    public readonly string name = "";
    public readonly Dictionary<string,Variable> vars = [];
    public readonly Dictionary<string,ParseFunction> functions = [];
    public readonly Instruction[] instructions = [];
    public Function(string name, Variable[] inputTypes, ParseFunction[] functions, Instruction[] instructions)
    {
        this.name = name;
        Dictionary<string,Variable> vars = [];
        foreach (var x in inputTypes)
            vars.Add(x.name,x);
        
        Dictionary<string,ParseFunction> funcs = [];
        foreach (var x in functions)
            funcs.Add(x.name,x);
        
        this.vars = vars;
        this.functions = funcs;
        this.instructions = instructions;
    }
}
class ParseFunction
{
    public string name = "";
    public List<Variable> inputTypes = [];
    public List<ParseFunction> functions = [];
    public List<Instruction> instructions = [];
    public Function ToFunc()=>new Function(name,inputTypes.ToArray(),functions.ToArray(),instructions.ToArray());
}
public class Variable
{
    public string name;
    public VariableType type;
    public string StringType => VTToType(type);
    public string StringShortType => VTToShortType(type);
    
    public static VariableType VTFromString(string str) => str switch
    {
        "byte"=>VariableType.BYTE,
        "short"=>VariableType.SHORT,
        "int"=>VariableType.INT,
        "long"=>VariableType.LONG,
        _ => throw new ArgumentException(),
    };
    public static string VTToType(VariableType variableType) => variableType switch
    {
        VariableType.BYTE => "byte",
        VariableType.SHORT => "word",
        VariableType.INT => "dword",
        VariableType.LONG => "qword",
        _ => throw new ArgumentException("Invalid variableType"),
    };
    public static string VTToShortType(VariableType variableType) => variableType switch
    {
        VariableType.BYTE => "db",
        VariableType.SHORT => "dw",
        VariableType.INT => "dd",
        VariableType.LONG => "dq",
        _ => throw new ArgumentException("Invalid variableType"),
    };
}
public enum VariableType
{
    BYTE,SHORT,INT,LONG
}