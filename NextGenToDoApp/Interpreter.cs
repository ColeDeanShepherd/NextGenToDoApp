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
        if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            var functionSymbol = parseNode.Children[0].Symbol as BuiltInSymbol;
            if (functionSymbol?.Name == "ConsoleLog")
            {
                var argNodes = parseNode.Children.Skip(1).Where(c => c.ParseNodeType.IsExpression());

                if (argNodes.Count() != 1)
                {
                    throw new Exception("ConsoleLog expects exactly one argument");
                }

                var args = argNodes.Select(Interpret).ToArray();

                if (args.Single() is string s)
                {
                    Console.WriteLine(s);
                    return null;
                }
                else
                {
                    throw new Exception("ConsoleLog expects a string argument");
                }
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
