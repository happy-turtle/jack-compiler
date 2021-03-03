using System;
using System.Xml;
using System.Collections.Generic;

namespace JackCompiler
{
    /// <summary>
    /// Generates the compiler's output.
    /// </summary>
    class CompilationEngine
    {
        XmlDocument document;
        List<Token> tokens;
        Token current;

        /// <summary>
        /// Creates a new compilation engine with the given input and output.
        /// The next routine called must be CompileClass.
        /// </summary>
        public CompilationEngine(List<Token> tokenList)
        {
            document = new XmlDocument();
            tokens = tokenList;
            current = tokens[0];
            CompileClass();
        }

        /// <summary>
        /// Compiles a complete class.
        /// </summary>
        void CompileClass()
        {
            XmlNode root = document.AppendChild(document.CreateNode(XmlNodeType.Element, "class", ""));
            //class
            AppendKeyword(root);
            //class name
            AppendIdentifier(root);
            //'{'
            AppendSymbol(root);
            //classVarDec*
            CompileClassVarDec(root);
            //subroutineDec*
            CompileSubroutineDec(root);
            //'}'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles a static variable declaration or a field declaration.
        /// </summary>
        void CompileClassVarDec(XmlNode parent)
        {
            while(current.Keyword == Keyword.STATIC || current.Keyword == Keyword.FIELD)
            {
                XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "classVarDec", ""));
                //'static'|'field'
                AppendKeyword(root);
                //type
                CompileType(root);
                //varName
                AppendIdentifier(root);
                //(, varName)*
                while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
                {
                    AppendSymbol(root);
                    AppendIdentifier(root);
                }
                //';'
                AppendSymbol(root);
            }
        }

