# Dokumentacja XMLValidator - Walidator XML dla Eksponatów Muzealnych

## Spis treści

- Przegląd  
- Instalacja i konfiguracja  
- Struktura klasy  
- Metody publiczne  
- Metody prywatne  
- Schemat XSD  
- Przykłady użycia  
- Obsługa błędów  
- Rozszerzenia i customizacja  

---

## Przegląd

Klasa XMLValidator jest narzędziem do walidacji dokumentów XML opisujących eksponaty muzealne. Zapewnia kompleksową walidację zarówno strukturalną (zgodność ze schematem XSD) jak i semantyczną (reguły biznesowe specyficzne dla muzeum).  
Główne funkcjonalności:

- Walidacja przeciwko schematowi XSD  
- Sprawdzanie reguł biznesowych  
- Walidacja formatów dat historycznych  
- Kontrola poprawności wymiarów i jednostek  
- Weryfikacja plików multimedialnych  
- Generowanie szczegółowych raportów błędów  

---

## Instalacja i konfiguracja

### Wymagania:

- .NET Framework 4.7.2 lub nowszy / .NET Core 3.1+  
- System.Xml.Linq  
- System.Xml.Schema  

### Użycie w projekcie:

```csharp
using MuseumXMLAPI.Utilities;

// Utworzenie instancji walidatora
var validator = new XMLValidator();
```

---

## Struktura klasy

### Stałe i pola prywatne:

```csharp
private const string MUSEUM_NAMESPACE = "http://museum.example.com/exhibit";
private readonly XmlSchemaSet _schemaSet;
private readonly List<string> _validationErrors;
```

### Właściwości publiczne:

- `ValidationErrors` - Lista błędów z ostatniej walidacji (tylko do odczytu)  
- `IsValid` - Wskazuje czy ostatnia walidacja zakończyła się sukcesem  

---

## Metody publiczne

### ValidateExhibitXML(string xmlContent)

Główna metoda walidująca dokument XML eksponatu.

- **Parametry:**  
  `xmlContent` - Zawartość XML do walidacji  
- **Zwraca:**  
  `bool` - true jeśli dokument jest poprawny, false w przeciwnym przypadku  

#### Przykład użycia:

```csharp
var validator = new XMLValidator();
string xmlContent = File.ReadAllText("exhibit.xml");

bool isValid = validator.ValidateExhibitXML(xmlContent);

if (isValid)
{
    Console.WriteLine("Dokument XML jest poprawny!");
}
else
{
    Console.WriteLine("Znaleziono błędy:");
    foreach (string error in validator.ValidationErrors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

---

### GetValidationReport()

Generuje szczegółowy raport walidacji w formacie tekstowym.

- **Zwraca:**  
  `string` - Sformatowany raport z wynikami walidacji  

#### Przykład użycia:

```csharp
var validator = new XMLValidator();
validator.ValidateExhibitXML(xmlContent);

