using System.Text.RegularExpressions;

namespace NextGenToDoApp;

public enum TokenType
{
    Identifier,
    LeftParen,
    RightParen,
    StringLiteral
}

public record Token(TokenType Type, string Text);

public static class Lexer
{
    public static readonly List<(TokenType TokenType, string RegexString)> TokenRegexPatterns =
    [
        (TokenType.Identifier, @"[a-zA-Z]+"),
        (TokenType.LeftParen, @"\("),
        (TokenType.RightParen, @"\)"),
        (TokenType.StringLiteral, @"""[^""]*"""),
    ];

    public static List<Token> Tokenize(string sourceCode)
    {
        Dictionary<Regex, TokenType> regexToTokenTypes = TokenRegexPatterns
            .ToDictionary(x => new Regex("^" + x.RegexString), x => x.TokenType);

        int nextCharIndex = 0;
        List<Token> tokens = new();

        while (nextCharIndex < sourceCode.Length)
        {
            SkipWhitespace();

            bool readToken = false;
            string restOfSourceCode = sourceCode.Substring(nextCharIndex);

            foreach (var (regex, tokenType) in regexToTokenTypes)
            {
                Match match = regex.Match(restOfSourceCode);

                if (match.Success)
                {
                    nextCharIndex += match.Length;
                    tokens.Add(new(tokenType, match.Value));
                    readToken = true;
                    break;
                }
            }

            if (!readToken)
            {
                throw new Exception($"Unexpected character at index {nextCharIndex}");
            }
        }

        return tokens;

        void SkipWhitespace()
        {
            while ((nextCharIndex < sourceCode.Length) && char.IsWhiteSpace(sourceCode[nextCharIndex]))
            {
                nextCharIndex++;
            }
        }
    }
}
