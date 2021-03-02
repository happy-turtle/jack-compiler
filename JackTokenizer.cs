using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        //This matches the KEYWORD enum of the class Token, so we can use the matching id.
        readonly string[] keywords = { "class", "method", "function", "constructor", "int", "boolean", "char", "void", 
        "var", "static", "field", "let", "do", "if", "else", "while", "return", "true", "false", "null", "this" };
        const string symbolReg = "[\\&\\*\\+\\(\\)\\.\\/\\,\\-\\]\\;\\~\\}\\|\\{\\>\\=\\[\\<]";
        const string intReg = "[0-9]+";
        const string strReg = "\"[^\"\n]*\"";
        const string idReg = "[\\w_]+";
        string keywordReg = "";

        /// <summary>
        /// Opens the input .jack file and gets ready to tokenize it.
        /// </summary>
        public JackTokenizer(string[] jackLines)
        {
            Tokenize(jackLines);            
        }

        void Tokenize(string[] lines)
        {
            tokens = new List<string>();

            //build regex pattern
            keywordReg = "";
            foreach (var keyword in keywords)
                keywordReg += keyword + "|";
            string pattern = keywordReg + symbolReg + "|" + intReg + "|" + strReg + "|" + idReg;
            keywordReg = keywordReg.Remove(keywordReg.Length - 1);

            foreach(string line in lines)
            {
                //find tokens
                MatchCollection matches = Regex.Matches(line, pattern);
                foreach(Match match in matches)
                    tokens.Add(match.Value);
            }
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
            if(Regex.Match(tokens[currentToken], keywordReg).Success)
                return TokenType.KEYWORD;
            else if(Regex.Match(tokens[currentToken], symbolReg).Success)
                return TokenType.SYMBOL;
            else if(Regex.Match(tokens[currentToken], intReg).Success)
                return TokenType.INT_CONST;
            else if(Regex.Match(tokens[currentToken], strReg).Success)
                return TokenType.STRING_CONST;
            else if(Regex.Match(tokens[currentToken], idReg).Success)
                return TokenType.IDENTIFIER;
            else
                throw new Exception("Unknown token");
        }

        /// <summary>
        /// Returns the keyword which is the current token, as a constant.
        /// This method should be called only if TokenType is KEYWORD.
        /// </summary>
        public Keyword GetKeyword()
        {
            string word = Regex.Match(tokens[currentToken], keywordReg).Value;
            for(int i = 0; i < keywords.Length; i++)
            {
                if(keywords[i] == word)
                    return (Keyword)i;
            }
            return Keyword.NULL;
        }

        /// <summary>
        /// Returns the character which is the current token.
        /// This method should be called only if TokenType is SYMBOL.
        /// </summary>
        public char GetSymbol()
        {
            return tokens[currentToken].ToCharArray()[0];
        }

        /// <summary>
        /// Returns the identifier which is the current token.
        /// Should be called only if TokenType is IDENTIFIER.
        /// </summary>
        public string GetIdentifier()
        {
            return tokens[currentToken];
        }

        /// <summary>
        /// Returns the integer value of the current token.
        /// Should be called only if TokenType is INT_CONST.
        /// </summary>
        public int GetIntVal()
        {
            return int.Parse(tokens[currentToken]);
        }

        /// <summary>
        /// Returns the string value of the current token, without the two enclosing double quotes.
        /// Should be called only if TokenType is STRING_CONST.
        /// </summary>
        public string GetStringVal()
        {
            return tokens[currentToken];
        }
    }
}