string report = validator.GetValidationReport();
File.WriteAllText("validation_report.txt", report);
```

---

## Metody statyczne pomocnicze

### HasValue(XElement element)

Sprawdza czy element XML istnieje i ma niepustą wartość.

- **Parametry:**  
  `element` - Element XML do sprawdzenia  
- **Zwraca:**  
  `bool` - true jeśli element ma wartość  

#### Przykład użycia:

```csharp
XElement titleElement = doc.Element("Title");
if (XMLValidator.HasValue(titleElement))
{
    Console.WriteLine($"Tytuł: {titleElement.Value}");
}
```

### SanitizeXML(string xmlContent)

Oczyszcza XML z potencjalnie niebezpiecznych elementów.

- **Parametry:**  
  `xmlContent` - Zawartość XML do oczyszczenia  
- **Zwraca:**  
  `string` - Oczyszczona zawartość XML  

#### Przykład użycia:

```csharp
string unsafeXml = "<!-- komentarz --><?xml-stylesheet type='text/xsl'?><root>dane</root>";
string safeXml = XMLValidator.SanitizeXML(unsafeXml);
```

---

## Metody prywatne

### ValidateAgainstSchema(XDocument doc)

Waliduje dokument przeciwko schematowi XSD.

- Sprawdza zgodność struktury XML ze schematem  
- Zbiera błędy i ostrzeżenia schematu  
- Dodaje błędy do listy _validationErrors  

### ValidateBusinessRules(XDocument doc)

Waliduje reguły biznesowe specyficzne dla muzeum.  
Sprawdza:

- Poprawność ID eksponatu  
- Kompletność sekcji BasicInfo  
- Prawidłowość wymiarów  
- Lokalizację eksponatu  
- Daty historyczne  
- Pliki multimedialne  
- Stan zachowania  

### ValidateExhibitID(XElement exhibit)

Sprawdza format ID eksponatu (EXH001, EXH002, etc.).  

Przykłady poprawnych ID:  

- EXH001  
- EXH999  
- EXH123  

Przykłady niepoprawnych ID:  

- EX001 (za krótkie)  
- EXH0001 (za długie)  
- EXH000 (zero)  

### ValidateBasicInfo(XElement basicInfo, XNamespace ns)

Waliduje sekcję podstawowych informacji o eksponacie.  
Sprawdza:

- Obecność i długość tytułu (max 255 znaków)  
- Wymaganą kategorię  
- Format daty stworzenia (jeśli podana)  

### ValidateDimensions(XElement dimensions, XNamespace ns)

Waliduje wymiary eksponatu.  

Obsługiwane jednostki:

- cm (centymetry)  
- mm (milimetry)  
- m (metry)  
- in (cale)  
- ft (stopy)  

Sprawdza:

- Poprawność jednostki  
- Wartości liczbowe > 0  
- Maksymalny rozmiar 10000 jednostek  
- Obecność co najmniej jednego wymiaru  

### ValidateLocation(XElement location, XNamespace ns)

Waliduje lokalizację eksponatu w muzeum.  

Wymagane pola:

- Building (budynek)  
- Room (pokój)  
- onDisplay (atrybut boolean)  

Dodatkowe wymagania:

- Jeśli onDisplay="true", pole Display jest wymagane  

### ValidateDates(XElement history, XNamespace ns)

Waliduje daty w historii eksponatu.  

Sprawdza:

- Data nabycia nie może być w przyszłości  
- Data nabycia nie może być wcześniej niż 1800 rok  
- Cena nabycia musi być liczbą ≥ 0  
- Daty wystaw (data rozpoczęcia < data zakończenia)  

### ValidateMedia(XElement media, XNamespace ns)

Waliduje sekcję mediów (obrazy).  

Sprawdza:

- Maksymalnie jeden główny obraz (primary="true")  
- Wymagane nazwy plików  
- Poprawne rozszerzenia plików obrazów  
- Długość nazwy pliku (max 255 znaków)  

Obsługiwane formaty obrazów:

- .jpg, .jpeg  
- .png  
- .gif  
- .bmp  
- .tiff  
- .webp  

### ValidateCondition(XElement technical, XNamespace ns)

Waliduje stan zachowania eksponatu.  

Dozwolone wartości:

- Excellent (doskonały)  
- Good (dobry)  
- Fair (zadowalający)  
- Poor (słaby)  
- Critical (krytyczny)  

---

## Walidacja dat historycznych

Klasa obsługuje różne formaty dat historycznych:

Obsługiwane formaty:

```csharp
// Daty przed naszą erą
"500 p.n.e."
"100 p.n.e."

// Daty naszej ery
"1500"
"1800 n.e."

// Zakresy dat
"500 p.n.e. - 400 p.n.e."
"1400 - 1500"

