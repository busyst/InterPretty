using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static class IS
{
    public static readonly string Mode = "16";
}
class Interpreter(Class[] _classes)
{
    public static bool bootloader = false;

    private Queue<Instruction> instructionBuffer = [];
    private readonly StringBuilder main = new StringBuilder();

    private readonly Dictionary<string,Variable> data = [];
    private Class GetClass(string name)
    {
        var _class = _classes.FirstOrDefault((x)=>x.name==name);
        if(_class!=null)
            return _class;
        throw new ArgumentException($"Class \"{name}\" dont exist");
    }
    private Variable GetVariable(string className,string name)
    {
        var _name = $"{className}_{name}";
        if(data.ContainsKey(_name))
            return data[_name];
        throw new ArgumentException($"Class \"{name}\" dont exist");
    }
    private static bool isNumber(string s) =>char.IsNumber(s[0]);
    private void AddLine(string str) => main.AppendLine('\t' + str);

    private void HandleCreation(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var type = Variable.VTFromString(instruction.args[1]);
        var name = callingFrom +'_'+ instruction.args[2];
        data.Add(name,new Variable(){name = name,type = type});
    }
    private void HandleOperation(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var callingTo = instruction.args[1];
        var name = instruction.args[2];
        var expresion = instruction.args.AsSpan(3);
        var variable = GetVariable(callingTo,name);
        switch (expresion.Length)
        {
            case 1:
                if(isNumber(expresion[0]))
                    AddLine($"mov {variable.StringType} [{variable.name}], {expresion[0]}");
                else
                {
                    var scv = GetVariable(callingFrom,expresion[0]);
                    var mxr = Variable.VTToPrimaryRegister(Variable.MaxReg(scv.type,variable.type));
                    AddLine($"mov {scv.StringType} {mxr}, [{scv.name}]");
                    AddLine($"mov {variable.StringType} [{variable.name}],{variable.StringType} {mxr}");
                }
                break;
        }

        System.Console.WriteLine();

    }
    private void HandleCall(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var callingTo = instruction.args[1];
        var name = instruction.args[2];
        var instructions = GetClass(callingTo).functions.First((x)=>x.name==name).instructions;
        foreach (var x in instructions)
            instructionBuffer.Enqueue(x);
    }
    private void HandleCJP(Instruction instruction)
    {
        string relClass = instruction.args[0];
        string name = instruction.args[1];
        AddLine($"{relClass}_C_{name}:");
    }
    private void HandleJP(Instruction instruction)
    {
        string relClass = instruction.args[0];
        string name = instruction.args[1];
        AddLine($"jmp {relClass}_C_{name}");
    }
    private void HandleCondition(Instruction instruction)
    {
        var relClass = instruction.args[0];
        var jp = $"{relClass}_C_{instruction.args[1]}";
        var expr = instruction.args.AsSpan(2);

        (string operand, VariableType type) GetOperandInfo(string relClass, string expression)
        {
            if (!isNumber(expression))
            {
                var variable = GetVariable(relClass, expression);
                return ($"{variable.StringType} [{variable.name}]", variable.type);
            }
            else
            {
                return (expression, VariableType.INT);
            }
        }
        var (fNv, first) = GetOperandInfo(relClass, expr[0]);
        var (sNv, second) = GetOperandInfo(relClass, expr[2]);

        var max = Variable.MaxReg(first, second);
        var rega = Variable.VTToPrimaryRegister(max);
        var regb = Variable.VTToSecondaryRegister(max);
        var tetf = (max == first) ? "mov" : "movzx";
        var tets = (max == second) ? "mov" : "movzx";

        AddLine($"{tetf} {rega},{fNv}");
        AddLine($"{tets} {regb},{sNv}");
        AddLine($"cmp {rega},{regb}");

        switch (expr[1])
        {
            case "<":  AddLine($"jge {jp}"); break;
            case ">":  AddLine($"jle {jp}"); break;
            case "<=": AddLine($"jg {jp}");  break;
            case ">=": AddLine($"jl {jp}");  break;
            case "==": AddLine($"jne {jp}"); break;
            case "!=": AddLine($"je {jp}");  break;
        }
    }


    public string Interpret()
    {
        var c = GetMain();
        List<Instruction> instructions = [];
        foreach (var x in _classes)
            instructions.AddRange(x.functions.First((y)=>y.name==x.name).instructions);
        instructions.AddRange(c.instructions);
        instructionBuffer = new Queue<Instruction>(instructions);
        while(instructionBuffer.Count!=0)
        {
            var instruction = instructionBuffer.Dequeue();
            switch (instruction.instructionType)
            {
                case InstructionType.CREATE_VARIABLE:
                    HandleCreation(instruction);
                    break;
                case InstructionType.OPERATION:
                    HandleOperation(instruction);
                    break;
                case InstructionType.CALL_FUNCTION:
                    HandleCall(instruction);
                    break;
                case InstructionType.CREATE_JP:
                    HandleCJP(instruction);
                    break;
                case InstructionType.JUMP_TO:
                    HandleJP(instruction);
                    break;
                case InstructionType.CONDITION:
                    HandleCondition(instruction);
                    break;
                default:
                    throw new Exception("Wrong instruction!");
            }
        }
        return GetCode();
    }
    private Function GetMain()
    {
        foreach (var _class in _classes)
            foreach (var x in _class.functions)
                if(x.name.Equals("main", StringComparison.CurrentCultureIgnoreCase))
                    return x;
        throw new Exception("There is no main method!");
    }
    private string GetCode()
    {
        StringBuilder outputBuilder = new StringBuilder();

        AppendIfTrue(outputBuilder, bootloader, "[org 0x7C00]\n");
        AppendIfTrue(outputBuilder, true, $"[BITS {IS.Mode}]\n");

        outputBuilder.AppendLine("section .data");

        foreach (var entry in data)
        {
            outputBuilder.AppendLine($"  {entry.Key}:{entry.Value.StringShortType} 0");
        }

        outputBuilder.AppendLine("section .text");
        outputBuilder.AppendLine("global _start");
        outputBuilder.AppendLine("_start:");
        outputBuilder.Append(main);

        return outputBuilder.ToString();
    }

    private static void AppendIfTrue(StringBuilder builder, bool condition, string value)
    {
        if (condition)
        {
            builder.Append(value);
        }
    }

}