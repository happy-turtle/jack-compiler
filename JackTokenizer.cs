using System;
using System.Collections.Generic;

namespace JackCompiler
{
    /// <summary>
    /// Serializes the input stream into Jack-language tokens.
    /// The token types are specified according to the Jack language grammar.
    /// </summary>
    class JackTokenizer
    {
        List<string> tokens;
        int currentToken = 0;

        readonly string[] keywords = { "class", "constructor", "function", "method", "field", "static",
        "var", "int", "char", "boolean", "void", "true", "false", "null", "this", "let", "do", "if",
        "else", "while", "return" };

        readonly char[] symbols = { '{', '}', '(', ')', '[', ']', '.', ',', ';', '+', '-', '*', '/',
        '&', '|', '<', '>', '=', '~' };

        /// <summary>
        /// Opens the input .jack file and gets ready to tokenize it.
        /// </summary>
        public JackTokenizer(string[] jackLines)
        {
            tokens = Tokenize(jackLines);            
        }

        List<string> Tokenize(string[] lines)
        {
            List<string> tokenized = new List<string>();
            foreach(string line in lines)
            {
                string[] parts = line.Split(' ', (StringSplitOptions)3);
                foreach(string part in parts)
                {
                    //TODO: split at symbols
                    tokenized.Add(part);
                }
            }
            return tokenized;
        }

        /// <summary>
        /// Are there more tokens in the input?
        /// </summary>
        public bool HasMoreTokens()
        {            
            if(currentToken < tokens.Count)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Gets the next token from the input and makes it the current token.
        /// This method should be called only if HasMoreTokens returns true.
        /// Initially there is no current token.
        /// </summary>
        public void Advance()
        {
            currentToken++;
        }

        /// <summary>
        /// Returns the type of the current token, as a constant.
        /// </summary>
        public TokenType GetTokenType()
        {
            return TokenType.KEYWORD;
        }

        /// <summary>
        /// Returns the keyword which is the current token, as a constant.
        /// This method should be called only if TokenType is KEYWORD.
        /// </summary>
        public Keyword GetKeyword()
        {
            return Keyword.NULL;
        }

        /// <summary>
        /// Returns the character which is the current token.
        /// This method should be called only if TokenType is SYMBOL.
        /// </summary>
        public char GetSymbol()
        {
            return '0';
        }

        /// <summary>
        /// Returns the identifier which is the current token.
        /// Should be called only if TokenType is IDENTIFIER.
        /// </summary>
        public string GetIdentifier()
        {
            return "";
        }

        /// <summary>
        /// Returns the integer value of the current token.
        /// Should be called only if TokenType is INT_CONST.
        /// </summary>
        public int GetIntVal()
        {
            return 0;
        }

        /// <summary>
        /// Returns the string value of the current token, without the two enclosing double quotes.
        /// Should be called only if TokenType is STRING_CONST.
        /// </summary>
        public string GetStringVal()
        {
            return "";
        }
    }
}