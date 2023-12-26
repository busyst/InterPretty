enum TokenType{
    NAME,
    NUMBER,
    EQUAL,
    PLUS,
    MINUS,
    MULTIPLY,
    DIVIDE,
    SPECIAL_SYMBOL,
    IF,
    GOTO,
    OPEN_PAREN,
    CLOSE_PAREN,
    OPEN_BRACE,
    OPEN_SQUARE_BRACE,
    CLOSE_SQUARE_BRACE,
    CLOSE_BRACE,
    COLON,
    SEMICOLON,
    MODIFIER,
    PREPROCES,
    ASMCODE,
    EOF_TOKEN
};
class Token
{
    public TokenType type;
    public string lexeme = string.Empty;
    public static Token CreateToken(TokenType type, string lexeme) {
        Token token = new Token
        {
            type = type,
            lexeme = lexeme
        };
        return token;
    }
}
class Lexer(string input)
{
    private int position = 0;
    public Token Lex() 
    {
        SkipWhitespace();

        char current = Peek();

        if (current == '\0') {
            return CreateToken(TokenType.EOF_TOKEN, "");
        }
        switch (current) {
            case '+':
                Advance();
                return CreateToken(TokenType.PLUS, "");
            case '-':
                Advance();
                return CreateToken(TokenType.MINUS, "");
            case '*':
                Advance();
                return CreateToken(TokenType.MULTIPLY, "");
            case '/':
                if(Peek()=='/')
                {
                    while (Advance()!='\n'){}
                    return null;
                }
                Advance();
                return CreateToken(TokenType.DIVIDE, "");
            case '(':
                Advance();
                return CreateToken(TokenType.OPEN_PAREN, "");
            case ')':
                Advance();
                return CreateToken(TokenType.CLOSE_PAREN, "");
            case '{':
                Advance();
                return CreateToken(TokenType.OPEN_BRACE, "");
            case '}':
                Advance();
                return CreateToken(TokenType.CLOSE_BRACE, "");
            case ':':
                Advance();
                return CreateToken(TokenType.COLON, "");
            case ';':
                Advance();
                return CreateToken(TokenType.SEMICOLON, "");
            case '=':
                Advance();
                return CreateToken(TokenType.EQUAL, "");
            case '&':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, "&");
            case '.':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, ".");
            case ',':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, ",");
            case '|':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, "|");
            case '>':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, ">");
            case '<':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, "<");
            case '$':
                Advance();
                return CreateToken(TokenType.SPECIAL_SYMBOL, "$");
            case '[':
                Advance();
                return CreateToken(TokenType.OPEN_SQUARE_BRACE, "");
            case ']':
                Advance();
                return CreateToken(TokenType.CLOSE_SQUARE_BRACE, "");
            case '\'':
                Advance();
                char[] chars = new char[2];
                chars[0] = Advance();
                if(chars[0]=='\\')
                {
                    chars[1] = Advance();
                }
                Advance();
                return CreateToken(TokenType.NUMBER,$"{(int)chars[0]}");
            default:
                if (char.IsAsciiLetter(current) || current == '_') 
                    return LexName();
                else if(char.IsDigit(current))
                    return LexNumber();
                else 
                    return CreateToken(TokenType.EOF_TOKEN,"");
        }
    }
    private static Token CreateToken(TokenType tt,string lexeme)=>Token.CreateToken(tt,lexeme);
    private char Peek() => input[position];
    private char Advance() {
        char current = Peek();
        if (current != '\0') {
            position++;
        }
        if(position == input.Length)
        {
            input+='\0';
        }
        return current;
    }

    private void SkipWhitespace() {
        while (char.IsWhiteSpace(Peek())) {
            Advance();
        }
    }
    private Token LexName() {
        string buffer = string.Empty;
        while (char.IsAsciiLetter(Peek()) || Peek() == '_') {
            buffer += Advance();
            if (buffer.Length>63) {
                System.Console.WriteLine("Name is too long:{0}",buffer);
                return CreateToken(TokenType.EOF_TOKEN, "");
            }
        }
        if(buffer=="asm")
        {
            var code = "";
            SkipWhitespace();
            Advance();
            int braces = 1;
            while (true)
            {
                var c = Advance();
                if(c == '}')
                {
                    braces--;
                    if(braces<=0)
                        break;
                }
                else if(c=='{')
                    braces++;
                code+=c;
            }
            return CreateToken(TokenType.ASMCODE, code);
        }
        return buffer switch
        {
            "if" => CreateToken(TokenType.IF, ""),
            "goto" => CreateToken(TokenType.GOTO, ""),
            _ => CreateToken(TokenType.NAME, buffer),
        };
    }

    private Token LexNumber() {
        string buffer = string.Empty;
        while (char.IsDigit(Peek())||Peek()=='x'||(Peek()>=65&&Peek()<=70)) {
            buffer += Advance();
            if (buffer.Length>63) {
                System.Console.WriteLine("Number is too long:{0}",buffer);
                return CreateToken(TokenType.EOF_TOKEN, "");
            }
        }
        return CreateToken(TokenType.NUMBER, buffer);
    }
    private Token LexPreproces(){
        string buffer = string.Empty;
        while (char.IsAsciiLetter(Peek()) || Peek() == '_' || Peek()=='#') {
            buffer += Advance();
            if (buffer.Length>63) {
                System.Console.WriteLine("Name is too long:{0}",buffer);
                return CreateToken(TokenType.EOF_TOKEN, "");
            }
        }
        return CreateToken(TokenType.PREPROCES, buffer);
    }
}