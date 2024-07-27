using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NextGenToDoApp;

public static class Interpreter
{
    public static IJSRuntime JSRuntime = null!;

    public static async Task<object?> Interpret(string sourceCode)
    {
        var tokens = Lexer.Tokenize(sourceCode);
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
        var result = await Interpret(parseTree);
        return result;
    }

    public static async Task<object?> Interpret(ParseNode parseNode)
    {
        if (parseNode.ParseNodeType == ParseNodeType.Program)
        {
            foreach (var child in parseNode.Children)
            {
                await Interpret(child);
            }

            return null;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            var functionSymbol = parseNode.Children[0].Symbol;

            var argNodes = parseNode.Children.Skip(1).Where(c => c.ParseNodeType.IsExpression()).ToList();
            var args = await Task.WhenAll(argNodes.Select(Interpret));

            if (functionSymbol == TypeChecker.Symbols["ConsoleLog"])
            {
                var arg = args.Single();
                if (arg is not string s)
                {
                    throw new Exception("ConsoleLog expects a string argument");
                }

                Console.WriteLine(s);
                return null;
            }
            else if (functionSymbol == TypeChecker.Symbols["SetDocumentTitle"])
            {
                var arg = args.Single();
                if (arg is not string s)
                {
                    throw new Exception("SetDocumentTitle expects a string argument");
                }

                await JSRuntime.InvokeVoidAsync("setDocumentTitle", s);
                return null;
            }
            else if (functionSymbol == TypeChecker.Symbols["CreateUI"])
            {
                var arg = args.Single();
                //if (arg is not HtmlNode htmlNode)
                //{
                //    throw new Exception("CreateUI expects an HtmlNode argument");
                //}

                await JSRuntime.InvokeVoidAsync("CreateUI", arg);
                return null;
            }
            else if (functionSymbol == TypeChecker.Symbols["h1"])
            {
                var arg = args.Single();
                //if (arg is not List<HtmlNode> htmlNodes)
                //{
                //    throw new Exception("h1 expects a list of HtmlNode arguments");
                //}

                return await JSRuntime.InvokeAsync<ElementReference>("h1", arg);
            }
            else if (functionSymbol == TypeChecker.Symbols["text"])
            {
                var arg = args.Single();
                if (arg is not string s)
                {
                    throw new Exception("text expects a string argument");
                }

                return await JSRuntime.InvokeAsync<ElementReference>("text", s);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ListLiteral)
        {
            System.Collections.ArrayList list = new();

            foreach (var child in parseNode.Children.Where(c => c.ParseNodeType == ParseNodeType.Expression))
            {
                var item = await Interpret(child);
                list.Add(item);
            }

            return list;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.StringLiteral)
        {
            return parseNode.Children[0].Token!.Text.Trim('"');
        }
        else
        {
            throw new NotImplementedException($"Unknown parse node type: {parseNode.ParseNodeType}");
        }
    }
}
