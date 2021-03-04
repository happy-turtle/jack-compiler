using System;
using System.Collections.Generic;

namespace JackCompiler
{
    /// <summary>
    /// Generates the compiler's output.
    /// </summary>
    class CompilationEngine
    {
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
            //class
            Advance();
            //class name
            className = current.Identifier;
            Advance();
            //'{'
            Advance();
            //classVarDec*
            CompileClassVarDec();
            //subroutineDec*
            CompileSubroutineDec();
            //'}'
            Advance();
        }

        /// <summary>
        /// Compiles a static variable declaration or a field declaration.
        /// </summary>
        void CompileClassVarDec()
        {
            while(current.Keyword == Keyword.STATIC || current.Keyword == Keyword.FIELD)
            {
                //'static'|'field'
                SymbolKind kind;
                if(current.Keyword == Keyword.STATIC)
                    kind = SymbolKind.STATIC;
                else
                    kind = SymbolKind.FIELD;
                Advance();
                //type
                string type = CompileType();
                //varName
                symbolTable.Define(current.Identifier, type, kind);
                Advance();
                //(, varName)*
                while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
                {
                    Advance();
                    symbolTable.Define(current.Identifier, type, kind);
                    Advance();
                }
                //';'
                Advance();
            }
        }

        /// <summary>
        /// Compiles a complete method, function or constructor.
        /// </summary>
        void CompileSubroutineDec()
        {            
            while(current.Keyword == Keyword.FUNCTION || current.Keyword == Keyword.METHOD ||
                current.Keyword == Keyword.CONSTRUCTOR)
            {
                symbolTable.StartSubroutine();
                Keyword keyword = current.Keyword;
                //first argument of a method is always this
                if(current.Keyword == Keyword.METHOD)
                    symbolTable.Define("this",  className, SymbolKind.ARG);
                //'constructor'|'function'|'method'
                Advance();
                //'void|type'
                string type;
                if(current.Keyword == Keyword.VOID)
                {
                    type = "void";
                    Advance();
                }
                else
                    type = CompileType();
                //subroutineName
                string name = className + "." + current.Identifier;
                Advance();
                //'('parameterList')'
                Advance();
                CompileParameterList();
                Advance();
                //subroutineBody
                CompileSubroutineBody(keyword, name);
            }
        }

