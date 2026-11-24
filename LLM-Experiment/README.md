\# Experiment s lokálním LLM



\## Úloha



Porovnání dvou lokálních jazykových modelů při hledání a opravování chyb v programovacím jazyce C#.



\## Použité modely



\- Qwen 2.5 3B (1,9 GB)

\- Llama 3.2 3B (2 GB)



Oba modely splňují požadovaný limit 4 GiB.



\## Postup



1\. Stažení a zprovoznění Ollamy.

2\. Stažení dvou jazykových modelů.

3\. Customizace modelů pomocí souborů Modelfile.

4\. Nastavení systémových instrukcí, teploty 0,1 a seedu 42.

5\. Vytvoření konzolové aplikace v C# komunikující s Ollama API.

6\. Automatické otestování deseti chybných ukázek kódu na každém modelu.

7\. Měření rychlosti odpovědí a uložení výsledků do CSV.



\## Výsledky



| Model | Průměrný čas | Celkový čas |

|---|---:|---:|

| Qwen 2.5 | 9,66 s | 96,55 s |

| Llama 3.2 | 8,04 s | 80,43 s |



Llama potřebovala přibližně o 16,7 % méně času než Qwen. Qwen však poskytoval kvalitnější odpovědi v českém jazyce.



Oba modely dokázaly navrhovat opravy jednoduchých chyb, ale některá vysvětlení byla nepřesná.



Kompletní výsledky jsou dostupné v souboru vysledky.csv.



\## Spuštění projektu



Při spuštěné Ollamě je potřeba vytvořit customizované modely:



```powershell

ollama create qwen-csharp -f Modelfile.qwen

ollama create llama-csharp -f Modelfile.llama

```



Následně spustit aplikaci:



```powershell

dotnet run

```



\## Závěr



Lokální jazykové modely lze využít jako pomocný nástroj při programování. Jejich výhodou je fungování bez připojení k internetu a soukromí zdrojového kódu.



Pro praktické využití je však nutné ověřovat správnost odpovědí, kontrolovat vygenerovaný kód pomocí kompilátoru a provádět automatické testy.



Samotné jazykové modely nejsou součástí repozitáře.

