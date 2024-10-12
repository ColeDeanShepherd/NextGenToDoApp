namespace NextGenToDoApp;

public enum ParseNodeType
{
    Program,
    Expression,
    Binding,
    FunctionDefinition,
    ParameterTuple,
    ParameterDefinition,
    ExplicitReturnType,
    FunctionCall,
    TypeArgumentTuple,
    ArgumentTuple,
    TextLiteral,
    ListLiteral,
    Identifier,
    Token,
    SingleLineComment,
}

public static class ParseNodeTypeExtensions
{
    public static bool IsExpression(this ParseNodeType parseNodeType) =>
        parseNodeType switch
        {
            ParseNodeType.Expression => true,
            ParseNodeType.Binding => true,
            ParseNodeType.FunctionDefinition => true,
            ParseNodeType.FunctionCall => true,
            ParseNodeType.TextLiteral => true,
            ParseNodeType.ListLiteral => true,
            ParseNodeType.Identifier => true,
            _ => false
        };
}

public class ParseNode
{
    public ParseNodeType ParseNodeType;
    public List<ParseNode> Children;
    public Token? Token;
    public ISymbol? Symbol = null;
    public IType? Type = null;

    public ParseNode(ParseNodeType parseNodeType, List<ParseNode> children, Token? token)
    {
        ParseNodeType = parseNodeType;
        Children = children;
        Token = token;
    }

    public override string ToString()
    {
        var tokenStr = Token?.Text ?? "";
        var childrenStr = string.Join("", Children.Select(c => c.ToString()));
        return $"{tokenStr}{childrenStr}";
    }
}

public record NonterminalDefinition(ParseNodeType ParseNodeType, List<IGrammarSymbol> RHS);

public interface IGrammarSymbol { }

public record TerminalSymbol(TokenType TokenType) : IGrammarSymbol;

public record NonterminalSymbol(ParseNodeType ParseNodeType) : IGrammarSymbol;

public record OptionalSymbol(IGrammarSymbol GrammarSymbol): IGrammarSymbol;

public record ZeroOrMoreSymbol(IGrammarSymbol GrammarSymbol, IGrammarSymbol? Separator = null): IGrammarSymbol;

public record OrderedChoiceSymbol(IGrammarSymbol[] GrammarSymbolChoices) : IGrammarSymbol;

public record PrefixExpressionDefinition(
    ParseNodeType ParseNodeType,
    List<IGrammarSymbol> RHS
) : NonterminalDefinition(ParseNodeType, RHS);

public record InfixExpressionDefinition(
    ParseNodeType ParseNodeType,
    List<IGrammarSymbol> RHS,
    int LeftBindingPower,
    int RightBindingPower
) : NonterminalDefinition(ParseNodeType, RHS);

public record PostfixExpressionDefinition(
    ParseNodeType ParseNodeType,
    List<IGrammarSymbol> RHS,
    int LeftBindingPower
) : NonterminalDefinition(ParseNodeType, RHS);

public class ParseState
{
    public List<Token> Tokens;
    public int NextTokenIndex;

    public ParseState(List<Token> tokens, int nextTokenIndex)
    {
        Tokens = tokens;
        NextTokenIndex = nextTokenIndex;
    }
}

public static class Parser
{
    public static TerminalSymbol Sym(TokenType tokenType) => new(tokenType);
    public static NonterminalSymbol Sym(ParseNodeType parseNodeType) => new(parseNodeType);

