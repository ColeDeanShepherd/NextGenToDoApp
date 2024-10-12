using System.Text;

namespace NextGenToDoApp;

public static class JSCodeGenerator
{
    public static string Generate(ParseNode parseNode)
    {
        if (parseNode.ParseNodeType == ParseNodeType.Program)
        {
            var sb = new StringBuilder();

            foreach (var child in parseNode.Children)
            {
                sb.AppendLine(Generate(child));
            }

            return sb.ToString();
        }
        else if (parseNode.ParseNodeType == ParseNodeType.Binding)
        {
            ParseNode identNode = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.Identifier);
            ParseNode valueNode = parseNode.Children.Last(c => c.ParseNodeType.IsExpression());

            return $"const {Generate(identNode)} = {Generate(valueNode)};";
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionDefinition)
        {
            ParseNode paramTuple = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.ParameterTuple);
            ParseNode returnType = parseNode.Children.First(c => c.ParseNodeType == ParseNodeType.ExplicitReturnType);
            ParseNode body = parseNode.Children.Last();

            return $"{Generate(paramTuple)} => {Generate(body)}";
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ParameterTuple)
        {
            var paramDefs = parseNode.Children.Where(c => c.ParseNodeType == ParseNodeType.ParameterDefinition).ToList();

            return $"({string.Join(", ", paramDefs.Select(Generate))})";
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ParameterDefinition)
        {
            ParseNode paramNameNode = parseNode.Children.Where(c => c.ParseNodeType == ParseNodeType.Identifier).First();
            ParseNode paramNameTokenNode = paramNameNode.Children.Single();

            return paramNameTokenNode.Token!.Text;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            var functionSymbol = parseNode.Children[0].Symbol;

            var argTuple = parseNode.Children.Where(c => c.ParseNodeType == ParseNodeType.ArgumentTuple).First();
            var argNodes = argTuple.Children.Skip(1).Where(c => c.ParseNodeType.IsExpression()).ToList();
            var args = argNodes.Select(Generate).ToList();

            var arg = args.Single();
            return $"window.{Generate(parseNode.Children[0])}({arg})";
        }
        else if (parseNode.ParseNodeType == ParseNodeType.Identifier)
        {
            return parseNode.Children.Single().Token!.Text;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.ListLiteral)
        {
            var sb = new StringBuilder();

            sb.Append("[");
            sb.Append(string.Join(", ", parseNode.Children.Where(c => c.ParseNodeType.IsExpression()).Select(Generate)));
            sb.Append("]");

            return sb.ToString();
        }
        else if (parseNode.ParseNodeType == ParseNodeType.TextLiteral)
        {
            return $"\"{parseNode.Children.Single().Token!.Text.Trim('"')}\"";
        }
        else if (parseNode.ParseNodeType == ParseNodeType.SingleLineComment)
        {
            return parseNode.Children.Single().Token!.Text;
        }
        else if (parseNode.ParseNodeType == ParseNodeType.Token)
        {
            return parseNode.Token!.Text;
        }
        else
        {
            throw new Exception($"Unknown node type: {parseNode.ParseNodeType}");
        }
    }
}
