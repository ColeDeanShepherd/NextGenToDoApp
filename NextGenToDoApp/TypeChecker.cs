namespace NextGenToDoApp;

#region Types

public interface IType { }

public record BuiltInType(string Name) : IType;

public record ListType(IType ElementType) : IType;

public record FunctionType(List<string> TypeArgNames, List<IType> ParamTypes, IType ReturnType) : IType;

public record TypeArgumentType(string Name) : IType;

public static class Types
{
    public static readonly IType Nothing = new BuiltInType("Nothing");
    public static readonly IType Text = new BuiltInType("Text");
    public static readonly IType HtmlNode = new BuiltInType("HtmlNode");
}

#endregion Types

#region Symbols

public interface ISymbol
{
    string Name { get; }
}

public record BuiltInSymbol(string Name, IType Type) : ISymbol;

public static class Symbols
{
    public static readonly ISymbol NothingSymbol = new BuiltInSymbol("Nothing", Types.Nothing);
    public static readonly ISymbol TextSymbol = new BuiltInSymbol("Text", Types.Text);
    public static readonly ISymbol HtmlNodeSymbol = new BuiltInSymbol("HtmlNode", Types.HtmlNode);
    public static readonly ISymbol ExecJSSymbol = new BuiltInSymbol(
        "exec_JS",
        new FunctionType(
            new List<string> { "TReturn" },
            new List<IType> { new BuiltInType("Text") },
            new TypeArgumentType("TReturn")
        )
    );

    public static List<ISymbol> All = [
        NothingSymbol,
        TextSymbol,
        HtmlNodeSymbol,
        ExecJSSymbol
    ];
}

#endregion Symbols

public static class TypeChecker
{
    public static readonly Dictionary<string, ISymbol> NamesToSymbols =
        Symbols.All.ToDictionary(s => s.Name);

    public static IType? CheckType(ParseNode parseNode)
    {
        if (parseNode.ParseNodeType == ParseNodeType.Binding)
        {
            ParseNode identNode = parseNode.Children[0];
            ParseNode valueNode = parseNode.Children.Last();
            IType? valueType = CheckType(valueNode);

            identNode.Type = valueType;
            parseNode.Type = valueType;

            return valueType;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ParameterDefinition)
        {
            ParseNode identNode = parseNode.Children[0];
            ParseNode typeNode = parseNode.Children.Last();
            IType? type = CheckType(typeNode);

            identNode.Type = type;
            parseNode.Type = type;

            return type;
        }
        else
        {
            foreach (var child in parseNode.Children)
            {
                CheckType(child);
            }

            IType? type = parseNode.ParseNodeType switch
            {
                ParseNodeType.Expression => CheckType(parseNode.Children[0]),
                ParseNodeType.FunctionCall => (CheckType(parseNode.Children[0]) as FunctionType)?.ReturnType,
                ParseNodeType.TextLiteral => Types.Text,
                ParseNodeType.ListLiteral => new ListType(CheckType(parseNode.Children[0])!),
                ParseNodeType.Identifier => CheckType(parseNode, NamesToSymbols[parseNode.Children[0].Token!.Text]),
                _ => null
            };

            parseNode.Type = type;

            return type;
        }
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
