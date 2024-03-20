using System.Text;
using System.Text.RegularExpressions;
class Interpreter(IEnumerable<Statement> statements)
{
    public static bool bootloader = false;
    private readonly StringBuilder main = new();
    public string Interpret()
    {
        FunctionInterpreter functionInterpreter = new("_start",statements);
        functionInterpreter.Parse();
        main.Append(functionInterpreter.GetCode());
        return GetCode();
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

partial class FunctionInterpreter(string name,IEnumerable<Statement> statements)
{
    private ASMCOMM asmContext = new();

    private readonly StringBuilder main = new StringBuilder();
    private void AddLine(string str) => main.AppendLine('\t' + str);
    private void HandleCreation(Statement statement)
    {
        var type = Variable.VTFromString(statement._1_statement[0].lexeme);
        System.Console.WriteLine();
        if(statement.flag==1) // Array
        {
            asmContext.PreAllocArray(statement._1_statement[3].lexeme,type);
            statement._1_statement = statement._1_statement.AsSpan(3).ToArray();
            HandleOperation(statement);
        }
        else//Variable
        {
            asmContext.AllocValue(statement._1_statement[1].lexeme,type);
            statement._1_statement = statement._1_statement.AsSpan(1).ToArray();
            HandleOperation(statement);
        }
    }
    private void HandleOperation(Statement statement)
    {
        var expression = statement._2_expresion;
        if(expression.Length==0)
            return;
        string name = statement._1_statement[0].lexeme;
        bool array = asmContext.arrays.Contains(name);
        if(array&&statement._1_statement.Length>1)
        {
            var ind = statement._1_statement.AsMemory(2,statement._1_statement.Length-3);
            // array[smt] = ...
            if(!IsComplex(ind))
            {
                ExpresionSolver(expression,name+$"[{ind.Span[0].lexeme}]");
                return;
            }
            throw new NotImplementedException();
        }
        else if(array)
        {
            // array = ...
            int from = 0;
            int c = 0;
            List<ReadOnlyMemory<Token>> exprs = [];
            for (int i = 0; i < expression.Length; i++)
            {
                if(c==0&&((expression[i].type==TokenType.SPECIAL_SYMBOL&&expression[i].lexeme==",")||i+1==expression.Length))
                {
                    exprs.Add(expression.AsMemory(from,i-from));
                    from = i+1;
                    i++;
                }
                else if(expression[i].type==TokenType.SPECIAL_SYMBOL&&expression[i].lexeme=="(")
                    c++;
                else if(expression[i].type==TokenType.SPECIAL_SYMBOL&&expression[i].lexeme=="(")
                    c--;
                
            }
            asmContext.AllocArray(name,exprs.Count);
            for (int i = 0; i < exprs.Count; i++)
            {
                if(ExpresionSolver(exprs[i],name+$"[{i}]"))
                {

                }

            }

        }
        else
        {
            // var = ...
            if(ExpresionSolver(expression,name))
            {

            }
        }
    }

    static bool IsComplex(ReadOnlyMemory<Token> tokens)
    {
        if(tokens.Length==1&&char.IsNumber(tokens.Span[0].lexeme[0]))
            return false;

        return true;

    }
    enum LexemeType
    {
        VarName,Greater,Less,GreaterEqual,LessEqual,EqualEqual,Index,OpenParen,ClosedParen,Comma,Dot,Plus,Minus,Multiply,Module,Increment,Decrement,And,Or,Not,BitwiseAnd,BitwiseOr,BitwiseXor,BitwiseNot,ShiftLeft,ShiftRight
    }
    class InternalLexer
    {
        static int Precedence(LexemeType type)
        {
            return type switch
            {
                LexemeType.Multiply or LexemeType.Module => 3,
                LexemeType.Plus or LexemeType.Minus => 2,
                LexemeType.Less or LexemeType.LessEqual or LexemeType.Greater or LexemeType.GreaterEqual or LexemeType.EqualEqual => 1,
                _ => 0,
            };
        }
        public List<(LexemeType type,string lexeme)> ConvertToRPN(List<(LexemeType type,string lexeme)> tokens)
        {
            Stack<(LexemeType type,string lexeme)> operatorStack = new Stack<(LexemeType type,string lexeme)>();
            List<(LexemeType type,string lexeme)> outputQueue = new List<(LexemeType type,string lexeme)>();

            foreach (var token in tokens)
            {
                switch (token.type)
                {
                    case LexemeType.VarName:
                        outputQueue.Add(token);
                        break;
                    case LexemeType.OpenParen:
                        operatorStack.Push(token);
                        break;
                    case LexemeType.ClosedParen:
                        while (operatorStack.Count > 0 && operatorStack.Peek().type != LexemeType.OpenParen)
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        if (operatorStack.Count == 0)
                            throw new Exception("Mismatched parentheses");
                        operatorStack.Pop(); // Discard the OpenParen
                        break;
                    default:
                        while (operatorStack.Count > 0 && Precedence(token.type) <= Precedence(operatorStack.Peek().type))
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        operatorStack.Push(token);
                        break;
                }
            }
            while (operatorStack.Count > 0)
                outputQueue.Add(operatorStack.Pop());

            return outputQueue;
        }
        public List<(LexemeType type,string lexeme)> values = [];
        public InternalLexer(ReadOnlyMemory<Token> tokens)
        {
            var span = tokens.Span;
            for (int i = 0; i < span.Length; i++)
            {
                TokenType Peek(int forward)
                {
                    if((i+forward)>=tokens.Span.Length)
                        return TokenType.EOF_TOKEN;
                    else
                        return tokens.Span[i+forward].type;
                }
                switch (span[i].type)
                {
                    case TokenType.NAME:
                        if(Peek(1)== TokenType.OPEN_SQUARE_BRACE)
                        {
                            var r = CollectionUtils.CaptureUntil(span.ToArray(),i+2,x=>x.type==TokenType.CLOSE_SQUARE_BRACE);
                            if(!IsComplex(r))
                            {
                                values.Add((LexemeType.VarName,span[i].lexeme+$"[{r[0].lexeme}]"));
                                i+=3;
                                break;
                            }
                            throw new NotImplementedException();
                            System.Console.WriteLine();
                            break;
                        }
                        values.Add((LexemeType.VarName,span[i].lexeme));
                        break;
                    case TokenType.NUMBER:
                        values.Add((LexemeType.VarName,span[i].lexeme));
                        break;
                    case TokenType.SPECIAL_SYMBOL:
                        var ss = span[i].lexeme;
                        switch (ss)
                        {
                            case "==":
                                values.Add((LexemeType.EqualEqual,""));
                                break;
                            default:
                                throw new NotImplementedException($"{ss}");
                        }
                        break;
                    case TokenType.OPEN_PAREN:
                        values.Add((LexemeType.OpenParen,""));
                        break;
                    case TokenType.CLOSE_PAREN:
                        values.Add((LexemeType.ClosedParen,""));
                        break;
                    
                    default:
                        throw new NotImplementedException($"{span[i].type}");
                }
            }
            while (values.First().type == LexemeType.OpenParen && values.Last().type == LexemeType.ClosedParen)
            {
                values.RemoveAt(0);
                values.RemoveAt(values.Count - 1);
            }
        }
    }
    bool StatementSolver(ReadOnlyMemory<Token> tokens,string addition)
    {
        InternalLexer internalLexer = new(tokens);
        var c = new Queue<(LexemeType type,string lexeme)>(internalLexer.ConvertToRPN(internalLexer.values));
        var stack = new Queue<(LexemeType type,string lexeme)>();
        while (c.TryDequeue(out var result))
        {
            switch (result.type)
            {
                case LexemeType.VarName:
                    stack.Enqueue(result);
                    break;
                case LexemeType.EqualEqual:
                    if(stack.Count!=2)
                        throw new Exception("Wrong argument count");
                    var fo = stack.Dequeue().lexeme;
                    var so = stack.Dequeue().lexeme;
                    AddLine(asmContext.Compare(fo,so));

                    if(c.Count==0) // Last
                        AddLine($"jne {addition}");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        
        return false;
    }
    bool ExpresionSolver(ReadOnlyMemory<Token> tokens,string writeTo)
    {
        var toks = tokens.Span;
        var operation = TokenType.EQUAL;
        if(toks[0].type == TokenType.EQUAL)
        {
            toks = toks[1..];
        }
        if(toks.Length==0)
            return false;
        if(toks.Length==1)
        {
            AddLine(asmContext.CopyValue(writeTo,toks[0].lexeme));
            return false;
        }
        System.Console.WriteLine();
        return true;
    }
    private void HandleCondition(Statement statement)
    {
        StatementSolver(statement._1_statement,statement._2_expresion[0].lexeme);

    }
    private void HandleDirect(Statement statement)
    {
        var code = statement._1_statement[0].lexeme;
        while (true)
        {
            var r = MyRegex().Match(code);
            if(!r.Success)
                break;
            var ind = r.Index;
            code = code.Remove(ind,r.Length);
            code = code.Insert(ind,asmContext.LocalValue(r.ValueSpan[1..^1].ToString().Trim()));
        }

        string[] lines = code.Split('\n');
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();
            string tabbedLine = (i==0?"":"\t") + trimmedLine;
            sb.AppendLine(tabbedLine);
        }
        AddLine(sb.ToString());
    }
    public void Parse()
    {
        Queue<Statement> statementsBuffer;
        statementsBuffer = new Queue<Statement>(statements);
        while(statementsBuffer.Count!=0)
        {
            var statement = statementsBuffer.Dequeue();
            switch (statement.type)
            {
                case StatementType.Declaration:
                    HandleCreation(statement);
                    break;
                case StatementType.Assigment:
                    HandleOperation(statement);
                    break;
                case StatementType.Condition:
                    HandleCondition(statement);
                    break;
                case StatementType.CreateJumpPoint:
                    HandleCJP(statement);
                    break;
                case StatementType.InsertASM:
                    HandleDirect(statement);
                    break;
                default:
                    throw new NotImplementedException("Wrong instruction!");
            }
        }
    }
    private void HandleCJP(Statement statement) => AddLine($"{statement._1_statement[0].lexeme}:");

    public string GetCode()
    {
        StringBuilder outputBuilder = new StringBuilder();
        outputBuilder.AppendLine($"{name}:");
        outputBuilder.AppendLine("\tpush bp");
        //outputBuilder.AppendLine("\tmov bp, sp");
        outputBuilder.Append(main);
        outputBuilder.AppendLine("\tpop bp");
        outputBuilder.AppendLine("\tret");
        return outputBuilder.ToString();
    }

    [GeneratedRegex(@"\{(.+?)\}")]
    private static partial Regex MyRegex();
}
public class ASMCOMM
{
    private int offset = 0;
    public HashSet<string> arrays = [];
    public Dictionary<string,(int size,VariableType type)> offsets = [];
    public void AllocValue(string name, VariableType type)
    {
        offset+= (int)type;
        offsets.Add(name,(offset,type));
}
    public void PreAllocArray(string name, VariableType type)
    {
        offsets.Add(name,(-1,type));
        arrays.Add(name);
    }
    public void AllocArray(string name,int size)
    {
        if(!arrays.Contains(name))
            throw new Exception("Using of non existing array. Fuck yourself.");
        if(offsets[name].size!=-1)
            throw new Exception("Reallocating existing array.");
        VariableType type = offsets[name].type;
        for (int i = size - 1; i >= 0; i--)
        {
            offsets.Add(name+$"[{i}]",(offset+i*((int)type),type));
        }
        offsets[name] = new ((size - 1)*((int)type), type);
        offset+= ((int)type) * size;
   
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
    public string Compare(string what1,string what2)
    {
        if(offsets.ContainsKey(what1)||offsets.ContainsKey(what2))
        {
            if(offsets.ContainsKey(what2))
                return $"mov ax, {LocalValue(what1)}"+"\t"+$"cmp ax,{LocalValue(what2)}";
            else
                return $"cmp {LocalValue(what1)},{what2}";
        }
        else
            return $"cmp {what1},{what2}";

        throw new Exception("Wha?");
    }
}


public class Register(string name){public string Name => name;}