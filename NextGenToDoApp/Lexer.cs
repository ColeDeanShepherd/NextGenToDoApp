using System.Text.RegularExpressions;

namespace NextGenToDoApp;

public record struct TextPosition(int LineIndex, int ColumnIndex)
{
    public override string ToString() => $"({LineIndex + 1}, {ColumnIndex + 1})";
}

public enum TokenType
{
    SingleLineComment,

    LeftParen,
    RightParen,
    LeftSquareBracket,
    RightSquareBracket,
    Comma,
    EqualsSign,
    Colon,
    RightArrow,

    FnKeyword,

    NumberLiteral,
    TextLiteral,
    Identifier,
}

public record Token(TokenType Type, string Text, TextPosition Position);

public static class Lexer
{
    public static readonly List<(TokenType TokenType, string RegexString)> TokenRegexPatterns =
    [
        (TokenType.SingleLineComment, @"//[^\r\n]*"),

        (TokenType.LeftParen, @"\("),
        (TokenType.RightParen, @"\)"),
        (TokenType.LeftSquareBracket, @"\["),
        (TokenType.RightSquareBracket, @"\]"),
        (TokenType.Comma, @","),
        (TokenType.EqualsSign, @"="),
        (TokenType.Colon, @":"),
        (TokenType.RightArrow, @"->"),

        (TokenType.FnKeyword, @"fn"),

        (TokenType.NumberLiteral, @"[0-9]+"),
        (TokenType.TextLiteral, @"""[^""]*"""),
        (TokenType.Identifier, @"[_a-zA-Z][_a-zA-Z0-9]*"),
    ];

    public static List<Token> Tokenize(string sourceCode)
    {
        Dictionary<Regex, TokenType> regexToTokenTypes = TokenRegexPatterns
            .ToDictionary(x => new Regex("^" + x.RegexString), x => x.TokenType);

        int nextCharIndex = 0;
        TextPosition nextCharPosition = new(0, 0);
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
                    tokens.Add(new(tokenType, match.Value, nextCharPosition));

                    for (int i = 0; i < match.Length; i++)
                    {
                        ReadChar();
                    }

                    readToken = true;
                    break;
                }
            }

            if (!readToken)
            {
                throw new Exception($"Unexpected '{sourceCode[nextCharIndex]}' at index {nextCharPosition}");
            }
        }

        return tokens;

        void ReadChar()
        {
            if (sourceCode[nextCharIndex] == '\n')
            {
                nextCharPosition = new(nextCharPosition.LineIndex + 1, 0);
            }
            else
            {
                nextCharPosition = new(nextCharPosition.LineIndex, nextCharPosition.ColumnIndex + 1);
            }

            nextCharIndex++;
        }

        void SkipWhitespace()
        {
            while ((nextCharIndex < sourceCode.Length) && char.IsWhiteSpace(sourceCode[nextCharIndex]))
            {
                ReadChar();
            }
        }
    }
}