    public static readonly List<NonterminalDefinition> Grammar =
    [
        new NonterminalDefinition(
            ParseNodeType.Program,
            [ new ZeroOrMoreSymbol(Sym(ParseNodeType.Expression)) ]),

        // expression is implicit

        new InfixExpressionDefinition(
            ParseNodeType.Binding,
            [ Sym(ParseNodeType.Expression), Sym(TokenType.EqualsSign), Sym(ParseNodeType.Expression) ],
            LeftBindingPower: 4,
            RightBindingPower: 5),

        new PrefixExpressionDefinition(ParseNodeType.TextLiteral, [ Sym(TokenType.TextLiteral) ]),
        new PrefixExpressionDefinition(
            ParseNodeType.ListLiteral,
            [
                Sym(TokenType.LeftSquareBracket),
                new ZeroOrMoreSymbol(Sym(ParseNodeType.Expression), Separator: Sym(TokenType.Comma)),
                Sym(TokenType.RightSquareBracket)
            ]),
        new PrefixExpressionDefinition(ParseNodeType.Identifier, [ Sym(TokenType.Identifier) ]),
        
        new PrefixExpressionDefinition(
            ParseNodeType.FunctionDefinition,
            [
                Sym(TokenType.FnKeyword),
                Sym(ParseNodeType.ParameterTuple),
                Sym(ParseNodeType.ExplicitReturnType),
                Sym(TokenType.RightArrow),
                Sym(ParseNodeType.Expression)
            ]),
        new NonterminalDefinition(
            ParseNodeType.ParameterTuple,
            [
                Sym(TokenType.LeftParen),
                new ZeroOrMoreSymbol(Sym(ParseNodeType.ParameterDefinition), Separator: Sym(TokenType.Comma)),
                Sym(TokenType.RightParen)
            ]),
        new NonterminalDefinition(
            ParseNodeType.ParameterDefinition,
            [
                Sym(ParseNodeType.Identifier),
                Sym(TokenType.Colon),
                Sym(ParseNodeType.Identifier)
            ]),
        new NonterminalDefinition(
            ParseNodeType.ExplicitReturnType,
            [
                Sym(TokenType.Colon),
                Sym(ParseNodeType.Identifier)
            ]),

        new PostfixExpressionDefinition(
            ParseNodeType.FunctionCall,
            [
                Sym(ParseNodeType.Expression),
                new OptionalSymbol(Sym(ParseNodeType.TypeArgumentTuple)),
                Sym(ParseNodeType.ArgumentTuple)
            ],
            12
        ),
        new NonterminalDefinition(
            ParseNodeType.TypeArgumentTuple,
            [
                Sym(TokenType.LeftSquareBracket),
                new ZeroOrMoreSymbol(Sym(ParseNodeType.Identifier), Separator: Sym(TokenType.Comma)),
                Sym(TokenType.RightSquareBracket)
            ]),
        new NonterminalDefinition(
            ParseNodeType.ArgumentTuple,
            [
                Sym(TokenType.LeftParen),
                new ZeroOrMoreSymbol(Sym(ParseNodeType.Expression), Separator: Sym(TokenType.Comma)),
                Sym(TokenType.RightParen)
            ]),

        new NonterminalDefinition(ParseNodeType.SingleLineComment, [ Sym(TokenType.SingleLineComment) ])
    ];

    public static readonly NonterminalDefinition RootNonterminal = Grammar.Single(r => r.ParseNodeType == ParseNodeType.Program);

    public static readonly List<PrefixExpressionDefinition> PrefixExpressionDefinitions = Grammar
        .OfType<PrefixExpressionDefinition>()
        .ToList();

    public static readonly Dictionary<TokenType, PrefixExpressionDefinition> TokenTypeToPrefixExpressionDefinitions =
        PrefixExpressionDefinitions
            .SelectMany(ped => GetPossibleFirstTokenTypes(ped).Select(tokenType => (tokenType, ped)))
            .ToDictionary(pair => pair.tokenType, pair => pair.ped);

    public static readonly Dictionary<TokenType, InfixExpressionDefinition> TokenTypeAfterExpressionToInfixExpressionDefinitions =
        Grammar
            .OfType<InfixExpressionDefinition>()
            .SelectMany(ied => GetPossibleFirstTokenTypes(ied.RHS.Skip(1)).Select(tokenType => (tokenType, ied)))
            .ToDictionary(pair => pair.tokenType, pair => pair.ied);

    public static readonly Dictionary<TokenType, PostfixExpressionDefinition> TokenTypeAfterExpressionToPostfixExpressionDefinitions =
        Grammar
            .OfType<PostfixExpressionDefinition>()
            .SelectMany(ped => GetPossibleFirstTokenTypes(ped.RHS.Skip(1)).Select(tokenType => (tokenType, ped)))
            .ToDictionary(pair => pair.tokenType, pair => pair.ped);

    public static ParseNode Parse(List<Token> tokens)
    {
        var state = new ParseState(tokens, 0);
        return ParseNonterminal(state, RootNonterminal);
    }

