using System.Reflection.Metadata.Ecma335;

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

    IType Type { get; }
}

public record SimpleSymbol(string Name, IType Type) : ISymbol;

public static class Symbols
{
    public static readonly ISymbol NothingSymbol = new SimpleSymbol("Nothing", Types.Nothing);
    public static readonly ISymbol TextSymbol = new SimpleSymbol("Text", Types.Text);
    public static readonly ISymbol HtmlNodeSymbol = new SimpleSymbol("HTMLNode", Types.HtmlNode);
    public static readonly ISymbol ExecJSSymbol = new SimpleSymbol(
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

public record TypeCheckerState(Dictionary<string, ISymbol> NamesToSymbols);

public static class TypeChecker
{
    public static IType? CheckType(ParseNode parseNode)
    {
        Dictionary<string, ISymbol> namesToSymbols = Symbols.All.ToDictionary(s => s.Name);
        TypeCheckerState state = new(namesToSymbols);

        return CheckType(state, parseNode);
    }

    public static IType? InferFunctionCallReturnType(Dictionary<string, IType> knownTypeArguments, FunctionType fnType, List<IType> argTypes)
    {
        if (fnType.ReturnType is TypeArgumentType returnTypeArg)
        {
            if (knownTypeArguments.ContainsKey(returnTypeArg.Name))
            {
                return knownTypeArguments[returnTypeArg.Name];
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            return fnType.ReturnType;
        }
    }

    public static IType? CheckType(TypeCheckerState state, ParseNode parseNode)
    {
        if (parseNode.ParseNodeType == ParseNodeType.Binding)
        {
            ParseNode identNode = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.Identifier);
            string name = identNode.Children.First(c => c.ParseNodeType == ParseNodeType.Token).Token!.Text;
            ParseNode valueNode = parseNode.Children.Last();
            IType? valueType = CheckType(state, valueNode);

            identNode.Type = valueType;
            parseNode.Type = valueType;

            SimpleSymbol symbol = new(name, valueType!);
            state.NamesToSymbols.Add(name, symbol);

            return valueType;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ParameterDefinition)
        {
            ParseNode identNode = parseNode.Children[0];
            ParseNode typeNode = parseNode.Children.Last();
            IType? type = CheckType(state, typeNode);

            identNode.Type = type;
            parseNode.Type = type;

            return type;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionDefinition)
        {
            // TODO: handle type parameters after they're added

            //

            ParseNode paramTupleNode = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.ParameterTuple);
            List<IType?> paramTypes = paramTupleNode.Children
                .Where(c => c.ParseNodeType == ParseNodeType.ParameterDefinition)
                .Select(c => CheckType(state, c))
                .ToList();

            if (paramTypes.Any(t => t == null))
            {
                throw new NotImplementedException();
            }

            ParseNode explicitReturnTypeNode = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.ExplicitReturnType);
            IType? returnType = CheckType(state, explicitReturnTypeNode);

            if (returnType == null)
            {
                throw new NotImplementedException();
            }

            ParseNode bodyNode = parseNode.Children.Last(c => c.ParseNodeType.IsExpression());
            CheckType(state, bodyNode);

            List<string> typeArgNames = [];
            FunctionType type = new(typeArgNames, paramTypes.Cast<IType>().ToList(), returnType);
            return type;

            //IType? returnType = InferFunctionCallReturnType(knownTypeArgs,  CheckType(state, bodyNode);
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ExplicitReturnType)
        {
            ParseNode identNode = parseNode.Children.Single(c => c.ParseNodeType == ParseNodeType.Identifier);
            ParseNode tokenNode = identNode.Children.Single(c => c.ParseNodeType == ParseNodeType.Token);
            string name = tokenNode.Token!.Text;
            ISymbol symbol = state.NamesToSymbols[name];

            parseNode.Type = symbol.Type;

            return symbol.Type;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            IType? type = (CheckType(state, parseNode.Children[0]) as FunctionType)?.ReturnType;

            parseNode.Type = type;

            return type;
        }
        else
        {
            foreach (var child in parseNode.Children)
            {
                CheckType(state, child);
            }

            IType? type = parseNode.ParseNodeType switch
            {
                ParseNodeType.Expression => CheckType(state, parseNode.Children[0]),
                ParseNodeType.TextLiteral => Types.Text,
                ParseNodeType.ListLiteral => new ListType(CheckType(state, parseNode.Children[0])!),
                ParseNodeType.Identifier => CheckType(state, parseNode, state.NamesToSymbols[parseNode.Children[0].Token!.Text]),
                ParseNodeType.FunctionCall => null,
                ParseNodeType.TypeArgumentTuple => null,
                ParseNodeType.ParameterTuple => null,
                ParseNodeType.ArgumentTuple => null,
                ParseNodeType.Token => null,
                ParseNodeType.ExplicitReturnType => null,
                ParseNodeType.Program => null,
                _ => throw new NotImplementedException($"Unknown parse node type: {parseNode.ParseNodeType}")
            };

            parseNode.Type = type;

            return type;
        }
    }
    
    public static IType? CheckType(TypeCheckerState state, ParseNode parseNode, ISymbol symbol)
    {
        parseNode.Symbol = symbol;

        return symbol switch
        {
            SimpleSymbol builtInSymbol => builtInSymbol.Type,
            _ => null
        };
    }
}
