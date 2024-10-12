namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void CompilerWorks()
    {
        string sourceCode = @"log = fn (text: Text): Nothing -> exec_JS[Nothing](""console.log(text)"")";
        var jsCode = Compiler.Compile(sourceCode);
    }
}