    public static ParseNode ParseNonterminal(ParseState state, ParseNodeType parseNodeType, List<ParseNode>? parsedChildren = null)
    {
        if (parseNodeType == ParseNodeType.Expression)
        {
           return ParseExpression(state);
        }

        var nonterminalDefinition = Grammar.Single(r => r.ParseNodeType == parseNodeType);
        return ParseNonterminal(state, nonterminalDefinition, parsedChildren);
    }

    public static ParseNode ParseNonterminal(ParseState state, NonterminalDefinition nonterminalDefinition, List<ParseNode>? parsedChildren = null)
    {
        List<ParseNode> children = parsedChildren ?? new();

        foreach (var grammarSymbol in nonterminalDefinition.RHS.Skip(children.Count))
        {
            children.AddRange(ParseGrammarSymbol(state, grammarSymbol));
        }

        return new ParseNode(nonterminalDefinition.ParseNodeType, children, null);
    }

    public static List<ParseNode> ParseGrammarSymbol(ParseState state, IGrammarSymbol grammarSymbol)
    {
        switch (grammarSymbol)
        {
            case TerminalSymbol terminalSymbol:
                var token = ReadToken(state);
                if (token.Type != terminalSymbol.TokenType)
                {
                    throw new Exception($"Expected token type {terminalSymbol.TokenType} but got {token.Text} ({token.Type})");
                }
                return [new(ParseNodeType.Token, new(), token)];
            case NonterminalSymbol nonterminalSymbol:
                return [ParseNonterminal(state, nonterminalSymbol.ParseNodeType)];
            case OptionalSymbol optionalSymbol:
                {
                    var possibleFirstTokenTypes = GetPossibleFirstTokenTypes(optionalSymbol);
                    List<ParseNode> nodes = new();

                    if (TryPeekToken(state) != null && possibleFirstTokenTypes.Contains(PeekToken(state).Type))
                    {
                        nodes.AddRange(ParseGrammarSymbol(state, optionalSymbol.GrammarSymbol));
                    }

                    return nodes;
                }
            case ZeroOrMoreSymbol zeroOrMoreSymbol:
                {
                    var possibleFirstTokenTypes = GetPossibleFirstTokenTypes(zeroOrMoreSymbol);
                    var separatorFirstTokenTypes = zeroOrMoreSymbol.Separator == null ? new HashSet<TokenType>() : GetPossibleFirstTokenTypes(zeroOrMoreSymbol.Separator);
                    List<ParseNode> nodes = new();
                    while (TryPeekToken(state) != null && possibleFirstTokenTypes.Contains(PeekToken(state).Type))
                    {
                        nodes.AddRange(ParseGrammarSymbol(state, zeroOrMoreSymbol.GrammarSymbol));

                        if (zeroOrMoreSymbol.Separator != null)
                        {
                            if (TryPeekToken(state) == null || !separatorFirstTokenTypes.Contains(PeekToken(state).Type))
                            {
                                break;
                            }

                            nodes.AddRange(ParseGrammarSymbol(state, zeroOrMoreSymbol.Separator));
                        }
                    }
                    return nodes;
                }
            case OrderedChoiceSymbol orderedChoiceSymbol:
                {
                    var nextToken = PeekToken(state);
                    var matchingGrammarSymbol = orderedChoiceSymbol.GrammarSymbolChoices
                        .First(grammarSymbol => GetPossibleFirstTokenTypes(grammarSymbol).Contains(nextToken.Type));
                    return ParseGrammarSymbol(state, matchingGrammarSymbol);
                }
            default:
                throw new NotImplementedException();
        }
    }

    public static ParseNode ParseExpression(ParseState state)
    {
        return ParseExpression(state, 0);
    }

    public static ParseNode ParseExpression(ParseState state, int minBindingPower)
    {
        var prefixExpression = ParsePrefixExpression(state);
        var expression = ParsePostfixAndInfixExpressions(state, prefixExpression, minBindingPower);
        return expression;
    }

    public static ParseNode ParsePrefixExpression(ParseState state)
    {
        var nextToken = PeekToken(state);
        var prefixExpressionDefinition = TokenTypeToPrefixExpressionDefinitions[nextToken.Type];
        return ParseNonterminal(state, prefixExpressionDefinition);
    }

