namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void LogFnDef()
    {
        string sourceCode = @"log = fn (text: Text): Nothing -> exec_JS[Nothing](""console.log(text);"")";
        var jsCode = Compiler.Compile(sourceCode);
        Assert.Equal("const log = (text) => { console.log(text); };\r\n", jsCode);
    }

    [Fact]
    public void FnDefAndCall()
    {
        string sourceCode =
@"set_document_title = fn (title: Text): Nothing -> exec_JS[Nothing](""document.title = title"")
set_document_title(""Next Gen To-Do App"")";
        var jsCode = Compiler.Compile(sourceCode);
    }

    [Fact]
    public void GenericTypeReference()
    {
        string sourceCode = @"div = List[HTMLNode]";
        var jsCode = Compiler.Compile(sourceCode);
    }

    [Fact]
    public void FnWithInstantiatedGenericParamType()
    {
        string sourceCode = "test_fn = fn (children: List[HTMLNode]): Nothing -> exec_JS[Nothing](\"\")";
        var jsCode = Compiler.Compile(sourceCode);
    }

    [Fact]
    public void ValueAfterComment()
    {
        string sourceCode =
@"//
0";
        var jsCode = Compiler.Compile(sourceCode);
    }
}