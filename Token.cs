namespace JackCompiler
{
    public enum TokenType { KEYWORD, SYMBOL, IDENTIFIER, INT_CONST, STRING_CONST };
    public enum Keyword { CLASS, METHOD, FUNCTION, CONSTRUCTOR, INT, BOOLEAN, CHAR, VOID, VAR, 
    STATIC, FIELD, LET, DO, IF, ELSE, WHILE, RETURN, TRUE, FALSE, NULL, THIS };

    public class Token 
    {
        public TokenType Type { get; private set; }
        public Keyword Keyword { get; private set; }
        public char Symbol { get; private set; }
        public string Identifier { get; private set; }
        public string StringVal { get; private set; }
        public int IntVal { get; private set; }

        public Token(TokenType type, char symbol)
        {
            this.Type = type;
            this.Symbol = symbol;
        }

        public Token(TokenType type, int intVal)
        {
            this.Type = type;
            this.IntVal = intVal;
        }

        public Token(TokenType type, Keyword keyword)
        {
            this.Type = type;
            this.Keyword = keyword;
        }

        public Token(TokenType type, string identifierOrString)
        {
            if(type == TokenType.IDENTIFIER)
            {
                this.Type = type;
                this.Identifier = identifierOrString;
            }
            else if(type == TokenType.STRING_CONST)
            {
                this.Type = TokenType.STRING_CONST;
                this.StringVal = identifierOrString;
            }
        }

        public override string ToString()
        {
            switch(Type)
            {
                case TokenType.IDENTIFIER:
                    return "IDENTIFIER: " + Identifier;
                case TokenType.INT_CONST:
                    return "INT: " + IntVal;
                case TokenType.KEYWORD:
                    return "KEYWORD: " + Keyword;
                case TokenType.STRING_CONST:
                    return "STRING: " + StringVal;
                case TokenType.SYMBOL:
                    return "SYMBOL: " + Symbol;
                default:
                    return Identifier;
            }
        }
    }
}