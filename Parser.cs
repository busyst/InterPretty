class Parser(List<Token> tokens)
{
    public static int Counter = 0; 
    private int i = 0;
    private readonly (TokenType[] tokens,Func<ParseFunction,List<Token>,int,int> handle)[] posibilities =
    [
        ([TokenType.NAME,TokenType.NAME,TokenType.OPEN_PAREN],FunctionCreation),// void a(...
        ([TokenType.NAME,TokenType.NAME,TokenType.SEMICOLON],VariableCreation),// int a;
        ([TokenType.NAME,TokenType.NAME,TokenType.EQUAL],VariableCreation),// int a = ...
        ([TokenType.NAME,TokenType.EQUAL],AssignTo), // a =...
        ([TokenType.NAME,TokenType.OPEN_PAREN],FunctionCall),// a(
        ([TokenType.NAME,TokenType.COLON],LabelCreation),// a:
        ([TokenType.IF,TokenType.OPEN_PAREN],Condition),//if(...
        ([TokenType.GOTO,TokenType.NAME,TokenType.SEMICOLON],JumpTo),//goto a
    ];
    private static int FunctionCreation(ParseFunction pc,List<Token> tokens,int pos)
    {
        ParseFunction function = new ParseFunction{
            name = tokens[pos + 1].lexeme
        };
        int pointer = pos+3;
        int parens = 1;
        List<Token> ttp = [];
        while (true)
        {
            if(tokens[pointer].type == TokenType.CLOSE_PAREN)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            else if(tokens[pointer].type == TokenType.OPEN_PAREN)
                parens++;
            if(!(tokens[pointer].type == TokenType.SPECIAL_SYMBOL&&tokens[pointer].lexeme[0]==','))
                ttp.Add(tokens[pointer]);
            pointer++;
        }
        ttp.Clear();
        pointer+=2;
        parens = 1;
        while (true)
        {
            if(tokens[pointer].type == TokenType.CLOSE_BRACE)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            ttp.Add(tokens[pointer]);
            pointer++;
        }
        Parser parser = new Parser(ttp);
        parser.Parse(function);
        pc.functions.Add(function);
        return pointer - pos - 2;
    }
    private static int VariableCreation(ParseFunction pc,List<Token> tokens,int pos)
    {
        var ttp = new List<Token>();
        int pointer = pos+1;
        while (true)
        {
            if(tokens[pointer].type==TokenType.SEMICOLON)
                break;
            ttp.Add(tokens[pointer]);
            pointer++;
        }
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.CREATE_VARIABLE,args = [tokens[pos],tokens[pos+1]]});
        
        if(ttp.Count>1)
        {
            AssignTo(pc,ttp,-1);
        }
        else
        {
            pc.instructions.Add(new Instruction(){instructionType = InstructionType.OPERATION,args = [tokens[pos+1],new Token(){type = TokenType.NUMBER,lexeme = "0"}]});
        }
        return pointer-pos-2;
    }
    private static int AssignTo(ParseFunction pc,List<Token> tokens,int pos)
    {
        if(pos==-1)
        {
            var toks= new List<Token>(){tokens[0]};
            for (int i = 2; i < tokens.Count; i++)
                toks.Add(tokens[i]);
            pc.instructions.Add(new Instruction(){instructionType = InstructionType.OPERATION,args = toks});
            return 0;
        }
        var ttp = new List<Token>();
        int pointer = pos;
        while (true)
        {
            if(tokens[pointer].type==TokenType.SEMICOLON)
                break;
            if(tokens[pointer].type!=TokenType.EQUAL)
                ttp.Add(tokens[pointer]);
            pointer++;
        }
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.OPERATION,args = ttp});
        return pointer-pos-1;
    }
    private static int FunctionCall(ParseFunction pc,List<Token> tokens,int pos)
    {
        List<Token> args = [];
        args.Add(tokens[pos]);
        int pointer = pos+2;
        int parens = 1;
        while (true)
        {
            if(tokens[pointer].type == TokenType.CLOSE_PAREN)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            else if(tokens[pointer].type == TokenType.OPEN_PAREN)
                parens++;
            if(!(tokens[pointer].type == TokenType.SPECIAL_SYMBOL&&tokens[pointer].lexeme[0]==','))
                args.Add(tokens[pointer]);
            pointer++;
        }
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.CALL_FUNCTION,args = args});
        
        return pointer - pos;
    }
    private static int JumpTo(ParseFunction pc,List<Token> tokens,int pos)
    {
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.JUMP_TO,args = [tokens[pos+1]]});
        return 0;
    }
    private static int LabelCreation(ParseFunction pc,List<Token> tokens,int pos)
    {
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.CREATE_JP,args = [tokens[pos]]});
        return 0;
    }
    private static int Condition(ParseFunction pc,List<Token> tokens,int pos)
    {
        int pointer = pos+2;
        int parens = 1;
        List<Token> ttp = [];
        while (true)
        {
            if(tokens[pointer].type == TokenType.CLOSE_PAREN)
            {
                parens--;
                if(parens == 0)
                    break;
            }
            else if(tokens[pointer].type == TokenType.OPEN_PAREN)
                parens++;
            ttp.Add(tokens[pointer]);
            pointer++;
        }
        string label = $"_C{Counter++}";
        var tre = new List<Token>();
        tre.Add(new Token(){type = TokenType.NAME,lexeme = label});
        tre.AddRange(ttp);
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.CONDITION,args = tre});
        ttp.Clear();
        
        pointer+=2;
        parens = 1;
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
            ttp.Add(tokens[pointer]);
            pointer++;
        }
        var t = new ParseFunction();
        Parser parser = new Parser(ttp);
        parser.Parse(t);
        pc.instructions.AddRange(t.instructions);
        pc.instructions.Add(new Instruction(){instructionType = InstructionType.CREATE_JP,args = [new Token(){type = TokenType.NAME,lexeme = label}]});
        
        return pointer-pos-1;
    }

    public void Parse(in ParseFunction function)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if(tokens[i].type == TokenType.NAME&&tokens[i].lexeme == "while")
            {
                tokens[i].type = TokenType.IF;
                tokens[i].lexeme=  "";
            }
            if(tokens[i].type == TokenType.OPEN_PAREN&&tokens[i+1].type == TokenType.NAME&&tokens[i+2].type == TokenType.CLOSE_PAREN)
            {
                tokens.Insert(i,new Token(){type = TokenType.MODIFIER,lexeme = tokens[i+1].lexeme});
                tokens.RemoveAt(i+1);
                tokens.RemoveAt(i+1);
                tokens.RemoveAt(i+1);
            }
        }

        List<TokenType> buffer = [];
        while(i < tokens.Count)
        {
            var token = tokens[i];
            buffer.Add(token.type);
            for (int j = 0; j < posibilities.Length; j++)
            {
                bool skip = false;
                for (int g = 0; g < buffer.Count; g++)
                {
                    if(buffer[g]!=posibilities[j].tokens[g])
                    {
                        skip = true;
                        break;
                    }
                }
                if(skip)
                    continue;
                if(posibilities[j].tokens.Length == buffer.Count)
                {
                    i+=posibilities[j].handle(function,tokens,i-posibilities[j].tokens.Length+1);
                    buffer.Clear();
                }
                break;
            }
            i++;
        }
    }
}
