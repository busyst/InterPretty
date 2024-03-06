public static class Variable
{
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
    public static string VTToPrimaryRegister(VariableType variableType) => variableType switch
    {
        VariableType.BYTE => "al",
        VariableType.SHORT => "ax",
        VariableType.INT => "eax",
        VariableType.LONG => "rax",
        _ => throw new ArgumentException("Invalid variableType"),
    };
    public static string VTToSecondaryRegister(VariableType variableType) => variableType switch
    {
        VariableType.BYTE => "dl",
        VariableType.SHORT => "dx",
        VariableType.INT => "edx",
        VariableType.LONG => "rdx",
        _ => throw new ArgumentException("Invalid variableType"),
    };
    public static VariableType MaxReg(VariableType va1,VariableType va2) => (VariableType)Math.Max((byte)va1,(byte)va2);
}
public enum VariableType : byte
{
    BYTE = 1,SHORT = 2,INT = 4,LONG = 8
}