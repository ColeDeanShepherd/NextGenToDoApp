namespace NextGenToDoApp;

public interface ISymbol { }

public record BuiltInSymbol(string Name, IType Type) : ISymbol;

public interface IType { }

public record VoidType() : IType
{
    public static VoidType Instance = new();
}

public record StringType() : IType
{
    public static StringType Instance = new();
}

public record FunctionType(List<IType> ParamTypes, IType ReturnType) : IType;

public static class TypeChecker
{
    public static Dictionary<string, ISymbol> Symbols = new()
    {
        { "ConsoleLog", new BuiltInSymbol("ConsoleLog", new FunctionType([ StringType.Instance ], VoidType.Instance)) }
    };

    public static IType? CheckType(ParseNode parseNode)
    {
        foreach (var child in parseNode.Children)
        {
            CheckType(child);
        }

        IType? type = parseNode.ParseNodeType switch
        {
            ParseNodeType.Expression => CheckType(parseNode.Children[0]),
            ParseNodeType.FunctionCall => (CheckType(parseNode.Children[0]) as FunctionType)?.ReturnType,
            ParseNodeType.StringLiteral => StringType.Instance,
            ParseNodeType.Identifier => GetType(Symbols[parseNode.Children[0].Token!.Text]),
            _ => null
        };

        parseNode.Type = type;

        return type;
    }
    
    public static IType? GetType(ISymbol symbol) => symbol switch
    {
        BuiltInSymbol builtInSymbol => builtInSymbol.Type,
        _ => null
    };
}
