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
        VMWriter vmWriter;
        SymbolTable symbolTable;
        string className;
        int labelIndex = 0;

        /// <summary>
        /// Creates a new compilation engine with the given input and output.
        /// The next routine called must be CompileClass.
        /// </summary>
        public CompilationEngine(List<Token> tokenList, VMWriter writer)
        {
            document = new XmlDocument();
            tokens = tokenList;
            current = tokens[0];
            vmWriter = writer;
            symbolTable = new SymbolTable();
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
            className = current.Identifier;
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
                SymbolKind kind;
                if(current.Keyword == Keyword.STATIC)
                    kind = SymbolKind.STATIC;
                else
                    kind = SymbolKind.FIELD;
                AppendKeyword(root);
                //type
                string type = CompileType(root);
                //varName
                symbolTable.Define(current.Identifier, type, kind);
                AppendIdentifier(root);
                //(, varName)*
                while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
                {
                    AppendSymbol(root);
                    symbolTable.Define(current.Identifier, type, kind);
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
                symbolTable.StartSubroutine();
                Keyword keyword = current.Keyword;
                //first argument of a method is always this
                if(current.Keyword == Keyword.METHOD)
                    symbolTable.Define("this",  className, SymbolKind.ARG);
                XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "subroutineDec", ""));
                //'constructor'|'function'|'method'
                AppendKeyword(root);
                //'void|type'
                string type;
                if(current.Keyword == Keyword.VOID)
                {
                    type = "void";
                    AppendKeyword(root);
                }
                else
                    type = CompileType(root);
                //subroutineName
                string name = className + "." + current.Identifier;
                AppendIdentifier(root);
                //'('parameterList')'
                AppendSymbol(root);
                CompileParameterList(root);
                AppendSymbol(root);
                //subroutineBody
                CompileSubroutineBody(root, keyword, name);
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
            string type = CompileType(root);
            symbolTable.Define(current.Identifier, type, SymbolKind.ARG);
            AppendIdentifier(root);

            //(',' type varName)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                AppendSymbol(root);
                type = CompileType(root);
                symbolTable.Define(current.Identifier, type, SymbolKind.ARG);
                AppendIdentifier(root);
            }
        }

        /// <summary>
        /// Compiles a subroutine's body.
        /// </summary>
        void CompileSubroutineBody(XmlNode parent, Keyword keyword, string functionName)
        {
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "subroutineBody", ""));
            //'{'
            AppendSymbol(root);
            //varDec*
            while(current.Keyword == Keyword.VAR)
                CompileVarDec(root);
            //VM function declaration
            vmWriter.WriteFunction(functionName, symbolTable.VarCount(SymbolKind.VAR));
            //METHOD and CONSTRUCTOR need to load this pointer
            if (keyword == Keyword.METHOD)
            {
                //A Jack method with k arguments is compiled into a VM function that operates on k + 1 arguments.
                //The first argument always refers to 'this'.
                vmWriter.WritePush(Segment.ARG, 0);
                vmWriter.WritePop(Segment.POINTER, 0);
            }
            else if (keyword == Keyword.CONSTRUCTOR){
                //A Jack function or constructor with k arguments is compiled into a VM function that operates on k arguments.
                vmWriter.WritePush(Segment.CONST, symbolTable.VarCount(SymbolKind.FIELD));
                vmWriter.WriteCall("Memory.alloc", 1);
                vmWriter.WritePop(Segment.POINTER, 0);
            }
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
            string type = CompileType(root);
            //varName (, varName)*
            symbolTable.Define(current.Identifier, type, SymbolKind.VAR);
            AppendIdentifier(root);
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                AppendSymbol(root);
                symbolTable.Define(current.Identifier, type, SymbolKind.VAR);
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
            bool isArray = false;
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "letStatement", ""));
            //'let'
            AppendKeyword(root);
            //varName
            string name = current.Identifier;
            AppendIdentifier(root);
            //'['expression']'
            if(current.Symbol == '[')
            {
                isArray = true;
                AppendSymbol(root);            
                //push base address of array variable into stack
                vmWriter.WritePush(symbolTable.SegmentOf(name),symbolTable.IndexOf(name));
                CompileExpression(root);
                AppendSymbol(root);
                //add offset to base
                vmWriter.WriteArithmetic(Command.ADD);
            }
            //'='
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            if(isArray)
            {
                //pop expression value to temp
                vmWriter.WritePop(Segment.TEMP, 0);
                //pop base + index to that
                vmWriter.WritePop(Segment.POINTER, 1);
                //pop expression value to *(base + index)
                vmWriter.WritePush(Segment.TEMP, 0);
                vmWriter.WritePop(Segment.THAT, 0);
            }   
            else
            {
                //pop expression value
                vmWriter.WritePop(symbolTable.SegmentOf(name), symbolTable.IndexOf(name));                
            }
            //';'
            AppendSymbol(root);
        }

        /// <summary>
        /// Compiles an if statement, possibly with a trailing else clause.
        /// </summary>
        void CompileIf(XmlNode parent)
        {
            string elseLabel = NewLabel("IF");
            string endLabel = NewLabel("IF");

            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "ifStatement", ""));
            //if
            AppendKeyword(root);
            //'('
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            //')'
            AppendSymbol(root);
            //if ~condition goto else
            vmWriter.WriteArithmetic(Command.NOT);
            vmWriter.WriteIf(elseLabel);
            //'{'
            AppendSymbol(root);
            //statements
            CompileStatements(root);
            //'}'
            AppendSymbol(root);
            //if condition goto end
            vmWriter.WriteGoto(endLabel);
            //else'{'statements'}'
            vmWriter.WriteLabel(elseLabel);
            if(current.Keyword == Keyword.ELSE)
            {
                AppendKeyword(root);
                AppendSymbol(root);
                CompileStatements(root);
                AppendSymbol(root);
            }            
            vmWriter.WriteLabel(endLabel);
        }

        /// <summary>
        /// Compiles a while statement.
        /// </summary>
        void CompileWhile(XmlNode parent)
        {
            string startLabel = NewLabel("WhileStart");
            string endLabel = NewLabel("WhileEnd");

            //start of the loop
            vmWriter.WriteLabel(startLabel);

            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "whileStatement", ""));
            //while
            AppendKeyword(root);
            //'('
            AppendSymbol(root);
            //expression
            CompileExpression(root);
            //')'
            AppendSymbol(root);
            //if ~condition go to end
            vmWriter.WriteArithmetic(Command.NOT);
            vmWriter.WriteIf(endLabel);
            //'{'
            AppendSymbol(root);
            //statements
            CompileStatements(root);
            //'}'
            AppendSymbol(root);
            //if condition go to start or continue
            vmWriter.WriteGoto(startLabel);
            vmWriter.WriteLabel(endLabel);
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
            //pop return value
            vmWriter.WritePop(Segment.TEMP, 0);
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
            else
            {
                vmWriter.WritePush(Segment.CONST, 0);
            }
            //';'
            AppendSymbol(root);
            vmWriter.WriteReturn();
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
                switch(current.Symbol)
                {
                    case '+':
                        vmWriter.WriteArithmetic(Command.ADD);
                        break;
                    case '-':
                        vmWriter.WriteArithmetic(Command.SUB);
                        break;
                    case '*':
                        vmWriter.WriteCall("Math.multiply", 2);
                        break;
                    case '/':
                        vmWriter.WriteCall("Math.divide", 2);
                        break;
                    case '<':
                        vmWriter.WriteArithmetic(Command.LT);
                        break;
                    case '>':
                        vmWriter.WriteArithmetic(Command.GT);
                        break;
                    case '=':
                        vmWriter.WriteArithmetic(Command.EQ);
                        break;
                    case '&':
                        vmWriter.WriteArithmetic(Command.AND);
                        break;
                    case '|':
                        vmWriter.WriteArithmetic(Command.OR);
                        break;
                }
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
                if(current.Symbol == '-')
                    vmWriter.WriteArithmetic(Command.NEG);
                else
                    vmWriter.WriteArithmetic(Command.NOT);
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
                switch(current.Keyword)
                {
                    case Keyword.THIS:
                        vmWriter.WritePush(Segment.POINTER, 0);
                        break;
                    case Keyword.NULL:
                        vmWriter.WritePush(Segment.CONST, 0);                        
                        break;
                    case Keyword.TRUE:
                        vmWriter.WritePush(Segment.CONST, 0); 
                        vmWriter.WriteArithmetic(Command.NOT); //~0 = -1
                        break;
                    case Keyword.FALSE:
                        vmWriter.WritePush(Segment.CONST, 0);
                        break;
                }
                AppendKeyword(root);
            }
            //integerConstant
            else if(current.Type == TokenType.INT_CONST)
            {
                vmWriter.WritePush(Segment.CONST, current.IntVal);
                AppendIntVal(root);
            }
            //stringConstant
            else if(current.Type == TokenType.STRING_CONST)
            {
                string str = current.StringVal;
                //new string
                vmWriter.WritePush(Segment.CONST, str.Length);
                vmWriter.WriteCall("String.new", 1);
                //append each char
                foreach(char ch in str.ToCharArray())
                {
                    vmWriter.WritePush(Segment.CONST, (int)ch);
                    vmWriter.WriteCall("String.appendChar", 2); 
                }
                AppendStrVal(root);
            }
            //identifier branch
            else if(current.Type == TokenType.IDENTIFIER)
            {
                string name = current.Identifier;
                //look ahead
                Token next = tokens[tokens.IndexOf(current) + 1];
                //array
                if(next.Type == TokenType.SYMBOL && next.Symbol == '[')
                {
                    //push base address of array variable into stack
                    vmWriter.WritePush(symbolTable.SegmentOf(name),symbolTable.IndexOf(name));
                    //varName
                    AppendIdentifier(root);
                    //'['
                    AppendSymbol(root);
                    //expression
                    CompileExpression(root);
                    //']'   
                    AppendSymbol(root);
                    //base+offset
                    vmWriter.WriteArithmetic(Command.ADD);
                    //pop into 'that' pointer
                    vmWriter.WritePop(Segment.POINTER,1);
                    //push *(base+index) onto stack
                    vmWriter.WritePush(Segment.THAT,0);
                }
                //subroutineCall
                else if(next.Type == TokenType.SYMBOL && (next.Symbol == '(' || next.Symbol == '.'))
                {
                    CompileSubroutineCall(root);
                }
                //varName
                else
                {
                    vmWriter.WritePush(symbolTable.SegmentOf(name), symbolTable.IndexOf(name));
                    AppendIdentifier(root);
                }
            }
        }

        /// <summary>
        /// Compiles a (possibly empty) comma-separated list of expressions.
        /// </summary>
        int CompileExpressionList(XmlNode parent)
        {
            int nArgs = 0;
            XmlNode root = parent.AppendChild(document.CreateNode(XmlNodeType.Element, "expressionList", ""));
            if(current.Type == TokenType.SYMBOL && current.Symbol == ')')
                return nArgs;
            //expression
            nArgs++;
            CompileExpression(root);
            //(',' expression)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                nArgs++;
                AppendSymbol(root);
                CompileExpression(root);
            }
            return nArgs;
        }

        void CompileSubroutineCall(XmlNode parent)
        {
            int nArgs = 0;
            string name;
            //look ahead
            Token next = tokens[tokens.IndexOf(current) + 1];

            //(className|varName).subroutineName'('expressionList')'
            if(next.Type == TokenType.SYMBOL && next.Symbol == '.')
            {
                name = current.Identifier;
                AppendIdentifier(parent);
                AppendSymbol(parent);
                //subroutineName
                string subroutineName = current.Identifier;
                string type = symbolTable.TypeOf(name);
                if(string.IsNullOrEmpty(type))
                {
                    name = name + "." + subroutineName;
                }
                else
                {
                    nArgs = 1;
                    //push variable onto stack
                    vmWriter.WritePush(symbolTable.SegmentOf(name), symbolTable.IndexOf(name));
                    name = symbolTable.TypeOf(name) + "." + subroutineName;
                }
                AppendIdentifier(parent);
                AppendSymbol(parent);
                nArgs += CompileExpressionList(parent);
                AppendSymbol(parent);
                //pointer
                vmWriter.WritePush(Segment.POINTER, 0);
                //call
                vmWriter.WriteCall(name, nArgs);
            }
            //subroutineName'('expressionList')'
            else
            {
                name = current.Identifier;
                AppendIdentifier(parent);
                AppendSymbol(parent);
                nArgs = CompileExpressionList(parent) + 1;
                AppendSymbol(parent);
                //pointer
                vmWriter.WritePush(Segment.POINTER, 0);
                //call
                vmWriter.WriteCall(className + "." + name, nArgs);
            }
        }

        string CompileType(XmlNode parent)
        {
            string type;
            if(current.Type == TokenType.KEYWORD)
            {
                type = Enum.GetName(typeof(Keyword), current.Keyword).ToLower();
                AppendKeyword(parent);
            }
            else
            {
                type = current.Identifier;
                AppendIdentifier(parent);
            }
            return type;
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

        string NewLabel(string name)
        {
            string label = name + labelIndex;
            labelIndex++;
            return label;
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