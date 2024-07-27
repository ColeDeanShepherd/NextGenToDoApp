﻿namespace NextGenToDoApp;

internal class Program
{
    static void Main(string[] args)
    {
        var tokens = Lexer.Tokenize("ConsoleLog(\"Hello, world!\")");
        var parseTree = Parser.Parse(tokens);
        TypeChecker.CheckType(parseTree);
        var result = Interpreter.Interpret(parseTree);
    }
}
