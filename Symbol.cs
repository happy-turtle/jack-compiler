namespace JackCompiler
{
    enum SymbolKind { STATIC, FIELD, ARG, VAR, NONE };

    class Symbol
    {

        public string Type { get; private set; }
        public SymbolKind Kind { get; private set; }
        public int Index { get; private set; }

        public Symbol(string type, SymbolKind kind, int index)
        {
            this.Type = type;
            this.Kind = kind;
            this.Index = index;
        }
    }
}