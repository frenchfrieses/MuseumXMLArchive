# Dokumentacja API XML Museum Archive

## Przydatne Linki

- [Dokumentacja API](#)
- [Dokumentacja testów jednostkowych](#)
- [Dokumentacja bazy danych](#)
- [Dokumentacja klasy XML Validator](#)

---

## Opis Projektu

**Muzealne Archiwum Dokumentów XML** to zaawansowany system zarządzania dokumentami XML służący do digitalizacji i archiwizacji opisów eksponatów muzealnych. System został zaprojektowany z myślą o nowoczesnych wymaganiach instytucji kultury, oferując kompleksowe rozwiązanie do zarządzania dziedzictwem kulturowym.

### Główne Funkcjonalności

- Przechowywanie dokumentów XML w natywnym typie danych XML w SQL Server  
- Zarządzanie dokumentami (dodawanie, usuwanie, modyfikacja, wersjonowanie)  
- Wyszukiwanie fragmentów dokumentów za pomocą XPath/XQuery  
- Generowanie raportów statystycznych i analitycznych  
- Walidacja dokumentów XML zgodnie ze schematami XSD  
- Import/Export danych w różnych formatach  

---

## Problem i Rozwiązanie

System rozwiązuje problem efektywnego zarządzania dziedzictwem kulturowym poprzez zastąpienie tradycyjnych form archiwizacji (papierowe katalogi, podstawowe bazy danych) nowoczesnym rozwiązaniem cyfrowym. Umożliwia to:

- Szybsze wyszukiwanie eksponatów  
- Lepszą organizację metadanych  
- Standardyzację opisów  
- Łatwą integrację z innymi systemami muzealnymi  

---

## Instalacja

### Wymagania Systemowe

#### Środowisko Deweloperskie

- Visual Studio 2022 (Community/Professional)  
- SQL Server Management Studio (SSMS)  
- Git do kontroli wersji  
- Windows 10/11 lub Windows Server 2019+  

#### Technologie Backend

- **Microsoft SQL Server** (wersja 2019 lub nowsza)  
  - Natywny typ danych XML  
  - Obsługa XPath/XQuery  
  - Procedury składowane T-SQL  

- **.NET Framework 4.8** lub **.NET 6/7**  
- ADO.NET do połączenia z bazą danych  

#### Biblioteki Dodatkowe

- `System.Data.SqlClient` lub `Microsoft.Data.SqlClient`  
- `System.Xml.Linq` (LINQ to XML)  
- `NUnit` lub `MSTest` do testów jednostkowych  

---

### Kroki Instalacji

#### 1. Sklonuj repozytorium

```bash
git clone https://github.com/your-repo/MuseumXMLArchive.git
cd MuseumXMLArchive
```

#### 2. Skonfiguruj bazę danych

```sql
-- Uruchom w SSMS w następującej kolejności:
-- 1. Database/CreateDatabase.sql
-- 2. Database/CreateTables.sql
-- 3. Database/StoredProcedures.sql
-- 4. Database/SampleData.sql (opcjonalnie
```

#### 3. Uzyskaj connection string do bazy danych

```sql
-- Uruchom w SSMS:
-- Database/QueryForAConnectionString.sql
```

#### 4. Zbuduj rozwiązanie

```bash
dotnet build
# lub w Visual Studio: Build > Build Solution
```

#### 5. Uruchom testy
```bash
dotnet test
# lub w Visual Studio: MuseumXMLAPI.Tests > Run Tests
```

## Przykłady użycia

### Podstawowe zarządzanie dokumentem

```csharp
// Initialize and connect
var api = new MuseumXMLAPI();
api.Connect("Server=localhost;Database=MuseumDB;Trusted_Connection=true;");

// Add a document
string xml = @"<?xml version='1.0'?>
<exhibit>
    <title>Ming Dynasty Vase</title>
    <category>Ceramics</category>
    <period>Ming Dynasty</period>
    <condition>Excellent</condition>
    <location>Storage Room A</location>
</exhibit>";

int docId = api.AddDocument("EXH002", "Ming Vase", xml, "curator");

// Retrieve and update
var doc = api.GetDocument(docId);
if (doc != null)
{
    api.UpdateDocument(docId, "Ming Dynasty Vase - Updated", xml, "curator");
}

// Search operations
var ceramics = api.SearchByCategory("Ceramics");
var mingDynasty = api.SearchByPeriod("Ming Dynasty");
var conservationNeeded = api.GetDocumentsRequiringConservation();

// Generate reports
var report = api.GenerateArchiveReport();
var stats = api.GetCategoryStatistics();

// Cleanup
api.Disconnect();
```

### Zaawansowane szukanie i Export

```csharp
// Complex XPath search
var results = api.SearchByXPath("//exhibit[condition='Needs Conservation' and category='Textiles']");

// Export matching documents
foreach (var result in results)
{
    string filename = $"export_{result.ExhibitId}.xml";
    api.ExportDocumentToFile(result.DocumentId, filename);
}

// Full-text search with caching
var searchResults = api.SearchFullText("ancient roman");
api.CleanExpiredSearchCache(30); // Clean cache older than 30 minutes
```

## Literatura
* Elliotte Rusty Harold, W. Scott Means, „XML in a Nutshell”, O'Reilly, 2004.
* David Hunter et al., „Beginning XML”, Wrox Press, 2007.
* Anne Gentle, „Docs Like Code”, Leanpub, 2017.
* Thomas M. Connolly, Carolyn E. Begg, „Systemy baz danych”, Warszawa, RM 2004.
* Ramez Elmasri, Shamkant B. Navathe, Wprowadzenie do systemów baz danych. Wydanie VII, Helion 2019
* Adam Pelikant, MS SQL Server. Zaawansowane metody programowania, Helion 2014
