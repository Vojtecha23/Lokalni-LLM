using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.Sqlite;

namespace LocalReviewer;
public record Exercise(string Name, string Level, string Code);
public record Attempt(int Number, string Answer, string Code, bool Compiled, string Diagnostics, double Seconds, int Tokens);
public record Review(string Model, List<Attempt> Attempts) {
    public Attempt Last => Attempts[^1];
    public double Seconds => Attempts.Sum(x => x.Seconds);
    public int Tokens => Attempts.Sum(x => x.Tokens);
}
public record Entry(string Id, string Date, string Name, string Level, string Source, bool Retry, Review Result);
public static class ReviewEngine {
    public static readonly string[] Models = ["qwen-csharp", "llama-csharp"];
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("http://localhost:11434/"), Timeout = TimeSpan.FromMinutes(10) };
    private static readonly Lazy<MetadataReference[]> Refs = new(() => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray());
    public static (bool Ok, string Message) Compile(string code) {
        if (string.IsNullOrWhiteSpace(code)) return (false,"Chybí blok C# kódu.");
        var imports = CSharpSyntaxTree.ParseText("global using System; global using System.Linq; global using System.Collections.Generic; global using System.Threading.Tasks; global using System.IO;");
        var compilation = CSharpCompilation.Create("Validation", [imports, CSharpSyntaxTree.ParseText(code)], Refs.Value, new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        return (result.Success, result.Success ? "Kompilace úspěšná" : string.Join("\n",result.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
    }
    public static async Task<Review> Run(string model, string source, bool retry, Action<string> progress, CancellationToken ct) {
        var attempts = new List<Attempt>();
        var original = "Najdi chyby v následujícím C# konzolovém programu. Stručně je vysvětli česky a vrať celý opravený program v jediném bloku ```csharp ... ```. Zachovej zamýšlenou funkčnost.\n"+source;
        var prompt = original;
        for(int i=1;i<=(retry?3:1);i++) {
            ct.ThrowIfCancellationRequested(); progress($"{model}: pokus {i}");
            var watch = Stopwatch.StartNew();
            using var response = await Http.PostAsJsonAsync("api/generate", new {model,prompt,stream=false,options=new {temperature=0.1,seed=42,num_predict=900}},ct);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            watch.Stop(); var root=json.RootElement;
            var answer=root.GetProperty("response").GetString()??"";
            var match=Regex.Match(answer,@"```(?:csharp|cs|c#)?[ \t]*\r?\n([\s\S]*?)```",RegexOptions.IgnoreCase);
            var code=match.Success?match.Groups[1].Value.Trim():"";
            var (ok,msg)=Compile(code);
            attempts.Add(new(i,answer,code,ok,msg,watch.Elapsed.TotalSeconds,root.TryGetProperty("eval_count",out var t)?t.GetInt32():0));
            if(ok) break;
            prompt=original+"\nPředchozí návrh:\n"+code+"\nDiagnostika kompilátoru:\n"+msg+"\nOprav návrh a vrať celý program v jediném bloku csharp.";
        }
        return new(model,attempts);
    }
    private static readonly string Db = Path.Combine(AppContext.BaseDirectory,"reviewer.db");
    private static SqliteConnection Open(){var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Db}.ToString());c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TABLE IF NOT EXISTS reviews(id TEXT PRIMARY KEY, payload TEXT NOT NULL)";cmd.ExecuteNonQuery();return c;}
    public static void Save(Entry e){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO reviews VALUES ($id,$p)";cmd.Parameters.AddWithValue("$id",e.Id);cmd.Parameters.AddWithValue("$p",JsonSerializer.Serialize(e));cmd.ExecuteNonQuery();}
    public static List<Entry> History(){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT payload FROM reviews ORDER BY rowid DESC";using var r=cmd.ExecuteReader();var list=new List<Entry>();while(r.Read()){var e=JsonSerializer.Deserialize<Entry>(r.GetString(0));if(e!=null)list.Add(e);}return list;}
    public static string Csv(IEnumerable<Entry> entries){string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";var lines=new List<string>{"Datum;Uloha;Obtiznost;Model;Samooprava;PrvniKompilace;PosledniKompilace;Pokusy;Sekundy;Tokeny;Zdroj;Odpoved;Kod;Diagnostika"};foreach(var e in entries){var r=e.Result;lines.Add(string.Join(";",new[]{Q(e.Date),Q(e.Name),Q(e.Level),Q(r.Model),e.Retry.ToString(),r.Attempts[0].Compiled.ToString(),r.Last.Compiled.ToString(),r.Attempts.Count.ToString(),r.Seconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),r.Tokens.ToString(),Q(e.Source),Q(r.Last.Answer),Q(r.Last.Code),Q(r.Last.Diagnostics)}));}return string.Join("\r\n",lines);}
}
