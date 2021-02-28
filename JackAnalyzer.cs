using System;
using System.IO;

namespace JackCompiler
{
    class JackAnalyzer
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

            codeWriter.WriteInit();

            if(attributes.HasFlag(FileAttributes.Directory))
            {
                string[] files = Directory.GetFiles(args[0], "*.vm");
                foreach(string filePath in files)
                {
                    TranslateFile(filePath);
                } 
                codeWriter.Close(args[0], true);
            }
            else
            {
                TranslateFile(args[0]);
                codeWriter.Close(args[0]);
            }
        }
    }
}
