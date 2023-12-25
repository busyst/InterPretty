class Parser(List<Token> tokens)
{
    private Token[] tokensArray;
    private List<ClassPointer> classesPointers = new List<ClassPointer>();
    public Class[] Parse()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if(tokens[i].type == TokenType.OPEN_PAREN&&tokens[i+1].type == TokenType.NAME&&tokens[i+2].type == TokenType.CLOSE_PAREN)
            {
                var type = tokens[i+1].lexeme;
                if(Variable.isType(type))
                {
                    tokens.RemoveRange(i,3);
                    tokens.Insert(i,new Token(){type = TokenType.MODIFIER,lexeme = type});
                }
            }
        }
        tokensArray = tokens.ToArray();

        int pointer = 0;
        while (pointer!=tokens.Count)
        {
            if(tokens[pointer].type == TokenType.NAME&&tokens[pointer].lexeme == "class"&&tokens[pointer].type == TokenType.NAME)
            {
                int start = pointer;
                int parens = 1;
                pointer += 3;
                while (true)
                {
                    if(tokens[pointer].type == TokenType.CLOSE_BRACE)
                    {
                        parens--;
                        if(parens == 0)
                            break;
                    }
                    if(tokens[pointer].type == TokenType.OPEN_BRACE)
                        parens++;
                    pointer++;
                }
                pointer++;
                classesPointers.Add(new ClassPointer(start,pointer,tokens[start+1].lexeme));
            }
        }
        foreach (var x in classesPointers)
            ParseClass(x);
        
        foreach (var x in classesPointers)
            foreach (var y in x.functionPointers)
                ParseFunction(x,y);
        
        Class[] classes = new Class[classesPointers.Count];
        for (int i = 0; i < classes.Length; i++)
        {
            Function[] functions = new Function[classesPointers[i].functionPointers.Count];
            for (int j = 0; j < functions.Length; j++)
            {
                functions[j] = new Function(classesPointers[i].functionPointers[j].name){instructions = classesPointers[i].functionPointers[j].instructions};
            }
            classes[i] = new Class(classesPointers[i].name){functions = functions};
        }
        return classes;
    }
    private void ParseClass(ClassPointer classPointer)
    {
        List<Instruction> initInstructions = [];
        var toks = tokensArray.AsSpan(classPointer.start,classPointer.end-classPointer.start);
        int pointer = 3;
        while (pointer!=toks.Length-1)
        {
            if(toks[pointer].type == TokenType.NAME&&toks[pointer].lexeme == "void")
            {
                int start = pointer;
                int parens = 1;
                pointer += 5;
                while (true)
                {
                    if(toks[pointer].type == TokenType.CLOSE_BRACE)
                    {
                        parens--;
                        if(parens == 0)
                            break;
                    }
                    if(toks[pointer].type == TokenType.OPEN_BRACE)
                        parens++;
                    pointer++;
                }
                pointer++;
                classPointer.functionPointers.Add(new FunctionPointer(start,pointer));
            }
            else if(toks[pointer].type == TokenType.NAME&&toks[pointer].lexeme != "void"&&toks[pointer+1].type== TokenType.NAME)
            {
                var cret = VarCreate(pointer,toks,toks[1].lexeme,ref pointer);
                initInstructions.Add(cret);
                pointer++;
            }
            else if(toks[pointer].type == TokenType.NAME&&toks[pointer+1].type == TokenType.EQUAL)
            {
                var cret = Assign(pointer,toks,toks[1].lexeme,ref pointer);
                initInstructions.Add(cret);
                pointer+=2;
            }
        }
        
        classPointer.functionPointers.Add(new FunctionPointer(0,0){name = "__init__",instructions = initInstructions.ToArray()});
    }
    private void ParseFunction(ClassPointer classPointer,FunctionPointer functionPointer)
    {
        int functionStart = classPointer.start + functionPointer.start;
        int functionEnd = classPointer.start + functionPointer.end-1;
        var name = tokens[functionStart+1].lexeme;
        string returnvalue = tokens[functionStart].lexeme;
        functionStart+=5;
        functionPointer.name = name;
        if(functionPointer.start!=0&&functionPointer.end!=0)
            functionPointer.instructions = Parse(tokensArray.AsSpan(functionStart,functionEnd-functionStart),classPointer.name).ToArray();
    }
    private readonly TokenType[][] posibilities =
    [
        ([TokenType.NAME,TokenType.OPEN_PAREN]),// a(...
        ([TokenType.NAME,TokenType.SPECIAL_SYMBOL,TokenType.NAME,TokenType.OPEN_PAREN]),// A.void a(...
        ([TokenType.NAME,TokenType.NAME,TokenType.SEMICOLON]),// int a;
        ([TokenType.NAME,TokenType.NAME,TokenType.EQUAL]),// int a = ...
        ([TokenType.NAME,TokenType.EQUAL]), // a =...
        ([TokenType.NAME,TokenType.COLON]),// a:
        ([TokenType.GOTO,TokenType.NAME,TokenType.SEMICOLON]),//goto a
        ([TokenType.IF,TokenType.OPEN_PAREN]),//if(...
        ([TokenType.NAME,TokenType.SPECIAL_SYMBOL,TokenType.NAME,TokenType.EQUAL]),        // A.a = ...
        ([TokenType.NAME,TokenType.OPEN_SQUARE_BRACE,TokenType.CLOSE_SQUARE_BRACE,TokenType.NAME,TokenType.EQUAL]),        // A.a = ...
    ];
    private List<Instruction> Parse(ReadOnlySpan<Token> tokens,string className)
    {
        List<Instruction> instructions = [];
        List<TokenType> buffer = [];
        for(int i =0;i<tokens.Length;i++)
        {
            var token = tokens[i];
            buffer.Add(token.type);
            for (int j = 0; j < posibilities.Length; j++)
            {
                bool skip = false;
                for (int g = 0; g < buffer.Count; g++)
                {
                    if(buffer[g]!=posibilities[j][g])
                    {
                        skip = true;
                        break;
                    }
                }
                if(skip)
                    continue;
                if(buffer.Count==posibilities[j].Length)
                {
                    if(j<=1)
                        instructions.Add(CallFunction(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j<=3)
                        instructions.Add(VarCreate(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==4)
                        instructions.Add(Assign(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==5)
                        instructions.Add(LabelCreation(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==6)
                        instructions.Add(GoTo(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==7)
                        instructions.AddRange(Condition(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==8)
                        instructions.Add(Assign(i-(buffer.Count-1),tokens,className,ref i));
                    else if(j==9)
                        instructions.Add(Assign(i-(buffer.Count-1),tokens,className,ref i));
                    buffer.Clear();
                }
                break;
            }
        }
        return instructions;
    }
    private static uint UID = 0;
    private List<Instruction> Condition(int start,ReadOnlySpan<Token> tokens,string className,ref int P)
    {
        var label = $"_IJ{UID++}";
        List<Instruction> instrs = new List<Instruction>();
        int parens = 1;
        int pointer = start+2;
        List<string> args = [];
        while (tokens[pointer].type!=TokenType.SEMICOLON)
        {
            if(tokens[pointer].type == TokenType.CLOSE_PAREN)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            else if(tokens[pointer].type == TokenType.OPEN_PAREN)
                parens++;

            args.Add(tokens[pointer].lexeme);
            pointer++;
        }
        instrs.Add(Instruction.Condition(className,label,args));
        List<Token> ttp = [];
        pointer++;
        if(tokens[pointer].type == TokenType.OPEN_BRACE)
        {
            parens = 1;
            pointer++;
            while (true)
            {
                if(tokens[pointer].type == TokenType.CLOSE_BRACE)
                {
                    parens--;
                    if(parens == 0)
                        break;
                }
                else if(tokens[pointer].type == TokenType.OPEN_BRACE)
                    parens++;

                ttp.Add(tokens[pointer]);
                pointer++;
            }
        }
        instrs.AddRange(Parse(ttp.ToArray(),className));
        instrs.Add(Instruction.CreateJP(className,label));

        return instrs;
        
    }
    private Instruction LabelCreation(int start,ReadOnlySpan<Token> tokens,string className,ref int P)=>Instruction.CreateJP(className,tokens[start].lexeme);
    private Instruction GoTo(int start,ReadOnlySpan<Token> tokens,string className,ref int P)=>Instruction.GoTo(className,tokens[start+1].lexeme);
    private Instruction CallFunction(int start,ReadOnlySpan<Token> tokens,string className,ref int P)
    {
        var _class = tokens[start+1].type==TokenType.SPECIAL_SYMBOL?tokens[start].lexeme:className;
        var name = tokens[start+1].type==TokenType.SPECIAL_SYMBOL?tokens[start+2].lexeme:tokens[start].lexeme;
        List<string> args = [];
        int parens = 1;
        int pointer = start + (tokens[start+1].type==TokenType.SPECIAL_SYMBOL?4:2);
        while (tokens[pointer].type!=TokenType.SEMICOLON)
        {
            if(tokens[pointer].type == TokenType.CLOSE_PAREN)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            else if(tokens[pointer].type == TokenType.OPEN_PAREN)
                parens++;
            if(tokens[pointer].type != TokenType.SPECIAL_SYMBOL)
                args.Add(tokens[pointer].lexeme);
            pointer++;
        }

        P = pointer + 1;
        var inst = Instruction.CallFunction(className,_class,name,args);
        return inst;
    }
    private Instruction VarCreate(int start,ReadOnlySpan<Token> tokens,string className,ref int P)
    {
        var type = tokens[start].lexeme;
        var name = tokens[start+1].lexeme;
        if(tokens[start+2].type != TokenType.SEMICOLON)
        {
            P = start;
        }
        return Instruction.CreateVariable(className,type,name);
    }
    private Instruction Assign(int start,ReadOnlySpan<Token> tokens,string className,ref int P)
    {
        if(tokens[start+1].type == TokenType.SPECIAL_SYMBOL) // A.a;
        {
            start+= 2;   
        }
        else if(tokens[start+1].type == TokenType.OPEN_SQUARE_BRACE) // int[] a;
        {
            start+=3;
        }
        var name = tokens[start].lexeme;
        int pointer = start + 2;
        List<string> expr = [];
        while (tokens[pointer].type!=TokenType.SEMICOLON)
        {
            expr.Add(tokens[pointer].lexeme);
            pointer++;
        }
        P+=expr.Count+1;
        if((start-1)>=0&&tokens[start-1].type == TokenType.SPECIAL_SYMBOL)
            return Instruction.AssignToVariable(className,tokens[start-2].lexeme,name,expr);
        else
            return Instruction.AssignToVariable(className,className,name,expr);  
    }
}
internal class ClassPointer
{
    public readonly string name;
    public List<FunctionPointer> functionPointers = [];
    public readonly int start,end;

    public ClassPointer(int start, int end, string name)
    {
        this.start = start;
        this.end = end;
        this.name = name;
    }
}
internal class FunctionPointer
{
    public string name;
    public readonly int start,end;
    public Instruction[] instructions;

    public FunctionPointer(int start, int end)
    {
        this.start = start;
        this.end = end;
    }
}
internal class Class
{
    public readonly string name;
    public Function[] functions= [];

    public Class(string name)
    {
        this.name = name;
    }
}
internal class Function
{
    public readonly string name;
    public Instruction[] instructions = [];
    public Function(string name)
    {
        this.name = name;
    }
}