        /// <summary>
        /// Compiles a (possibly empty) parameter list. Does not handle the enclosing "()".
        /// </summary>
        void CompileParameterList()
        {
            if(current.Type == TokenType.SYMBOL && current.Symbol == ')')
                return;
            
            //type varName
            string type = CompileType();
            symbolTable.Define(current.Identifier, type, SymbolKind.ARG);
            Advance();

            //(',' type varName)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                Advance();
                type = CompileType();
                symbolTable.Define(current.Identifier, type, SymbolKind.ARG);
                Advance();
            }
        }

        /// <summary>
        /// Compiles a subroutine's body.
        /// </summary>
        void CompileSubroutineBody(Keyword keyword, string functionName)
        {
            //'{'
            Advance();
            //varDec*
            while(current.Keyword == Keyword.VAR)
                CompileVarDec();
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
            CompileStatements();
            //'}'
            Advance();
        }

        /// <summary>
        /// Compiles a var declaration.
        /// </summary>
        void CompileVarDec()
        {
            //var
            Advance();
            //type
            string type = CompileType();
            //varName (, varName)*
            symbolTable.Define(current.Identifier, type, SymbolKind.VAR);
            Advance();
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                Advance();
                symbolTable.Define(current.Identifier, type, SymbolKind.VAR);
                Advance();
            }
            //';'
            Advance();
        }

        /// <summary>
        /// Compiles a sequence of statements. Does not handle the enclosing "()".
        /// </summary>
        void CompileStatements()
        {
            while(current.Keyword == Keyword.LET ||
                current.Keyword == Keyword.IF ||
                current.Keyword == Keyword.WHILE ||
                current.Keyword == Keyword.DO ||
                current.Keyword == Keyword.RETURN)
            {
                if(current.Keyword == Keyword.LET)
                    CompileLet();
                else if(current.Keyword == Keyword.IF)
                    CompileIf();
                else if(current.Keyword == Keyword.WHILE)
                    CompileWhile();
                else if(current.Keyword == Keyword.DO)
                    CompileDo();
                else if(current.Keyword == Keyword.RETURN)
                    CompileReturn();
            }
        }

        /// <summary>
        /// Compiles a let statement.
        /// </summary>
        void CompileLet()
        {
            bool isArray = false;
            //'let'
            Advance();
            //varName
            string name = current.Identifier;
            Advance();
            //'['expression']'
            if(current.Symbol == '[')
            {
                isArray = true;
                Advance();            
                //push base address of array variable into stack
                vmWriter.WritePush(symbolTable.SegmentOf(name),symbolTable.IndexOf(name));
                CompileExpression();
                Advance();
                //add offset to base
                vmWriter.WriteArithmetic(Command.ADD);
            }
            //'='
            Advance();
            //expression
            CompileExpression();
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
            Advance();
        }

        /// <summary>
        /// Compiles an if statement, possibly with a trailing else clause.
        /// </summary>
        void CompileIf()
        {
            string elseLabel = NewLabel("IF");
            string endLabel = NewLabel("IF");

            //if
            Advance();
            //'('
            Advance();
            //expression
            CompileExpression();
            //')'
            Advance();
            //if ~condition goto else
            vmWriter.WriteArithmetic(Command.NOT);
            vmWriter.WriteIf(elseLabel);
            //'{'
            Advance();
            //statements
            CompileStatements();
            //'}'
            Advance();
            //if condition goto end
            vmWriter.WriteGoto(endLabel);
            //else'{'statements'}'
            vmWriter.WriteLabel(elseLabel);
            if(current.Keyword == Keyword.ELSE)
            {
                Advance();
                Advance();
                CompileStatements();
                Advance();
            }            
            vmWriter.WriteLabel(endLabel);
        }

        /// <summary>
        /// Compiles a while statement.
        /// </summary>
        void CompileWhile()
        {
            string startLabel = NewLabel("WhileStart");
            string endLabel = NewLabel("WhileEnd");

            //start of the loop
            vmWriter.WriteLabel(startLabel);

            //while
            Advance();
            //'('
            Advance();
            //expression
            CompileExpression();
            //')'
            Advance();
            //if ~condition go to end
            vmWriter.WriteArithmetic(Command.NOT);
            vmWriter.WriteIf(endLabel);
            //'{'
            Advance();
            //statements
            CompileStatements();
            //'}'
            Advance();
            //if condition go to start or continue
            vmWriter.WriteGoto(startLabel);
            vmWriter.WriteLabel(endLabel);
        }

        /// <summary>
        /// Compiles an do statement.
        /// </summary>
        void CompileDo()
        {
            //do
            Advance();
            //subroutineCall
            CompileSubroutineCall();
            //';'
            Advance();
            //pop return value
            vmWriter.WritePop(Segment.TEMP, 0);
        }

        /// <summary>
        /// Compiles a return statement.
        /// </summary>
        void CompileReturn()
        {
            //return
            Advance();
            //expression?
            if(!(current.Type == TokenType.SYMBOL && current.Symbol == ';'))
            {
                CompileExpression();
            }
            else
            {
                vmWriter.WritePush(Segment.CONST, 0);
            }
            //';'
            Advance();
            vmWriter.WriteReturn();
        }

        /// <summary>
        /// Compiles an expression.
        /// </summary>
        void CompileExpression()
        {
            //term
            CompileTerm();
            //(op term)*
            while(IsOp(current.Symbol))
            {
                //op
                char symbol = current.Symbol;
                Advance();
                //term
                CompileTerm();
                
                switch(symbol)
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
            }
        }

        /// <summary>
        /// Compiles a term. If the current token is an identifier, the routine must distinguish between
        /// a variable, an array entry, or a subroutine call. A single look-ahead token, which may be one
        /// of '[', '(', or '.', suffices to distinguish between the possibilities. Any other token is not
        /// part of this term and should not be advanced over.
        /// </summary>
        void CompileTerm()
        {
            //unaryOp Term
            if(current.Type == TokenType.SYMBOL && IsUnaryOp(current.Symbol))
            {
                Advance();
                CompileTerm();
                if(current.Symbol == '-')
                    vmWriter.WriteArithmetic(Command.NEG);
                else
                    vmWriter.WriteArithmetic(Command.NOT);
            }   
            //'('expression')'
            else if(current.Type == TokenType.SYMBOL && current.Symbol == '(')
            {
                Advance();
                CompileExpression();
                Advance();
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
                Advance();
            }
            //integerConstant
            else if(current.Type == TokenType.INT_CONST)
            {
                vmWriter.WritePush(Segment.CONST, current.IntVal);
                Advance();
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
                Advance();
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
                    Advance();
                    //'['
                    Advance();
                    //expression
                    CompileExpression();
                    //']'   
                    Advance();
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
                    CompileSubroutineCall();
                }
                //varName
                else
                {
                    vmWriter.WritePush(symbolTable.SegmentOf(name), symbolTable.IndexOf(name));
                    Advance();
                }
            }
        }

        /// <summary>
        /// Compiles a (possibly empty) comma-separated list of expressions.
        /// </summary>
        int CompileExpressionList()
        {
            int nArgs = 0;
            if(current.Type == TokenType.SYMBOL && current.Symbol == ')')
                return nArgs;
            //expression
            nArgs++;
            CompileExpression();
            //(',' expression)*
            while(current.Type == TokenType.SYMBOL && current.Symbol == ',')
            {
                nArgs++;
                Advance();
                CompileExpression();
            }
            return nArgs;
        }

        void CompileSubroutineCall()
        {
            int nArgs = 0;
            string name;
            //look ahead
            Token next = tokens[tokens.IndexOf(current) + 1];

            //(className|varName).subroutineName'('expressionList')'
            if(next.Type == TokenType.SYMBOL && next.Symbol == '.')
            {
                name = current.Identifier;
                Advance();
                Advance();
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
                Advance();
                Advance();
                nArgs += CompileExpressionList();
                Advance();
                //call
                vmWriter.WriteCall(name, nArgs);
            }
            //subroutineName'('expressionList')'
            else
            {
                name = current.Identifier;
                Advance();
                //pointer
                vmWriter.WritePush(Segment.POINTER, 0);
                Advance();
                nArgs = CompileExpressionList() + 1;
                Advance();
                //call
                vmWriter.WriteCall(className + "." + name, nArgs);
            }
        }

        string CompileType()
        {
            string type;
            if(current.Type == TokenType.KEYWORD)
            {
                type = Enum.GetName(typeof(Keyword), current.Keyword).ToLower();
                Advance();
            }
            else
            {
                type = current.Identifier;
                Advance();
            }
            return type;
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
    }
}