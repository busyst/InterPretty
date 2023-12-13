public static class IS
{
    public static readonly string Mode = "16";
    public static readonly string PrimaryRegister = "ax";
    public static readonly string ScratchRegister = "bx";

    // Arithmetic instructions
    public static string AddInstruction() => $"add {PrimaryRegister},{ScratchRegister}";
    public static string SubInstruction() => $"sub {PrimaryRegister},{ScratchRegister}";
    public static string MulInstruction() => $"imul {PrimaryRegister},{ScratchRegister}";
    public static string DivInstruction() => $"div {ScratchRegister}";

    // Comparison instructions
    public static string CmpInstruction(string operand1, string operand2) => $"cmp {operand1},{operand2}";
    public static string JmpInstruction(string label) => $"jmp {label}";
    public static string JeInstruction(string label) => $"je {label}";
    public static string JneInstruction(string label) => $"jne {label}";
    public static string JgInstruction(string label) => $"jg {label}";
    public static string JgeInstruction(string label) => $"jge {label}";
    public static string JlInstruction(string label) => $"jl {label}";
    public static string JleInstruction(string label) => $"jle {label}";

    // Bitwise instructions
    public static string AndInstruction(string operand1, string operand2) => $"and {operand1},{operand2}";
    public static string OrInstruction(string operand1, string operand2) => $"or {operand1},{operand2}";
    public static string XorInstruction(string operand1, string operand2) => $"xor {operand1},{operand2}";
    public static string NotInstruction(string operand) => $"not {operand}";

    // Shift instructions
    public static string LeftShiftInstruction(string operand, int count) => $"shl {operand},{count}";
    public static string RightShiftInstruction(string operand, int count) => $"shr {operand},{count}";

    // Other instructions
    public static string CallInstruction(string functionName) => $"call {functionName}";
    public static string RetInstruction() => "ret";
    
    // Generate assignment instruction
    public static string Assign(string destination, string source) => $"mov {destination},{source}";
    public static string Assign(Variable destination, string source) => $"mov {(destination.StringType)} [{destination.name}],{source}";
}




class Interpreter(Function function)
{
    private List<Instruction> instructions = function.instructions.ToList(); 
    public static bool bootloader = false;
    public int counter = 0;
    public string output = string.Empty;
    private Dictionary<string,Variable> data = [];
    private string main = string.Empty;
    public void AddLine(string str) => main += str + '\n';
    private static string GetVarOrConst(Token t)
    {
        if(t.type == TokenType.NUMBER)
            return t.lexeme;
        if(char.IsDigit(t.lexeme[0]))
            return t.lexeme;
        else
            return $"[{t.lexeme}]";
    }
    
