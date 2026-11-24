# Experiment s lokálním LLM – AI Code Reviewer

## Úloha a cíl experimentu

Cílem projektu je porovnat dva lokální jazykové modely při hledání, vysvětlování a opravování chyb ve zdrojovém kódu C#. Experiment zkoumá rychlost odpovědí, kvalitu oprav a možnosti využití lokální AI při programování.

## Použité modely

| Model | Velikost podle Ollamy | Upravený model |
|---|---:|---|
| Qwen 2.5 3B | 1,9 GB | `qwen-csharp` |
| Llama 3.2 3B | 2,0 GB | `llama-csharp` |

Oba modely splňují požadovaný limit 4 GiB.

## Postup experimentu

1. Stažení a zprovoznění Ollamy na Windows.
2. Stažení dvou jazykových modelů.
3. Customizace pomocí souborů `Modelfile.qwen` a `Modelfile.llama`. Oba modely mají stejné systémové instrukce pro opravu C# kódu, odpovědi v češtině, teplotu 0,1 a seed 42.
4. Vytvoření konzolového programu v C#, který komunikuje s lokálním Ollama API.
5. Otestování obou modelů na deseti stejných ukázkách chybného kódu. Každá úloha byla samostatným API požadavkem; před měřením byl model zahřát.
6. Měření doby odpovědi, uložení odpovědí do CSV a ruční posouzení jejich kvality.
7. Rozšíření projektu o webovou aplikaci a automatickou kontrolu kompilace.

## Výsledky základního experimentu

| Kritérium | Qwen 2.5 | Llama 3.2 |
|---|---:|---:|
| Počet úloh | 10 | 10 |
| Celkový měřený čas | 96,55 s | 80,43 s |
| Průměrný čas odpovědi | 9,66 s | 8,04 s |

Llama potřebovala přibližně o 16,7 % méně času. Qwen poskytoval jazykově konzistentnější odpovědi. Oba modely dokázaly navrhovat opravy, ale jejich vysvětlení nebyla vždy přesná. Například Qwen u dělení nulou nabídl i operátor zbytku po dělení, který problém s nulovým dělitelem neřeší. Oba modely také někdy nesprávně vysvětlily příčinu kompilační chyby.

Kompletní odpovědi a naměřené časy základního experimentu jsou v souboru `vysledky.csv`. Opravy v tomto měření nebyly automaticky kompilovány, proto z něj nelze vyvozovat objektivní procento funkčně správných řešení.

## Rozšířená webová aplikace

Projekt byl rozšířen pomocí ASP.NET Core, Blazoru, Roslynu a SQLite. Rozšířená verze obsahuje editor kódu, porovnání modelů, kontrolu kompilace, automatickou samoopravu na základě chyb kompilátoru (nejvýše tři pokusy), měření času a tokenů, benchmark s 30 úlohami ve třech obtížnostech, historii a export CSV.

Při ověřovacím testu webové aplikace s chybným cyklem a chybějícím středníkem oba modely dosáhly úspěšné kompilace na první pokus:

| Model | Čas odpovědi | Vygenerované tokeny |
|---|---:|---:|
| Qwen 2.5 | 29,85 s | 674 |
| Llama 3.2 | 10,42 s | 206 |

Úspěšná kompilace nezaručuje správnost algoritmu. Kód generovaný AI se automaticky nespouští. Výsledky úplného benchmarku s 30 úlohami a vliv samoopravy zatím nejsou vyhodnoceny.

## Spuštění projektu

Požadavky: Ollama, .NET SDK odpovídající cílové verzi v souboru `.csproj` a dostupné balíčky NuGet.

Spuštění serveru Ollamy v samostatném terminálu:

```powershell
ollama serve
```

Pokud server již běží, není nutné jej spouštět znovu. V druhém terminálu ve složce projektu:

```powershell
ollama pull qwen2.5:3b
ollama pull llama3.2:3b
ollama create qwen-csharp -f Modelfile.qwen
ollama create llama-csharp -f Modelfile.llama
dotnet restore
dotnet run
```

U přenosné Windows verze je potřeba místo `ollama` použít cestu k `ollama.exe` (například `..\ollama.exe`, pokud je o složku výše). Webová aplikace se otevře na adrese vypsané v terminálu a komunikuje s Ollamou přes `http://localhost:11434`.

## Vyhodnocení a závěr

Experiment ukázal, že malé lokální jazykové modely mohou být užitečným pomocníkem při programování. Llama byla v základním měření rychlejší, Qwen měl konzistentnější češtinu. Výhodou lokálního řešení je možnost práce bez internetu po stažení potřebných komponent a zpracování zdrojového kódu na vlastním počítači.

Pro praktické využití je nutné kontrolovat výstupy kompilátorem, ověřovat logickou správnost pomocí testů a ruční kontroly a zajistit dostatečný výkon počítače. Modely nelze považovat za plně spolehlivou náhradu programátora.

Samotné modely nejsou součástí odevzdávaného repozitáře. Po dokončení měření je potřeba lokální modely odstranit podle zadání.
