using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

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
    private bool isVariable(string className,string name)
    {
        var _name = $"{className}_{name}";
        if(data.ContainsKey(_name))
            return true;
        return false;
    }
    private static bool isNumber(string s) =>char.IsNumber(s[0]);
    private void AddLine(string str) => main.AppendLine('\t' + str);

    private void HandleCreation(Instruction instruction)
    {
        var callingFrom = instruction.args[0];
        var type = Variable.VTFromString(instruction.args[1]);
        var name = callingFrom +'_'+ instruction.args[2];
        data.Add(name,new Variable(){name = name,type = type,array = instruction.args[3]=="1"});
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
            default:
                if(expresion[0].Length==0)
                {
                    if(!variable.array)
                        throw new Exception($"You tried writing a array to \"{variable.name}\".");
                    if(variable.changed)
                        throw new Exception($"Arrays are static, dont change them.");
                    // a = {...}
                    var vals = expresion[1..^1];
                    string def = "";
                    for (int i = 0; i < vals.Length; i++)
                    {
                        def+=vals[i];
                    }
                    variable.defaultValue = def;
                }
                else
                {
                    // a = q - a;
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
    private void HandleDirect(Instruction instruction)
    {
        string className = instruction.args[0];
        string code = instruction.args[1].Trim();
        string leadingWhitespacePattern = @"^[ \t]+";
        string emptyStringReplacement = "";
        RegexOptions multilineOptions = RegexOptions.Multiline;

        // Remove leading whitespaces
        string cleanedCode = Regex.Replace(code, leadingWhitespacePattern, emptyStringReplacement, multilineOptions);

        var lines = cleanedCode.Split("\r\n");

        void ProcessLine(int i)
        {
            string ModifyTextInBraces(string input)
            {
                // Define a regular expression pattern to match text within curly braces
                string pattern = @"\{([^}]*)\}";

                // Use Regex.Matches to find all matches in the input string
                MatchCollection matches = Regex.Matches(input, pattern);

                // Loop through each match and replace the text within curly braces
                foreach (Match match in matches.Cast<Match>())
                {
                    string originalText = match.Value; // Text within curly braces
                    string modifiedText = ModifyText(originalText); // Modify the text as needed
                    input = input.Replace(originalText, modifiedText); // Replace original text with modified text
                }

                return input;
            }
            string ModifyText(string originalText)
            {
                var text = originalText[1..^1].Trim();
                var shieeet = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var output = "";
                for (int i1 = 0; i1 < shieeet.Length; i1++)
                {
                    string x = shieeet[i1];
                    if (isVariable(className,x))
                        output +=  GetVariable(className,x).name + (i1==shieeet.Length-1?"":" ");
                    else
                        output += x + (i1==shieeet.Length-1?"":" ");
                }
                return output;
            }
            lines[i] = ModifyTextInBraces(lines[i]);
        }

        // Process each line
        for (int i = 0; i < lines.Length; i++)
            ProcessLine(i);

        // Add each processed line
        foreach (var line in lines)
            AddLine(line);
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
                case InstructionType.DIRECT_CODE:
                    HandleDirect(instruction);
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

        AppendIfTrue(outputBuilder,data.Count!=0,"section .data\n");

        foreach (var entry in data)
        {
            outputBuilder.AppendLine($"  {entry.Key} {entry.Value.StringShortType} {entry.Value.defaultValue}");
        }

        outputBuilder.AppendLine("section .text");
        outputBuilder.AppendLine("  global _start");
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