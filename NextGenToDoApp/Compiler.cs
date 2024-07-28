namespace NextGenToDoApp;

public static class Compiler
{
    public static string Compile(string sourceCode)
    {
        var tokens = Lexer.Tokenize(sourceCode);
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
        var jsCode = JSCodeGenerator.Generate(parseTree);
        return jsCode;
    }
}
