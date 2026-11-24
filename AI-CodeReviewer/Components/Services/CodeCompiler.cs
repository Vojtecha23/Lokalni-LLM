using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;

public static class CodeCompiler
{
    public static string ExtractCode(string response)
    {
        var match = Regex.Match(
            response,
            @"```(?:csharp|cs)?[ \t]*\r?\n([\s\S]*?)```",
            RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups[1].Value.Trim()
            : "";
    }

    public static (bool Success, string Message) Compile(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Model neposkytl C# kód.");

        var globalUsings = CSharpSyntaxTree.ParseText("""
            global using System;
            global using System.Linq;
            global using System.Collections.Generic;
            global using System.Threading.Tasks;
            global using System.IO;
            """);

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var paths = ((string)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);

        var references = paths.Select(path =>
            MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "AIValidation",
            new[] { globalUsings, syntaxTree },
            references,
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication));

        using var stream = new MemoryStream();

        var result = compilation.Emit(stream);

        if (result.Success)
            return (true, "Kompilace úspěšná.");

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString());

        return (false, string.Join("\n", errors));
    }
}