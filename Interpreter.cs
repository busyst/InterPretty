using System.Text;
using System.Text.RegularExpressions;
class Interpreter(Class[] _classes)
{
    public static bool bootloader = false;
    private readonly StringBuilder main = new StringBuilder();
    public string Interpret()
    {
        var c = GetMain();
        FunctionInterpreter functionInterpreter = new("_start",c.instructions);
        functionInterpreter.Parse();
        main.Append(functionInterpreter.GetCode());
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
        static void AppendIfTrue(StringBuilder builder, bool condition, string value)   
        {
            if (condition)
            {
                builder.Append(value);
            }
        }
        StringBuilder outputBuilder = new StringBuilder();

        AppendIfTrue(outputBuilder, bootloader, "[org 0x7C00]\n");
        AppendIfTrue(outputBuilder, true, $"[BITS 16]\n");

        outputBuilder.AppendLine("section .text");
        outputBuilder.AppendLine("  global _start");
        outputBuilder.Append(main);

        return outputBuilder.ToString();
    }
}
class FunctionInterpreter(string name,IEnumerable<Instruction> instructions)
{
    private ASMCOMM asmContext = new();
    //16 bit registers
    private readonly StringBuilder main = new StringBuilder();
    private void AddLine(string str) => main.AppendLine('\t' + str);
    private void HandleCreation(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var type = Variable.VTFromString(instruction.args[1]);
        var name = callingFrom +'_'+ instruction.args[2];
        asmContext.AllocValue(name,type);
    }
    private void HandleOperation(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var callingTo = instruction.args[1];
        var _name = instruction.args[2];
        var expresion = instruction.args.AsSpan(3);

        var name = callingFrom +'_'+ _name;
        if(expresion.Length==1)
        {
            if(char.IsNumber(expresion[0][0]))
                AddLine(asmContext.CopyValue(name,expresion[0]));
            else
            {
                var op_name = callingTo +'_'+expresion[0];
                AddLine(asmContext.CopyValue(name,op_name));
            }
        }
        else if(expresion.Length==3)
        {
            string fo = char.IsNumber(expresion[0][0])?expresion[0]:callingFrom +'_'+expresion[0];
            bool bfo = char.IsNumber(expresion[0][0]);
            string so = char.IsNumber(expresion[2][0])?expresion[2]:callingFrom +'_'+expresion[2];
            bool bso = char.IsNumber(expresion[2][0]);
            if (!bfo)
            {
                fo = asmContext.LocalValue(fo);
            }
            AddLine($"mov ax, {fo}");
            if(!bso)
            {
                so = asmContext.LocalValue(so);
                AddLine($"mov dx, {so}");
                so = "dx";
            }
            switch (expresion[1][0])
            {
                case '+':
                    AddLine($"add ax, {so}");
                    break;
                case '-':
                    AddLine($"sub ax, {so}");
                    break;
                case '*':
                    AddLine($"imul ax, {so}");
                    break;
                default:
                    break;
            }
            AddLine($"mov {fo}, ax");
        }
    }
    private void HandleCondition(Instruction instruction)
    {
        var relClass = instruction.args[0];
        var jp = $"{relClass}_C_{instruction.args[1]}";
        var expr = instruction.args.AsSpan(2);
        var op_name = relClass + '_' + expr[0];
        
        AddLine($"cmp word [bp-{asmContext.offsets[op_name]}],{expr[2]}");
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
    private void HandleDirect(Instruction instruction)
    {
        string className = instruction.args[0];
        string code = instruction.args[1].Trim();
        string regex = @"\{(.*?)\}";
        var matches = Regex.Matches(code,regex);
        foreach (Match x in matches)
        {
            string cap = (x).Value;
            string val = (x).Value.AsSpan(1,x.Value.Length-2).ToString();
            if (char.IsNumber(val[0]))
            {
                int startIndex = x.Index;
                int length = x.Length;
                code = code.Remove(startIndex, length).Insert(startIndex, val);
            }
            else
            {
                var name = className +'_'+ val;
                int startIndex = x.Index;
                int length = x.Length;
                code = code.Remove(startIndex, length).Insert(startIndex, asmContext.LocalValue(name));
            }

        }
        string[] lines = code.Split('\n');
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();
            string tabbedLine = (i!=0?"\t":"") + trimmedLine;
            if(i+1!=lines.Length)
                sb.AppendLine(tabbedLine);
            else
                sb.Append(tabbedLine);
        }
        AddLine(sb.ToString());
        System.Console.WriteLine();
    }
    public void Parse()
    {
        Queue<Instruction> instructionBuffer;
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
                case InstructionType.CONDITION:
                    HandleCondition(instruction);
                    break;
                case InstructionType.CREATE_JP:
                    HandleCJP(instruction);
                    break;
                case InstructionType.DIRECT_CODE:
                    HandleDirect(instruction);
                    break;
                default:
                    throw new NotImplementedException("Wrong instruction!");
            }
        }
    }
    private void HandleCJP(Instruction instruction)
    {
        string relClass = instruction.args[0];
        string name = instruction.args[1];
        AddLine($"{relClass}_C_{name}:");
    }

    public string GetCode()
    {
        StringBuilder outputBuilder = new StringBuilder();
        outputBuilder.AppendLine($"{name}:");
        outputBuilder.AppendLine("\tpush bp");
        outputBuilder.Append(main);
        outputBuilder.AppendLine("\tpop bp");
        outputBuilder.AppendLine("\tret");
        return outputBuilder.ToString();
    }
}
public class ASMCOMM
{
    private int offset = 0;
    public Dictionary<string,(int size,VariableType type)> offsets = [];
    public void AllocValue(string name, VariableType type)
    {
        offset+= (int)type;
        offsets.Add(name,(offset,type));
    }
    public string LocalValue(string name)
    {
        if(offsets.TryGetValue(name, out (int size, VariableType type) value))
            return $"{Variable.VTToType(value.type)} [bp-{value.size}]";
        else
            throw new ArgumentException();
    }
    public string CopyValue(string to,string what)
    {
        if(offsets.ContainsKey(to))
        {
            if(offsets.ContainsKey(what))
                return $"mov ax, {LocalValue(what)}"+"\t"+$"mov {LocalValue(to)},ax";
            else
                return $"mov {LocalValue(to)},{what}";
        }

        throw new Exception("Wha?");
    }
}


public class Register(string name){public string Name => name;}