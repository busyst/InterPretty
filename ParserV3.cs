public class ParserV3(List<Token> tokens)
{
    private const byte NOTHING = 0;
    private const byte TOSEMICOLON = 1;
    private const byte EQUALPARENS = 2;
    private const byte EQUALBRACKETS = 3;
    private const byte SPECIALEQUALBRACKET = 4;
    private static readonly (StatementType type,byte expr)[] statementsTypes = 
    [
        (StatementType.Declaration,TOSEMICOLON),
        (StatementType.Declaration,TOSEMICOLON),
        (StatementType.Condition,SPECIALEQUALBRACKET),
        (StatementType.Assigment,TOSEMICOLON),
        (StatementType.Assigment,TOSEMICOLON),
        (StatementType.InsertASM,NOTHING),
        (StatementType.CreateJumpPoint,NOTHING),
        (StatementType.Jump,TOSEMICOLON),
    ];
    private static readonly TokenType[][] posibilities =
    [
        ([TokenType.TYPE,TokenType.NAME]),// int a ...
        ([TokenType.TYPE,TokenType.OPEN_SQUARE_BRACE,TokenType.CLOSE_SQUARE_BRACE,TokenType.NAME]),// int[] a ...
        ([TokenType.IF]),
        ([TokenType.NAME,TokenType.EQUAL]),
        ([TokenType.NAME,TokenType.OPEN_SQUARE_BRACE]),
        ([TokenType.ASMCODE]),
        ([TokenType.NAME,TokenType.COLON]),
        ([TokenType.GOTO]),
    ];
    public List<Statement> statements = [];
    private static void UpdateTokenTypes(List<Token> tokens)
    {
        var names = Enum.GetNames<VariableType>().Select(x => x.ToLower());
        Parallel.ForEach(tokens,(token)=>{
            if (names.Contains(token.lexeme.ToLower()))
                token.type = TokenType.TYPE;});
    }
    public void Parse()
    {
        long bts = GC.GetTotalMemory(true);
        UpdateTokenTypes(tokens);

        List<Token> buffer = [];
        for (int i1 = 0; i1 < tokens.Count; i1++)
        {
            buffer.Add(tokens[i1]);
            int stat = -1,hltCnt = 0;
            for (int j = 0; j < posibilities.Length; j++)
            {
                if(buffer.Count!=posibilities[j].Length){
                    if(buffer.Count>=posibilities[j].Length)
                        hltCnt++;
                    continue;
                }
                bool skip = false;
                for (int g = 0; g < posibilities[j].Length; g++)
                {
                    if(buffer[g].type!=posibilities[j][g])
                    {
                        skip = true;
                        break;
                    }
                }
                if(skip)
                    continue;
                stat = j;
                break;
            }
            if(stat==-1)
                if(hltCnt<posibilities.Length-1)
                    continue;
                else
                    throw new Exception("There is tokenization error!\r\n"+string.Join("\r\n",buffer.Select(x=>(x.type.ToString(),$"[{x.lexeme}]"))));
            i1++;
            var (type, expr) = statementsTypes[stat];
            List<Token> expression = [];
            if(expr!=NOTHING){
                bool started = false;
                int counter = 0;
                bool brack = ((expr==EQUALBRACKETS)||(expr==SPECIALEQUALBRACKET));
                var open = brack?TokenType.OPEN_BRACKET:TokenType.OPEN_PAREN;
                var clo = brack?TokenType.CLOSE_BRACKET:TokenType.CLOSE_PAREN;
                for (int a = i1; a < tokens.Count; a++)
                {
                    if(expr==TOSEMICOLON&&tokens[a].type==TokenType.SEMICOLON)
                        break;
                    if(expr!=TOSEMICOLON)
                        if(tokens[a].type==open){
                            counter++;
                            started = true;
                        }
                        else if(tokens[a].type==clo)
                            counter--;
                    if(started&&counter==0)
                        break;
                    expression.Add(tokens[a]);
                }
            }
            else
                i1--;
            
            if(expr==SPECIALEQUALBRACKET)
            {
                int offset = -1;
                for (int i = 0; i < expression.Count; i++)
                {
                    if(expression[i].type==TokenType.OPEN_BRACKET)
                    {
                        offset=i;
                        break;
                    }
                }
                var full = expression.ToArray();
                var exp = full.AsSpan(0,offset).ToArray();
                var content = full.AsSpan(offset+1).ToArray().ToList();
                string label = ".L"+i1;

                statements.Add(new Statement(type){_1_statement = exp,_2_expresion = [new Token(){lexeme = label,type = TokenType.NAME}]});
                ParserV3 parserV3 = new ParserV3(content);parserV3.Parse();
                statements.AddRange(parserV3.statements);
                statements.Add(new Statement(StatementType.CreateJumpPoint){_1_statement = [new Token(){lexeme = label,type = TokenType.NAME},new Token(){lexeme = ":",type = TokenType.COLON}],_2_expresion = []});
                i1+=expression.Count;
                expression.Clear();
                buffer.Clear();
                continue;
            }
            i1+=expression.Count;
            statements.Add(new Statement(type){_1_statement = [.. buffer],_2_expresion = [.. expression]});
            expression.Clear();
            buffer.Clear();
        }

        CorrectStatements();
        System.Console.WriteLine(GC.GetTotalMemory(false) - bts);
    }
    private void CorrectStatements()
    {
        foreach (var x in statements)
        {
            if(x.type == StatementType.CreateJumpPoint)
            {
                x._1_statement = x._1_statement.AsSpan(0,1).ToArray();
            }
            if(x.type == StatementType.Declaration&&x._1_statement[1].type==TokenType.OPEN_SQUARE_BRACE)
            {
                x._2_expresion = x._2_expresion.AsSpan(2).ToArray();
                x.flag = 1;
                continue;
            }
            if(x.type == StatementType.Assigment&&x._1_statement[1].type==TokenType.EQUAL)
            {
                var c = new List<Token>(){x._1_statement[1]};
                c.AddRange(x._2_expresion);
                x._2_expresion = [.. c];
                x._1_statement = [x._1_statement[0]];
                continue;
            }
            if(x.type == StatementType.Assigment&&x._1_statement[1].type==TokenType.OPEN_SQUARE_BRACE)
            {
                int offset = -1;
                for (int i = 0; i < x._2_expresion.Length; i++)
                {
                    if(x._2_expresion[i].type==TokenType.CLOSE_SQUARE_BRACE)
                    {
                        offset=i;
                        break;
                    }
                    
                }
                var c = x._1_statement.ToList();
                c.AddRange(x._2_expresion.AsSpan(0,offset+1));
                x._1_statement = [.. c];
                x._2_expresion = x._2_expresion.AsSpan(offset+1).ToArray();
                x.flag = 1;
                continue;
            }
        }
    }
}
public class Statement(StatementType type)
{
    public readonly StatementType type = type;
    public Token[] _1_statement;
    public Token[] _2_expresion;
    public byte flag = 0;

    
}
public enum StatementType
{
    Declaration, Assigment,Condition,Call,Jump,InsertASM,CreateJumpPoint,Expression
}