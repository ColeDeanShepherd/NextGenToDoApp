namespace NextGenToDoApp.Tests;

public class CompilerTests
{
    [Fact]
    public void CompilerWorks()
    {
        string sourceCode =
@"consoleLog = fn (text: Text): Nothing -> execJs[Nothing](""console.log(text)"")

setDocumentTitle = fn (title: Text): Nothing -> execJs[Nothing](""document.title = title"")

createUI = fn (node: HTMLNode): Nothing -> execJs[Nothing](""document.body.appendChild(node)"")

h1 = fn (text: Text): HTMLNode ->
	execJs[HTMLNode](
""const elem = document.createElement('h1');
for (const child of children) {
    elem.appendChild(child);
}
return elem;""
	)

txt = fn (text: Text): HTMLNode -> execJs[HTMLNode](""document.createTextNode(text)"")

setDocumentTitle(""Next Gen To-Do App"")

createUI(
  div([ h1([ txt(""Next Gen To-Do App"") ]) ])
)";
        var jsCode = Compiler.Compile(sourceCode);
    }
}