        /// <summary>
        /// Compiles a complete method, function or constructor.
        /// </summary>
        void CompileSubroutineDec(XmlNode parent)
        {            
            while(current.Keyword == Keyword.FUNCTION || current.Keyword == Keyword.METHOD ||
                current.Keyword == Keyword.CONSTRUCTOR)
            {
                XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "subroutineDec", ""));
                //'constructor'|'function'|'method'
                AppendKeyword(root);
                //'void|type'
                if(current.Keyword == Keyword.VOID)
                    AppendKeyword(root);
                else
                    CompileType(root);
                //subroutineName
                AppendIdentifier(root);
                //'('parameterList')'
                AppendSymbol(root);
                CompileParameterList(root);
                AppendSymbol(root);
                //subroutineBody
                CompileSubroutineBody(root);
            }
        }

        /// <summary>
        /// Compiles a (possibly empty) parameter list. Does not handle the enclosing "()".
        /// </summary>
        void CompileParameterList(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "parameterList", ""));
            if(current.Type == TokenType.SYMBOL && current.Symbol == ')')
                return;
            
            //type varName
            CompileType(root);
            AppendIdentifier(root);

            //(',' type varName)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                AppendSymbol(root);
                CompileType(root);
                AppendIdentifier(root);
            }
        }

        /// <summary>
        /// Compiles a subroutine's body.
        /// </summary>
        void CompileSubroutineBody(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "subroutineBody", ""));
            //'{'
            AppendSymbol(root);
            //varDec*
            while(current.Keyword == Keyword.VAR)
                CompileVarDec(root);
            //statements
            CompileStatements(root);
            //'}'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles a var declaration.
        /// </summary>
        void CompileVarDec(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "varDec", ""));
            //var
            AppendKeyword(root);
            //type
            CompileType(root);
            //varName (, varName)*
            AppendIdentifier(root);
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                AppendSymbol(root);
                AppendIdentifier(root);
            }
            //';'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles a sequence of statements. Does not handle the enclosing "()".
        /// </summary>
        void CompileStatements(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "statements", ""));
            while(current.Keyword == Keyword.LET ||
                current.Keyword == Keyword.IF ||
                current.Keyword == Keyword.WHILE ||
                current.Keyword == Keyword.DO ||
                current.Keyword == Keyword.RETURN)
            {
                if(current.Keyword == Keyword.LET)
                    CompileLet(root);
                else if(current.Keyword == Keyword.IF)
                    CompileIf(root);
                else if(current.Keyword == Keyword.WHILE)
                    CompileWhile(root);
                else if(current.Keyword == Keyword.DO)
                    CompileDo(root);
                else if(current.Keyword == Keyword.RETURN)
                    CompileReturn(root);
            }
        }

        /// <summary>
        /// Compiles a let statement.
        /// </summary>
        void CompileLet(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "letStatement", ""));
            //'let'
            AppendKeyword(root);
            //varName
            AppendIdentifier(root);
            //'['expression']'
            if(current.Symbol == '[')
            {
                AppendSymbol(root);
                CompileExpression(root);
                AppendSymbol(root);
            }
            //'='
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            //';'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles an if statement, possibly with a trailing else clause.
        /// </summary>
        void CompileIf(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "ifStatement", ""));
            //if
            AppendKeyword(root);
            //'('
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            //')'
            AppendSymbol(root);
            //'{'
            AppendSymbol(root);
            //statements
            CompileStatements(root);
            //'}'
            AppendSymbol(root);
            //else'{'statements'}'
            if(current.Keyword == Keyword.ELSE)
            {
                AppendKeyword(root);
                AppendSymbol(root);
                CompileStatements(root);
                AppendSymbol(root);
            }            
        }

        /// <summary>
        /// Compiles a while statement.
        /// </summary>
        void CompileWhile(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "whileStatement", ""));
            //while
            AppendKeyword(root);
            //'('
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            //')'
            AppendSymbol(root);
            //'{'
            AppendSymbol(root);
            //statements
            CompileStatements(root);
            //'}'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles an do statement.
        /// </summary>
        void CompileDo(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "doStatement", ""));
            //do
            AppendKeyword(root);
            //subroutineCall
            CompileSubroutineCall(root);
            //';'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles a return statement.
        /// </summary>
        void CompileReturn(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "returnStatement", ""));
            //return
            AppendKeyword(root);
            //expression?
            if(!(current.Type == TokenType.SYMBOL && current.Symbol == ';'))
            {
                CompileExpression(root);
            }
            //';'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles an expression.
        /// </summary>
        void CompileExpression(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "expression", ""));
            //term
            CompileTerm(root);
            //(op term)*
            while(IsOp(current.Symbol))
            {
                //op
                AppendSymbol(root);
                //term
                CompileTerm(root);
            }
        }

        /// <summary>
        /// Compiles a term. If the current token is an identifier, the routine must distinguish between
        /// a variable, an array entry, or a subroutine call. A single look-ahead token, which may be one
        /// of '[', '(', or '.', suffices to distinguish between the possibilities. Any other token is not
        /// part of this term and should not be advanced over.
        /// </summary>
        void CompileTerm(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "term", ""));
            //unaryOp Term
            if(current.Type == TokenType.SYMBOL && IsUnaryOp(current.Symbol))
            {
                AppendSymbol(root);
                CompileTerm(root);
            }
            //'('expression')'
            else if(current.Type == TokenType.SYMBOL && current.Symbol == '(')
            {
                AppendSymbol(root);
                CompileExpression(root);
                AppendSymbol(root);
            }
            //keywordConstant
            else if(current.Type == TokenType.KEYWORD && 
            (current.Keyword == Keyword.THIS || current.Keyword == Keyword.NULL ||
            current.Keyword == Keyword.TRUE || current.Keyword == Keyword.FALSE))
            {
                AppendKeyword(root);
            }
            //integerConstant
            else if(current.Type == TokenType.INT_CONST)
            {
                AppendIntVal(root);
            }
            //stringConstant
            else if(current.Type == TokenType.STRING_CONST)
            {
                AppendStrVal(root);
            }
            //identifier branch
            else if(current.Type == TokenType.IDENTIFIER)
            {
                //look ahead
                Token next = tokens[tokens.IndexOf(current) + 1];
                //array
                if(next.Type == TokenType.SYMBOL && next.Symbol == '[')
                {
                    //varName
                    AppendIdentifier(root);
                    //'['
                    AppendSymbol(root);
                    //expression
                    CompileExpression(root);
                    //']'
                    AppendSymbol(root);
                }
                //subroutineCall
                else if(next.Type == TokenType.SYMBOL && (next.Symbol == '(' || next.Symbol == '.'))
                {
                    CompileSubroutineCall(root);
                }
                //varName
                else
                {
                    AppendIdentifier(root);
                }
            }
        }

        /// <summary>
        /// Compiles a (possibly empty) comma-separated list of expressions.
        /// </summary>
        void CompileExpressionList(XmlNode parent)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "expressionList", ""));
            if(current.Type == TokenType.SYMBOL && current.Symbol == ')')
                return;
            //expression
            CompileExpression(root);
            //(',' expression)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                AppendSymbol(root);
                CompileExpression(root);
            }
        }

        void CompileSubroutineCall(XmlNode parent)
        {
            //look ahead
            Token next = tokens[tokens.IndexOf(current) + 1];

            //(className|varName).subroutineName'('expressionList')'
            if(next.Type == TokenType.SYMBOL && next.Symbol == '.')
            {
                AppendIdentifier(parent);
                AppendSymbol(parent);
                AppendIdentifier(parent);
                AppendSymbol(parent);
                CompileExpressionList(parent);
                AppendSymbol(parent);
            }
            //subroutineName'('expressionList')'
            else
            {
                AppendIdentifier(parent);
                AppendSymbol(parent);
                CompileExpressionList(parent);
                AppendSymbol(parent);
            }
        }

        void CompileType(XmlNode parent)
        {
            if(current.Type == TokenType.KEYWORD)
                AppendKeyword(parent);
            else
                AppendIdentifier(parent);
        }

        void AppendKeyword(XmlNode parent)
        {
            XmlNode child = document.CreateNode(XmlNodeType.Element, "keyword", "");
            child.InnerText = Enum.GetName(typeof(Keyword), current.Keyword).ToLower();
            parent.AppendChild(child);
            Advance();
        }

        void AppendSymbol(XmlNode parent)
        {
            XmlNode child = document.CreateNode(XmlNodeType.Element, "symbol", "");
            child.InnerText = current.Symbol.ToString();
            parent.AppendChild(child);
            Advance();
        }

        void AppendIdentifier(XmlNode parent)
        {
            XmlNode child = document.CreateNode(XmlNodeType.Element, "identifier", "");
            child.InnerText = current.Identifier;
            parent.AppendChild(child);
            Advance();
        }

        void AppendIntVal(XmlNode parent)
        {
            XmlNode child = document.CreateNode(XmlNodeType.Element, "integerConstant", "");
            child.InnerText = current.IntVal.ToString();
            parent.AppendChild(child);
            Advance();
        }

        void AppendStrVal(XmlNode parent)
        {
            XmlNode child = document.CreateNode(XmlNodeType.Element, "stringConstant", "");
            child.InnerText = current.StringVal;
            parent.AppendChild(child);
            Advance();
        }

        void Advance()
        {
            if(tokens.IndexOf(current) + 1 < tokens.Count)
                current = tokens[tokens.IndexOf(current) + 1];
        }

        bool IsOp(char symbol)
        {
            if(symbol == '+' || symbol == '-' || symbol == '*' || symbol == '/' || symbol == '&' || symbol == '|' ||
            symbol == '<' || symbol == '>' || symbol == '=')
                return true;
            return false;
        }

        bool IsUnaryOp(char symbol)
        {
            if(symbol == '-' || symbol == '~')
                return true;
            return false;
        }

        /// <summary>
        /// Save the document to a file and auto-indent the output.
        /// </summary>
        /// <param name="path">Complete file path without extension</param>
        public void WriteXML(string path)
        {
            string xmlPath = path.Split('.')[0] + ".xml";
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(xmlPath, settings);
            try
            {
                document.Save(writer);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}