// Standardowe daty
"12.05.1900"
"2023-01-15"
```

---

## Schemat XSD

Walidator używa wbudowanego schematu XSD definiującego strukturę dokumentu XML eksponatu:

Główne elementy:

- Exhibit (root) - główny element z atrybutami id i status  
- BasicInfo - podstawowe informacje  
- Description - opisy  
- Technical - dane techniczne  
- Location - lokalizacja  
- History - historia (opcjonalna)  
- Media - pliki multimedialne (opcjonalne)  

Typy wyliczeniowe:

- CategoryType - kategorie eksponatów  
- StatusType - statusy eksponatów  
- ConditionType - stany zachowania  
- AcquisitionMethodType - metody nabycia  

---

## Przykłady użycia

### Podstawowa walidacja:

```csharp
var validator = new XMLValidator();
string xml = @"<?xml version='1.0' encoding='UTF-8'?>
<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH001' status='Active'>
    <BasicInfo>
        <Title>Starożytna amfora</Title>
        <Category>Pottery</Category>
        <Creator>Nieznany</Creator>
        <DateCreated>500 p.n.e.</DateCreated>
    </BasicInfo>
    <Description>
        <ShortDescription>Grecka amfora z V wieku p.n.e.</ShortDescription>
    </Description>
    <Technical>
        <Dimensions unit='cm'>
            <Height>45.5</Height>
            <Diameter>20.0</Diameter>
        </Dimensions>
        <Material>Ceramika</Material>
        <Condition>Good</Condition>
    </Technical>
    <Location onDisplay='true'>
        <Building>Budynek główny</Building>
        <Room>Sala 1</Room>
        <Display>Gablota A</Display>
    </Location>
</Exhibit>";

bool isValid = validator.ValidateExhibitXML(xml);
Console.WriteLine($"Walidacja: {(isValid ? "SUKCES" : "BŁĄD")}");
```

### Walidacja z obsługą błędów:

```csharp
var validator = new XMLValidator();

try 
{
    bool result = validator.ValidateExhibitXML(xmlContent);
    
    if (!result)
    {
        Console.WriteLine("Błędy walidacji:");
        foreach (var error in validator.ValidationErrors)
        {
            Console.WriteLine($"❌ {error}");
        }
        
        // Zapisz raport do pliku
        string report = validator.GetValidationReport();
        File.WriteAllText($"validation_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt", report);
    }
    else
    {
        Console.WriteLine("✅ Dokument XML jest poprawny!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Błąd podczas walidacji: {ex.Message}");
}
```

### Walidacja batch (wielu plików):

```csharp
var validator = new XMLValidator();
var results = new List<(string fileName, bool isValid, int errorCount)>();

string[] xmlFiles = Directory.GetFiles("exhibits", "*.xml");

foreach (string file in xmlFiles)
{
    string content = File.ReadAllText(file);
    bool isValid = validator.ValidateExhibitXML(content);
    
    results.Add((Path.GetFileName(file), isValid, validator.ValidationErrors.Count));
    
    if (!isValid)
    {
        string reportFile = Path.ChangeExtension(file, ".validation.txt");
        File.WriteAllText(reportFile, validator.GetValidationReport());
    }
}

// Podsumowanie
Console.WriteLine("Podsumowanie walidacji:");
foreach (var result in results)
{
    string status = result.isValid ? "✅ OK" : $"❌ Błędów: {result.errorCount}";
    Console.WriteLine($"{result.fileName}: {status}");
}
```

---

## Obsługa błędów

### Typy błędów:

- Błędy parsowania XML - niepoprawna struktura XML  
- Błędy schematu - niezgodność ze schematem XSD  
- Błędy reguł biznesowych - naruszenie logiki muzealnej  

### Przykłady komunikatów błędów:

- Zawartość XML nie może być pusta  
- Błąd parsowania XML: Unexpected end of file  
- Błąd schematu: Element 'Title' is required  
- Niepoprawny format ID eksponatu (oczekiwany: EXH001, EXH002, etc.)  
- Tytuł eksponatu nie może być dłuższy niż 255 znaków  
- Data naby
