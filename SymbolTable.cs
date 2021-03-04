using System.Collections.Generic;

namespace JackCompiler
{
    class SymbolTable
    {
        Dictionary<string, Symbol> classSymbols = new Dictionary<string, Symbol>();
        Dictionary<string, Symbol> subroutineSymbols;
        Dictionary<SymbolKind, int> indices = new Dictionary<SymbolKind, int>()
        {
            { SymbolKind.ARG, 0 },
            { SymbolKind.VAR, 0 },
            { SymbolKind.STATIC, 0 },
            { SymbolKind.FIELD, 0 }
        };

        /// <summary>
        /// Starts a new subroutine scope.
        /// </summary>
        public void StartSubroutine()
        {
            subroutineSymbols = new Dictionary<string, Symbol>();
            indices[SymbolKind.ARG] = 0;
            indices[SymbolKind.VAR] = 0;
        }

        /// <summary>
        /// Defines a new identifier of the given name, type and kind,
        /// and assigns it a running index. STATIC and FIELD identifiers
        /// have a class scope, while ARG and VAR identifiers have a
        /// subroutine scope.
        /// </summary>
        public void Define(string name, string type, SymbolKind kind)
        {
            if(kind == SymbolKind.STATIC || kind == SymbolKind.FIELD)
            {
                int index = indices[kind];
                classSymbols.Add(name, new Symbol(type, kind, index));
                indices[kind] = index + 1;
            }
            else if(kind == SymbolKind.ARG || kind == SymbolKind.VAR)
            {
                int index = indices[kind];
                subroutineSymbols.Add(name, new Symbol(type, kind, index));
                indices[kind] = index + 1;
            }
        }

        /// <summary>
        /// Returns the number of variables of the given kind already
        /// defined in the current scope.
        /// </summary>
        public int VarCount(SymbolKind kind)
        {
            return indices[kind];
        }

        /// <summary>
        /// Returns the kind of the named identifier in the current scope.
        /// If the identifier is unknown in the current scope return NONE.
        /// </summary>
        public SymbolKind KindOf(string name)
        {
            Symbol symbol;
            if(classSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Kind;
            }
            else if(subroutineSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Kind;
            }
            return SymbolKind.NONE;
        }

        /// <summary>
        /// Returns the segment of the named identifier.
        /// </summary>
        public Segment SegmentOf(string name)
        {
            SymbolKind kind = KindOf(name);
            switch(kind)
            {
                case SymbolKind.VAR:
                    return Segment.LOCAL;
                case SymbolKind.ARG:
                    return Segment.ARG;
                case SymbolKind.FIELD:
                    return Segment.THIS;
                case SymbolKind.STATIC:
                    return Segment.STATIC;
                default:
                    return Segment.ARG;
            }
        }

        /// <summary>
        /// Returns the type of the named identifier in the current scope.
        /// </summary>
        public string TypeOf(string name)
        {
            Symbol symbol;
            if(classSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Type;
            }
            else if(subroutineSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Type;
            }
            return null;
        }

        /// <summary>
        /// Returns the index assigned to the named identifier.
        /// </summary>
        public int IndexOf(string name)
        {
            Symbol symbol;
            if(classSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Index;
            }
            else if(subroutineSymbols.TryGetValue(name, out symbol))
            {
                return symbol.Index;
            }
            return -1;
        }
    }
}