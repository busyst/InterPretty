class Instruction
{
    public VariableType extra = VariableType.BYTE;
    public InstructionType instructionType = InstructionType.NOOP;
    public string[] args;
    public static Instruction CallFunction(string callingFrom,string callingTo,string name,List<string> args)
    {
        var ins = new Instruction(){instructionType = InstructionType.CALL_FUNCTION,args = [callingFrom,callingTo,name]};
        for (int i = 0; i < args.Count; i++)
        {
            ins.args = ins.args.Append(args[i]).ToArray();
        }
        return ins;
    }
    public static Instruction CreateVariable(string callingFrom,string type,string name,string array) =>
    new Instruction(){instructionType = InstructionType.CREATE_VARIABLE,args = [callingFrom,type,name,array]};
    public static Instruction AssignToVariable(string callingFrom,string callingTo,string name,List<string> expresion)
    {
        var ins = new Instruction(){instructionType = InstructionType.OPERATION,args = [callingFrom,callingTo,name]};
        for (int i = 0; i < expresion.Count; i++)
        {
            ins.args = ins.args.Append(expresion[i]).ToArray();
        }
        return ins;
    }
    public static Instruction Condition(string callingFrom,string jp,List<string> expresion)
    {
        var ins = new Instruction(){instructionType = InstructionType.CONDITION,args = [callingFrom,jp]};
        for (int i = 0; i < expresion.Count; i++)
        {
            ins.args = ins.args.Append(expresion[i]).ToArray();
        }
        return ins;
    }
    public static Instruction CreateJP(string _class,string name) =>
    new Instruction(){instructionType = InstructionType.CREATE_JP,args = [_class,name]};
    public static Instruction GoTo(string _class,string name) =>
    new Instruction(){instructionType = InstructionType.JUMP_TO,args = [_class,name]};
}
enum InstructionType{
    NOOP,
    CREATE_VARIABLE,
    OPERATION,
    CALL_FUNCTION,
    CREATE_JP,
    CONDITION,
    JUMP_TO,
    DIRECT_CODE,
};