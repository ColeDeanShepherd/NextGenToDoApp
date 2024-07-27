using NextGenToDoApp;

namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void CompilerWorks()
    {
        string sourceCode =
@"SetDocumentTitle(""TestDocTitle"")
ConsoleLog(""ASDFasdf"")";
        var tokens = Lexer.Tokenize(sourceCode);
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
    }
}