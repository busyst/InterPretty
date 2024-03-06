using System.Text;
using System.Text.RegularExpressions;
class Interpreter(IEnumerable<Statement> statements)
{
    public static bool bootloader = false;
    private readonly StringBuilder main = new StringBuilder();
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
class FunctionInterpreter(string name,IEnumerable<Statement> statements)
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

    }
    private void HandleDirect(Statement statement)
    {

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
        outputBuilder.Append(main);
        outputBuilder.AppendLine("\tpop bp");
        outputBuilder.AppendLine("\tret");
        return outputBuilder.ToString();
    }
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
        if(!offsets.ContainsKey(name))
            throw new Exception("Using of non existing array. Fuck yourself.");
        if(offsets[name].size!=-1)
            throw new Exception("Reallocating existing array.");
        VariableType type = offsets[name].type;
        for (int i = 0; i < size; i++)
        {
            offsets.Add(name+$"[{i*((int)type)}]",(offset+i*((int)type),type));
        }
        offsets[name] = new (offset, type);
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