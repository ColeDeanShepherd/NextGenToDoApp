namespace NextGenToDoApp;

public static class Interpreter
{
    public static object? Interpret(string sourceCode)
    {
        var tokens = Lexer.Tokenize(sourceCode);
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
        var result = Interpreter.Interpret(parseTree);
        return result;
    }

    public static object? Interpret(ParseNode parseNode)
    {
        if (parseNode.ParseNodeType == ParseNodeType.Program)
        {
            foreach (var child in parseNode.Children)
            {
                Interpret(child);
            }

            return null;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            var functionSymbol = parseNode.Children[0].Symbol;

            var argNodes = parseNode.Children.Skip(1).Where(c => c.ParseNodeType.IsExpression()).ToList();
            var args = argNodes.Select(Interpret).ToList();

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

                Console.WriteLine("SetDocumentTitle: " + s);
                return null;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (parseNode.ParseNodeType == ParseNodeType.StringLiteral)
        {
            return parseNode.Children[0].Token!.Text.Trim('"');
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
