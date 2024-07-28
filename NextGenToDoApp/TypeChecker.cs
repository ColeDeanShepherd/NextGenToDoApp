namespace NextGenToDoApp;

public interface ISymbol
{
    string Name { get; }
}

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

public record ListType(IType ElementType) : IType;

public record FunctionType(List<IType> ParamTypes, IType ReturnType) : IType;

public record BuiltInType(string Name) : IType;

public static class TypeChecker
{
    public static IType HtmlNodeType = new BuiltInType("HtmlNode");
    public static ISymbol HtmlNodeSymbol = new BuiltInSymbol("HtmlNode", HtmlNodeType);

    public static Dictionary<string, ISymbol> Symbols = new()
    {
        { "consoleLog", new BuiltInSymbol("consoleLog", new FunctionType([ StringType.Instance ], VoidType.Instance)) },
        { "setDocumentTitle", new BuiltInSymbol("setDocumentTitle", new FunctionType([ StringType.Instance ], VoidType.Instance)) },
        { "createUI", new BuiltInSymbol("createUI", new FunctionType([ HtmlNodeType ], VoidType.Instance)) },
        { "h1", new BuiltInSymbol("h1", new FunctionType([ new ListType(HtmlNodeType) ], HtmlNodeType)) },
        { "text", new BuiltInSymbol("text", new FunctionType([ StringType.Instance ], HtmlNodeType)) }
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
            ParseNodeType.ListLiteral => new ListType(CheckType(parseNode.Children[0])!),
            ParseNodeType.Identifier => CheckType(parseNode, Symbols[parseNode.Children[0].Token!.Text]),
            _ => null
        };

        parseNode.Type = type;

        return type;
    }
    
    public static IType? CheckType(ParseNode parseNode, ISymbol symbol)
    {
        parseNode.Symbol = symbol;

        return symbol switch
        {
            BuiltInSymbol builtInSymbol => builtInSymbol.Type,
            _ => null
        };
    }
}
