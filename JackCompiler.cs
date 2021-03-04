using System;
using System.IO;
using System.Collections.Generic;

namespace JackCompiler
{
    /// <summary>
    /// The top-most / main module
    /// Input: a single fileName.jack, or a directory containing 0 or more such files
    /// For each file, goes through the following logic:
    /// 1. Creates a JackTokenizer from fileName.jack
    /// 2. Creates and uses a CompilationEngine, a SymbolTable and a VMWriter and compiles the input JackTokenizer into VM code.
    /// 3. Creates an output file named fileName.vm and writes the VM code into this file.
    /// </summary>
    class JackCompiler
    {
        static void Main(string[] args)
        {            if (args.Length == 0)
            {
                Console.WriteLine("No argument given. Path for input file must be specified.");
                return;
            }
            
            FileAttributes attributes;
            try
            {
                attributes = File.GetAttributes(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            if(attributes.HasFlag(FileAttributes.Directory))
            {
                string[] files = Directory.GetFiles(args[0], "*.jack");
                foreach(string filePath in files)
                {
                    AnalyzeFile(filePath);
                }
            }
            else
            {
                AnalyzeFile(args[0]);
            }
        }

        private static void AnalyzeFile(string filePath)
        {            
            string[] lines = new string[0];
            try
            {
                lines = StripComments(File.ReadAllLines(filePath));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if(lines.Length <= 0)
                return;

            JackTokenizer tokenizer = new JackTokenizer(lines);
            List<Token> tokens = new List<Token>();

            while(tokenizer.HasMoreTokens())
            {
                TokenType tokenType = tokenizer.GetTokenType();

                if(tokenType == TokenType.KEYWORD)
                    tokens.Add(new Token(TokenType.KEYWORD, tokenizer.GetKeyword()));
                else if(tokenType == TokenType.IDENTIFIER)
                    tokens.Add(new Token(TokenType.IDENTIFIER, tokenizer.GetIdentifier()));
                else if(tokenType == TokenType.INT_CONST)
                    tokens.Add(new Token(TokenType.INT_CONST, tokenizer.GetIntVal()));
                else if(tokenType == TokenType.STRING_CONST)
                    tokens.Add(new Token(TokenType.STRING_CONST, tokenizer.GetStringVal()));
                else if(tokenType == TokenType.SYMBOL)
                    tokens.Add(new Token(TokenType.SYMBOL, tokenizer.GetSymbol()));

                tokenizer.Advance();
            }

            VMWriter vmWriter = new VMWriter();
            CompilationEngine compilationEngine = new CompilationEngine(tokens, vmWriter);
            vmWriter.Close(filePath);
        }

        private static string[] StripComments(string[] lines)
        {
            List<string> lineList = new List<string>();
            foreach (string line in lines)
            {
                if (!line.TrimStart(' ').StartsWith("//") && !string.IsNullOrWhiteSpace(line) 
                && !line.TrimStart(' ').StartsWith("/*") && !line.TrimStart(' ').StartsWith("*"))
                {
                    string codeLine = line.Split("//")[0];
                    lineList.Add(codeLine);
                }
            }
            return lineList.ToArray();
        }
    }
}
