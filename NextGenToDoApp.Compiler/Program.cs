using NextGenToDoApp;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            string sourceCodeFilePath = args[0];
            string sourceCode = File.ReadAllText(sourceCodeFilePath);
            var jsCode = Compiler.Compile(sourceCode);

            string outputFilePath = Path.Combine(
                Path.GetDirectoryName(sourceCodeFilePath)!,
                Path.GetFileNameWithoutExtension(sourceCodeFilePath) + ".js"
            );
            File.WriteAllText(outputFilePath, jsCode);
        }
        catch (Exception e)
        {
            Console.Write(e.ToString());
        }
    }
}
