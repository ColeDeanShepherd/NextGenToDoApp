namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void LogFnDef()
    {
        string sourceCode = @"log = fn (text: Text): Nothing -> exec_JS[Nothing](""console.log(text)"")";
        var jsCode = Compiler.Compile(sourceCode);
        Assert.Equal("const log = (text) => console.log(text);\r\n", jsCode);
    }

    [Fact]
    public void FnDefAndCall()
    {
        string sourceCode =
@"set_document_title = fn (title: Text): Nothing -> exec_JS[Nothing](""document.title = title"")
set_document_title(""Next Gen To-Do App"")";
        var jsCode = Compiler.Compile(sourceCode);
    }
}