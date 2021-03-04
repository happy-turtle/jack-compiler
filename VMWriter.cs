using System;
using System.IO;
using System.Collections.Generic;


namespace JackCompiler
{
    enum Segment { CONST, ARG, LOCAL, STATIC, THIS, THAT, POINTER, TEMP };
    enum Command { ADD, SUB, NEG, EQ, GT, LT, AND, OR, NOT };

    /// <summary>
    /// Writes the virtual machine code to the output file.
    /// </summary>
    class VMWriter
    {
        List<string> vmCommands = new List<string>();

        const string VMFileExtension = ".vm";

        public void WritePush(Segment segment, int index)
        {
            vmCommands.Add("push " + Enum.GetName(typeof(Segment), segment).ToLower() + " " + index);
        }

        public void WritePop(Segment segment, int index)
        {
            vmCommands.Add("pop " + Enum.GetName(typeof(Segment), segment).ToLower() + " " + index);
        }

        public void WriteArithmetic(Command command)
        {
            vmCommands.Add(Enum.GetName(typeof(Command), command).ToLower());
        }

        public void WriteLabel(string label)
        {
            vmCommands.Add("(" + label + ")");
        }

        public void WriteGoto(string label)
        {
            vmCommands.Add("goto " + label);
        }

        public void WriteIf(string label)
        {
            vmCommands.Add("if-goto " + label);
        }

        public void WriteCall(string name, int nArgs)
        {
            vmCommands.Add("call " + name + " " + nArgs);
        }

        public void WriteFunction(string name, int nLocals)
        {
            vmCommands.Add("function " + name + " " + nLocals);
        }

        public void WriteReturn()
        {
            vmCommands.Add("return");
        }

        public void Close(string path)
        {
            try
            {
                string filePath = path.Split('.')[0] + VMFileExtension;
                File.WriteAllLines(filePath, vmCommands);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}