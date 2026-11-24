using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using var client = new HttpClient();
client.Timeout = TimeSpan.FromMinutes(10);

string[] modely =
{
    "qwen-csharp",
    "llama-csharp"
};

string[] testy =
{
    "int cislo = \"10\"; Console.WriteLine(cislo);",
    "int x = 10 Console.WriteLine(x);",
    "string text = null; Console.WriteLine(text.Length);",
    "int[] pole = {1, 2, 3}; Console.WriteLine(pole[3]);",
    "int a = 10; int b = 0; Console.WriteLine(a / b);",
    "bool stav = 5;",
    "Console.WriteLine(neexistujici);",
    "int x = 5; if (x = 10) Console.WriteLine(x);",
    "int cislo = 3.14;",
    "string text = \"Ahoj\"; Console.WriteLine(text.Lenght);"
};

string Csv(string text)
{
    return "\"" + text.Replace("\"", "\"\"") + "\"";
}

using var soubor = new StreamWriter(
    "vysledky.csv", false, new UTF8Encoding(true));

soubor.WriteLine("Model;Test;Cas_s;Kod;Odpoved");

foreach (string model in modely)
{
    Console.WriteLine($"\nTestovani modelu: {model}");

    // Zahřátí modelu před měřením
    using var warmup = await client.PostAsJsonAsync(
        "http://localhost:11434/api/generate",
        new { model, prompt = "Ahoj", stream = false });

    warmup.EnsureSuccessStatusCode();

    for (int i = 0; i < testy.Length; i++)
    {
        Console.WriteLine($"Test {i + 1}/{testy.Length}");

        var stopky = Stopwatch.StartNew();

        var request = new
        {
            model,
            prompt = "Najdi a oprav chybu v C# kódu:\n" + testy[i],
            stream = false
        };

        using var response = await client.PostAsJsonAsync(
            "http://localhost:11434/api/generate",
            request);

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        stopky.Stop();

        using var dokument = JsonDocument.Parse(json);

        string odpoved = dokument.RootElement
            .GetProperty("response").GetString() ?? "";

        double cas = stopky.Elapsed.TotalSeconds;

        Console.WriteLine($"Dokonceno za {cas:F2} s");

        soubor.WriteLine(
            $"{model};{i + 1};{cas:F2};" +
            $"{Csv(testy[i])};{Csv(odpoved)}");

        soubor.Flush();
    }
}

Console.WriteLine("\nExperiment dokoncen!");
Console.WriteLine("Vysledky jsou ulozeny v vysledky.csv");
