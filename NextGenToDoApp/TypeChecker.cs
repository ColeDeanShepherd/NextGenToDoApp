using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;

namespace NextGenToDoApp;

#region Types

public interface IType { }

public record BuiltInType(string Name) : IType;

public record GenericType(string Name, List<string> TypeParamNames) : IType;

public record InstantiatedGenericType(GenericType GenericType, List<IType> TypeArguments) : IType;

public record ListType(IType ElementType) : IType;

public record FunctionType(List<string> TypeArgNames, List<IType> ParamTypes, IType ReturnType) : IType;

public record TypeArgumentType(string Name) : IType;

public static class Types
{
    public static readonly IType Nothing = new BuiltInType("Nothing");
    public static readonly IType Text = new BuiltInType("Text");
    public static readonly IType GenericList = new GenericType("List", new List<string> { "T" });
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
    public static readonly ISymbol ListSymbol = new SimpleSymbol("List", Types.GenericList);
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
        ListSymbol,
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

    public static IType ReifyType(Dictionary<string, IType> knownTypeArguments, IType type)
    {
        if (type is TypeArgumentType typeArg)
        {
            if (knownTypeArguments.ContainsKey(typeArg.Name))
            {
                return knownTypeArguments[typeArg.Name];
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            return type;
        }
    }

    public static FunctionType ReifyFnType(Dictionary<string, IType> knownTypeArguments, FunctionType genericFnType)
    {
        var paramTypes = genericFnType.ParamTypes
            .Select(t => ReifyType(knownTypeArguments, t))
            .ToList();
        var returnType = ReifyType(knownTypeArguments, genericFnType.ReturnType);

        return new FunctionType([], paramTypes, returnType);
    }

    //public static FunctionType ReifyFnType(Dictionary<string, IType> knownTypeArguments, FunctionType genericFnType, List<IType> argTypes)
    //{
    //    var paramTypeAndArgTypes = genericFnType.ParamTypes.Zip(argTypes);
    //    var reifiedParamTypes = paramTypeAndArgTypes
    //        .Select(t =>
    //        {
    //            var (paramType, argType) = t;

    //            if (argType is BuiltInType argBuiltInType)
    //            {
    //                if (paramType is BuiltInType paramBuiltInType)
    //                {
    //                    if (argBuiltInType == paramBuiltInType)
    //                    {
    //                        return argBuiltInType;
    //                    }
    //                    else
    //                    {
    //                        throw new Exception($"Parameter type {paramType} doesn't match argument type {argType}");
    //                    }
    //                }
    //            }

    //            throw new NotImplementedException();
    //        })
    //        .ToList();





    //    //if (genericFnType.ReturnType is TypeArgumentType returnTypeArg)
    //    //{
    //    //    if (knownTypeArguments.ContainsKey(returnTypeArg.Name))
    //    //    {
    //    //        return typeParamNamesToTypes;
    //    //    }
    //    //    else
    //    //    {
    //    //        throw new NotImplementedException();
    //    //    }
    //    //}
    //    //else
    //    //{
    //    //    return genericFnType.ReturnType;
    //    //}
    //}

    public static string GetIdentifierText(ParseNode parseNode)
    {
        return parseNode.Children.Single(c => c.ParseNodeType == ParseNodeType.Token).Token!.Text;
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
        else if (parseNode.ParseNodeType == ParseNodeType.GenericInstantiation)
        {
            var genericExpr = parseNode.Children.First(c => c.ParseNodeType.IsExpression());
            var genericExprType = CheckType(state, genericExpr);

            if (genericExprType is GenericType genericType)
            {
                var typeArgTupleNode = parseNode.Children.Single(c => c.ParseNodeType == ParseNodeType.TypeArgumentTuple);
                var typeArgNodes = typeArgTupleNode.Children.Where(c => c.ParseNodeType == ParseNodeType.Identifier).ToList();

                var typeArgTypes = typeArgNodes.Select(n => CheckType(state, n)).ToList();

                var type = new InstantiatedGenericType(genericType!, typeArgTypes.Cast<IType>().ToList());

                parseNode.Type = type;

                return type;
            }
            else if (genericExprType is FunctionType genericFnType)
            {
                var typeArgTupleNode = parseNode.Children.Single(c => c.ParseNodeType == ParseNodeType.TypeArgumentTuple);
                var typeArgNodes = typeArgTupleNode.Children.Where(c => c.ParseNodeType == ParseNodeType.Identifier).ToList();
                var typeArgTypes = typeArgNodes.Select(n => CheckType(state, n)!);
                var typeParamAndArgTypes = genericFnType.TypeArgNames.Zip(typeArgTypes);
                Dictionary<string, IType> knownTypeArgs = typeParamAndArgTypes.ToDictionary(t => t.First, t => t.Second);

                var type = ReifyFnType(knownTypeArgs, genericFnType);

                parseNode.Type = type;

                string fnName = GetIdentifierText(parseNode.Children[0]);
                parseNode.Symbol = state.NamesToSymbols[fnName];

                return type;
            }
            else
            {
                throw new NotImplementedException($"Unexpected type: {genericExprType}");
            }
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
