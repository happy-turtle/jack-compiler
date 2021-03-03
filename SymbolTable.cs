namespace JackCompiler
{
    enum SymbolKind { STATIC, FIELD, ARG, VAR, NONE };
    class SymbolTable
        {
        /// <summary>
        /// Starts a new subroutine scope.
        /// </summary>
        void StartSubroutine()
        {

        }

        /// <summary>
        /// Defines a new identifier of the given name, type and kind,
        /// and assigns it a running index. STATIC and FIELD identifiers
        /// have a class scope, while ARG and VAR identifiers have a
        /// subroutine scope.
        /// </summary>
        void Define(string name, string type, SymbolKind kind)
        {

        }

        /// <summary>
        /// Returns the number of variables of the given kind already
        /// defined in the current scope.
        /// </summary>
        int VarCount(SymbolKind kind)
        {
            return 0;
        }

        /// <summary>
        /// Returns the kind of the named identifier in the current scope.
        /// If the identifier is unknown in the current scope , returns NONE.
        /// </summary>
        SymbolKind KindOf(string name)
        {
            return SymbolKind.NONE;
        }

        /// <summary>
        /// Returns the type of the named identifier in the current scope.
        /// </summary>
        string TypeOf(string name)
        {
            return null;
        }

        /// <summary>
        /// Returns the index assigned to the named identifier.
        /// </summary>
        int IndexOf(string name)
        {
            return 0;
        }
    }
}