    public static ParseNode ParsePostfixAndInfixExpressions(ParseState state, ParseNode prefixExpr, int minBindingPower)
    {
        var nextToken = TryPeekToken(state);
        if (nextToken == null)
        {
            return prefixExpr;
        }

        var postfixExprDef = TokenTypeAfterExpressionToPostfixExpressionDefinitions.GetValueOrDefault(nextToken.Type);

        if (postfixExprDef != null)
        {
            if (postfixExprDef.LeftBindingPower < minBindingPower)
            {
                return prefixExpr;
            }
            else
            {
                var postfixExpr = ParseNonterminal(state, postfixExprDef, [prefixExpr]);
                return ParsePostfixAndInfixExpressions(state, postfixExpr, minBindingPower);
            }
        }
        else
        {
            var infixExprDef = TokenTypeAfterExpressionToInfixExpressionDefinitions.GetValueOrDefault(nextToken.Type);

            if (infixExprDef != null)
            {
                if (infixExprDef.LeftBindingPower < minBindingPower)
                {
                    return prefixExpr;
                }
                else
                {
                    var infixExpr = ParseNonterminal(state, infixExprDef, [prefixExpr]);
                    return ParsePostfixAndInfixExpressions(state, infixExpr, minBindingPower);
                }
            }
            else
            {
                return prefixExpr;
            }
        }
    }

    private static HashSet<TokenType> GetPossibleFirstTokenTypes(NonterminalDefinition nonterminalDefinition)
    {
        return GetPossibleFirstTokenTypes(nonterminalDefinition.RHS);
    }
        
    private static HashSet<TokenType> GetPossibleFirstTokenTypes(IGrammarSymbol grammarSymbol)
    {
        return grammarSymbol switch
        {
            TerminalSymbol terminalSymbol => new HashSet<TokenType> { terminalSymbol.TokenType },
            NonterminalSymbol nonterminalSymbol => GetPossibleFirstTokenTypes(nonterminalSymbol.ParseNodeType),
            OptionalSymbol optionalSymbol => GetPossibleFirstTokenTypes(optionalSymbol.GrammarSymbol),
            ZeroOrMoreSymbol zeroOrMoreSymbol => GetPossibleFirstTokenTypes(zeroOrMoreSymbol.GrammarSymbol),
            OrderedChoiceSymbol orderedChoiceSymbol => orderedChoiceSymbol.GrammarSymbolChoices.SelectMany(GetPossibleFirstTokenTypes).ToHashSet(),
            _ => throw new NotImplementedException()
        };
    }

    private static HashSet<TokenType> GetPossibleFirstTokenTypes(IEnumerable<IGrammarSymbol> grammarSymbols)
    {
        var firstGrammarSymbol = grammarSymbols.First();
        HashSet<TokenType> possibleFirstTokenTypes = GetPossibleFirstTokenTypes(firstGrammarSymbol);

        bool isOptional = firstGrammarSymbol switch
        {
            TerminalSymbol terminalSymbol => false,
            NonterminalSymbol nonterminalSymbol => false,
            OptionalSymbol optionalSymbol => true,
            ZeroOrMoreSymbol zeroOrMoreSymbol => true,
            OrderedChoiceSymbol orderedChoiceSymbol => false,
            _ => throw new NotImplementedException()
        };

        if (!isOptional)
        {
            return possibleFirstTokenTypes;
        }
        else
        {
            return possibleFirstTokenTypes
                .Concat(GetPossibleFirstTokenTypes(grammarSymbols.Skip(1)))
                .ToHashSet();
        }
    }

    public static HashSet<TokenType> GetPossibleFirstTokenTypes(ParseNodeType parseNodeType)
    {
        if (parseNodeType == ParseNodeType.Expression)
        {
            return PrefixExpressionDefinitions.SelectMany(GetPossibleFirstTokenTypes).ToHashSet();
        }

        return GetPossibleFirstTokenTypes(
            Grammar
                .Single(r => r.ParseNodeType == parseNodeType)
                .RHS
            );
    }

    private static Token? TryPeekToken(ParseState state)
    {
        return state.NextTokenIndex < state.Tokens.Count ? state.Tokens[state.NextTokenIndex] : null;
    }

    private static Token PeekToken(ParseState state)
    {
        return state.Tokens[state.NextTokenIndex];
    }
    
    private static Token ReadToken(ParseState state)
    {
        if (state.NextTokenIndex >= state.Tokens.Count)
        {
            throw new Exception("Unexpected end of input");
        }

        var token = state.Tokens[state.NextTokenIndex];
        state.NextTokenIndex++;
        return token;
    }
}
