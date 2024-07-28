using NextGenToDoApp;

namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void CompilerWorks()
    {
        string sourceCode =
@"setDocumentTitle(""Next Gen To-Do App"")
createUI(
  h1([ text(""Hello, world, from Presto!"") ])
)

// This is a comment";
        var jsCode = Compiler.Compile(sourceCode);
    }
}