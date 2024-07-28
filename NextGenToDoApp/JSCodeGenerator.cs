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
        else if (parseNode.ParseNodeType == ParseNodeType.FunctionCall)
        {
            var functionSymbol = parseNode.Children[0].Symbol;

            var argNodes = parseNode.Children.Skip(1).Where(c => c.ParseNodeType.IsExpression()).ToList();
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
        else if (parseNode.ParseNodeType == ParseNodeType.StringLiteral)
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