    private void HandleCreation(Instruction instruction)
    {
        var type = Variable.VTFromString(instruction.args[0].lexeme);
        var name = instruction.args[1].lexeme;
        data.Add(name,new Variable(){name = name,type = type});
    }
    private void HandleOperation(Instruction instruction)
    {
        var name = instruction.args[0].lexeme;
        List<Token> ttp = [];
        for (int i = 1; i < instruction.args.Count; i++)
            ttp.Add(instruction.args[i]);
        if(ttp.Count == 1){
            AddLine(IS.Assign(data[name], GetVarOrConst(ttp[0])));
        }
        else
        {
            // Do RPN here
            RPN(ttp,data[name]);
        }
        
    }
    void RPN(List<Token> tokens,Variable variable)
    {
        List<Token> Result = [];
        Stack<Token> stack = [];
        static bool isOperation(Token token) => token.type == TokenType.PLUS || token.type == TokenType.MINUS || token.type == TokenType.MULTIPLY || token.type == TokenType.DIVIDE || (token.type == TokenType.SPECIAL_SYMBOL && (token.lexeme[0] == '&' || token.lexeme[0] == '|'|| token.lexeme[0] == '^'));
        for (int i = 0; i < tokens.Count; i++)
        {
            if(isOperation(tokens[i]))
                stack.Push(tokens[i]);
            else if(tokens[i].type == TokenType.OPEN_PAREN)
                stack.Push(tokens[i]);
            else if(tokens[i].type == TokenType.CLOSE_PAREN)
            {
                while (true)
                {
                    var token = stack.Pop();
                    if(token.type == TokenType.OPEN_PAREN)
                        break;
                    Result.Add(token);
                }
            }
            else
                Result.Add(tokens[i]);
        }
        while (stack.Count!=0)
        {
            Result.Add(stack.Pop());
        }
        foreach (var x in Result)
            if(!isOperation(x))
                AddLine($"push word {GetVarOrConst(x)}");
            else
            {
                var operation = x;
                AddLine($"pop {IS.ScratchRegister}");
                AddLine($"pop {IS.PrimaryRegister}");
                switch (operation.type)
                {
                    case TokenType.PLUS:
                        AddLine(IS.AddInstruction());
                        break;
                    case TokenType.MINUS:
                        AddLine(IS.SubInstruction());
                        break;
                    case TokenType.MULTIPLY:
                        AddLine(IS.MulInstruction());
                        break;
                    case TokenType.DIVIDE:
                        AddLine(IS.DivInstruction());
                        break;
                }
            }
        AddLine(IS.Assign($"[{variable.name}]",IS.PrimaryRegister));
    }
    private void HandleCondition(Instruction instruction,int to)
    {
        var labelName = instruction.args[0].lexeme;
        var first = instruction.args[1];
        var operation = instruction.args[2].lexeme;
        var second = instruction.args[3];


        AddLine(IS.Assign(IS.PrimaryRegister,GetVarOrConst(first)));
        AddLine(IS.Assign(IS.ScratchRegister,GetVarOrConst(second)));
        AddLine(IS.CmpInstruction(IS.PrimaryRegister,IS.ScratchRegister));
        switch (operation[0])
        {
            case '>':
                AddLine($"jle {labelName}");
                break;
            case '<':
                AddLine($"jge {labelName}");
                break;
        }
    }
    void HandleFunction(Instruction instruction,int poi)
    {
        var args = instruction.args;
        var first = args[0].lexeme;
        string arg = string.Empty; 
        for (int i = 1; i < args.Count; i++)
        {
            if(data.ContainsKey(args[i].lexeme))
                arg+=$"[{args[i].lexeme}]";
            else if(args[i].type == TokenType.MODIFIER)
            {
                throw new NotImplementedException();
                //arg+="word ";
                //continue;
            }
            else
                arg+=args[i].lexeme;
            
            if(i!=args.Count-1)
                arg+=", ";
        }
        switch (first)
        {
            case "cld":
                AddLine($"cld");
                break;
            case "inter":
                AddLine($"int {arg}");
                break;
            case "mov":
                AddLine($"mov {arg}");
                break;
            case "jmp":
                if(args.Count ==2)
                    AddLine($"jmp {args[1].lexeme}");
                else
                    AddLine($"jmp {args[1].lexeme}:{args[3].lexeme}");
                    
                break;
            default:
                if(function.functions.TryGetValue(first, out ParseFunction value))
                {
                    instructions.InsertRange(poi+1,value.instructions);
                }
                break;
        }
    }
    int HandleNewFunction(Instruction instruction,int pos)
    {
        var args = instruction.args;
        var name = args[1].lexeme;
        var To = int.Parse(args[0].lexeme);
        Instruction[] _loc  = new Instruction[To-pos-1];
        for (int i = pos+1; i < To; i++)
        {
            _loc[i-pos-1] = function.instructions[i];
        }
        //functions.Add(name,new Function(){instructions = _loc});

        return To-1;
    }
    public void Interpret()
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            switch (instructions[i].instructionType)
            {
                case InstructionType.NOOP:
                    AddLine("noop");
                    break;
                case InstructionType.CREATE_VARIABLE:
                    HandleCreation(instructions[i]);
                    break;
                case InstructionType.OPERATION:
                    HandleOperation(instructions[i]);
                    break;
                case InstructionType.CONDITION:
                    HandleCondition(instructions[i],i);
                    break;
                case InstructionType.CALL_FUNCTION:
                    HandleFunction(instructions[i],i);
                    break;
                case InstructionType.FUNCTION:
                    i = HandleNewFunction(instructions[i],i);
                    break;
                case InstructionType.CREATE_JP:
                    AddLine(instructions[i].args[0].lexeme+':');
                    break;
                case InstructionType.JUMP_TO:
                    AddLine("jmp "+instructions[i].args[0].lexeme);
                    break;
                default:
                    break;
            }
        }
        output+= bootloader?"[org 0x7C00]\n":"";
        output+= bootloader?$"[BITS {IS.Mode}]\n":"";
        output+= "section .data\n";
        var emn = data.AsEnumerable().ToArray();
        for (int i = 0; i < data.Count; i++)
        {
            output+= $"  {emn[i].Key} {emn[i].Value.StringShortType} 0\n";
        }
        output+= "section .text\n";
        output+= "global _start\n";
        output+= "_start:\n";
        output+= main+'\n';
        //output+= bootloader?"jmp $\n":"";
        output+= bootloader?"times 510-($-$$) db 0\n":"";
        output+= bootloader?"dw 0xAA55\n":"";
    }

}