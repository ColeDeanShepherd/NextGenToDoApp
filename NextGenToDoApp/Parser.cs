namespace NextGenToDoApp;

public enum ParseNodeType
{
    Expression,
    FunctionCall,
    StringLiteral,
    Identifier,
    Token
}

public class ParseNode
{
    public ParseNodeType ParseNodeType;
    public List<ParseNode> Children;
    public Token? Token;
    public IType? Type = null;

    public ParseNode(ParseNodeType parseNodeType, List<ParseNode> children, Token? token)
    {
        ParseNodeType = parseNodeType;
        Children = children;
        Token = token;
    }
}

public record NonterminalDefinition(ParseNodeType ParseNodeType, List<IGrammarSymbol> RHS);

public interface IGrammarSymbol { }

public record TerminalSymbol(TokenType TokenType) : IGrammarSymbol;

public record NonterminalSymbol(ParseNodeType ParseNodeType) : IGrammarSymbol;

public record PrefixExpressionDefinition(
    ParseNodeType ParseNodeType,
    List<IGrammarSymbol> RHS
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
        new PrefixExpressionDefinition(ParseNodeType.StringLiteral, [ Sym(TokenType.StringLiteral) ]),
        new PrefixExpressionDefinition(ParseNodeType.Identifier, [ Sym(TokenType.Identifier) ]),

        new PostfixExpressionDefinition(
            ParseNodeType.FunctionCall,
            [ Sym(ParseNodeType.Expression), Sym(TokenType.LeftParen), Sym(ParseNodeType.Expression), Sym(TokenType.RightParen) ],
            12
        )
    ];

    public static readonly List<PrefixExpressionDefinition> PrefixExpressionDefinitions = Grammar
        .OfType<PrefixExpressionDefinition>()
        .ToList();

    public static readonly Dictionary<TokenType, PrefixExpressionDefinition> TokenTypeToPrefixExpressionDefinitions =
        PrefixExpressionDefinitions
            .SelectMany(prefixExpressionDefinition => GetPossibleFirstTokenTypes(prefixExpressionDefinition).Select(tokenType => (tokenType, prefixExpressionDefinition)))
            .ToDictionary(pair => pair.tokenType, pair => pair.prefixExpressionDefinition);

    public static readonly Dictionary<TokenType, PostfixExpressionDefinition> TokenTypeAfterExpressionToPostfixExpressionDefinitions =
        Grammar
            .OfType<PostfixExpressionDefinition>()
            .ToDictionary(postfixExpressionDefinition => GetPossibleFirstTokenTypes(postfixExpressionDefinition.RHS[1]).Single(), postfixExpressionDefinition => postfixExpressionDefinition);

    public static ParseNode Parse(List<Token> tokens)
    {
        var state = new ParseState(tokens, 0);
        return ParseExpression(state);
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
            switch (grammarSymbol)
            {
                case TerminalSymbol terminalSymbol:
                    var token = ReadToken(state);
                    if (token.Type != terminalSymbol.TokenType)
                    {
                        throw new Exception($"Expected token type {terminalSymbol.TokenType} but got {token.Type}");
                    }
                    children.Add(new(ParseNodeType.Token, new(), token));
                    break;
                case NonterminalSymbol nonterminalSymbol:
                    children.Add(ParseNonterminal(state, nonterminalSymbol.ParseNodeType));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return new ParseNode(nonterminalDefinition.ParseNodeType, children, null);
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

    public static ParseNode ParsePostfixAndInfixExpressions(ParseState state, ParseNode left, int minBindingPower)
    {
        var nextToken = TryPeekToken(state);
        if (nextToken == null)
        {
            return left;
        }

        var postfixExpressionDefinition = TokenTypeAfterExpressionToPostfixExpressionDefinitions.GetValueOrDefault(nextToken.Type);
        if (postfixExpressionDefinition == null || postfixExpressionDefinition.LeftBindingPower < minBindingPower)
        {
            return left;
        }

        var postfixExpression = ParseNonterminal(state, postfixExpressionDefinition, [left]);
        return ParsePostfixAndInfixExpressions(state, postfixExpression, minBindingPower);
    }

    private static HashSet<TokenType> GetPossibleFirstTokenTypes(NonterminalDefinition nonterminalDefinition)
    {
        return GetPossibleFirstTokenTypes(nonterminalDefinition.RHS.First());
    }
        
    private static HashSet<TokenType> GetPossibleFirstTokenTypes(IGrammarSymbol grammarSymbol)
    {
        return grammarSymbol switch
        {
            TerminalSymbol terminalSymbol => new HashSet<TokenType> { terminalSymbol.TokenType },
            NonterminalSymbol nonterminalSymbol => GetPossibleFirstTokenTypes(Grammar.Single(r => r.ParseNodeType == nonterminalSymbol.ParseNodeType)),
            _ => throw new NotImplementedException()
        };
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
        var token = state.Tokens[state.NextTokenIndex];
        state.NextTokenIndex++;
        return token;
    }
}
