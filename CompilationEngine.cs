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

        /// <summary>
        /// Creates a new compilation engine with the given input and output.
        /// The next routine called must be CompileClass.
        /// </summary>
        public CompilationEngine(List<Token> tokenList)
        {
            document = new XmlDocument();
            tokens = tokenList;
        }

        /// <summary>
        /// Compiles a complete class.
        /// </summary>
        void CompileClass()
        {

        }

        /// <summary>
        /// Compiles a static variable declaration or a field declaration.
        /// </summary>
        void CompileClassVarDec()
        {

        }

        /// <summary>
        /// Compiles a complete method, function or constructor.
        /// </summary>
        void CompileSubroutineDec()
        {

        }

        /// <summary>
        /// Compiles a (possibly empty) parameter list. Does not handle the enclosing "()".
        /// </summary>
        void CompileParameterList()
        {

        }

        /// <summary>
        /// Compiles a subroutine's body.
        /// </summary>
        void CompileSubroutineBody()
        {

        }

        /// <summary>
        /// Compiles a var declaration.
        /// </summary>
        void CompileVarDec()
        {

        }

        /// <summary>
        /// Compiles a sequence of statements. Does not handle the enclosing "()".
        /// </summary>
        void CompileStatements()
        {

        }

        /// <summary>
        /// Compiles a let statement.
        /// </summary>
        void CompileLet()
        {

        }

        /// <summary>
        /// Compiles an if statement, possibly with a trailing else clause.
        /// </summary>
        void CompileIf()
        {

        }

        /// <summary>
        /// Compiles a while statement.
        /// </summary>
        void CompileWhile()
        {

        }

        /// <summary>
        /// Compiles an do statement.
        /// </summary>
        void CompileDo()
        {

        }

        /// <summary>
        /// Compiles a return statement.
        /// </summary>
        void CompileReturn()
        {

        }

        /// <summary>
        /// Save the document to a file and auto-indent the output.
        /// </summary>
        /// <param name="path">Complete file path without extension</param>
        public void WriteXML(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(path + ".xml", settings);
            document.Save(writer);
        }
    }
}