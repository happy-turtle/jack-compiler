namespace JackCompiler
{
    enum Segment { CONST, ARG, LOCAL, STATIC, THIS, THAT, POINTER, TEMP };
    enum Command { ADD, SUB, NEG, EQ, GT, LT, AND, OR, NOT };

    class VMWriter
    {
        VMWriter(string path)
        {

        }

        void WritePush(Segment segment, int index)
        {

        }

        void WritePop(Segment segment, int index)
        {

        }

        void WriteArithmetic(Command command)
        {

        }

        void WriteLabel(string label)
        {

        }

        void WriteGoto(string label)
        {

        }

        void WriteIf(string label)
        {

        }

        void WriteCall(string name, int nArgs)
        {

        }

        void WriteFunction(string name, int nLocals)
        {

        }

        void WriteReturn()
        {

        }

        void Close()
        {
            
        }
    }
}