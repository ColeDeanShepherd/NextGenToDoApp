namespace NextGenToDoApp;

public enum ParseNodeType
{
    FunctionCall,
    Expression,
    Token
}

public record ParseNode(ParseNodeType ParseNodeType, List<ParseNode> Children, Token? Token);

public record NonterminalDefinition(ParseNodeType ParseNodeType, List<IGrammarSymbol> RHS);

public interface IGrammarSymbol { }

public record TerminalSymbol(TokenType TokenType) : IGrammarSymbol;

public record NonterminalSymbol(ParseNodeType ParseNodeType) : IGrammarSymbol;

public record PrefixExpressionDefinition(TokenType FirstTokenType);

public static class Parser
{
    public static TerminalSymbol Sym(TokenType tokenType) => new(tokenType);
    public static NonterminalSymbol Sym(ParseNodeType parseNodeType) => new(parseNodeType);

    public static readonly List<NonterminalDefinition> Grammar =
    [
        new NonterminalDefinition(
            ParseNodeType.FunctionCall,
            [ Sym(ParseNodeType.Expression), Sym(TokenType.LeftParen), Sym(ParseNodeType.Expression), Sym(TokenType.RightParen) ]
        )
    ];

    public static readonly List<PrefixExpressionDefinition> PrefixExpressions =
    [
        new PrefixExpressionDefinition(TokenType.StringLiteral),
        new PrefixExpressionDefinition(TokenType.Identifier)
    ];
}
