using NextGenToDoApp;

namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void CompilerWorks()
    {
        string sourceCode =
@"SetDocumentTitle(""Next Gen To-Do App"")
CreateUI(
  h1([ text(""Hello, world, from Presto!"") ])
)";
        var tokens = Lexer.Tokenize(sourceCode);
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
    }
}