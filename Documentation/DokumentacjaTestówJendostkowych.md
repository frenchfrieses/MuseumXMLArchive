# Dokumentacja Testów Jednostkowych - MuseumXMLAPI

## Spis Treści
- [Przegląd](#przegląd)
- [Konfiguracja Testów](#konfiguracja-testów)
- [Przypadki Testowe](#przypadki-testowe)
- [Dane Testowe](#dane-testowe)
- [Wyniki i Metryki](#wyniki-i-metryki)
- [Rekomendacje](#rekomendacje)

## Przegląd
### Cel Testów
Niniejsza dokumentacja przedstawia kompleksowy zestaw testów jednostkowych dla API **MuseumXMLAPI**, zaprojektowanych w celu weryfikacji poprawności działania systemu zarządzania dokumentami XML w kontekście archiwum muzealnego.

### Środowisko Testowe
- **Framework:** Microsoft Visual Studio Test Tools (MSTest)  
- **Język:** C#  
- **Baza danych:** SQL Server LocalDB (MuseumXMLArchive)  
- **Namespace:** `MuseumXMLAPI.Tests`

### Zakres Testów
Testy obejmują następujące obszary funkcjonalne:
- Zarządzanie połączeniami z bazą danych
- Operacje CRUD na dokumentach XML
- Walidację dokumentów XML
- Funkcje wyszukiwania i filtrowania
- Generowanie raportów
- Obsługę zdarzeń systemowych
- Operacje importu/eksportu

## Konfiguracja Testów
### Struktura Klasy Testowej
```csharp
[TestClass]
public class APITests
{
    private MuseumXMLAPI _api;
    private string _testConnectionString;
    private string _testExhibitId;
    private int _testDocumentId;
    private List<int> _createdDocumentIds;
    private List<string> _createdExhibitIds;
}
```

### Inicjalizacja i Czyszczenie
- **TestInitialize**
  - Inicjalizacja instancji API
  - Konfiguracja parametrów testowych
  - Nawiązanie połączenia z bazą testową
  - Ustawienie obsługi zdarzeń

- **TestCleanup**
  - Usunięcie utworzonych dokumentów testowych
  - Czyszczenie danych ekshibitów
  - Rozłączenie z bazą danych
  - Zwolnienie zasobów

- **ClassCleanup**
  - Oczyszczenie artefaktów na poziomie klasy
  - Finalizacja środowiska testowego

## Przypadki Testowe
### 1. Zarządzanie Połączeniami
- `Connect_ValidConnectionString_ReturnsTrue`
- `Connect_InvalidConnectionString_ReturnsFalse`
- `IsConnected_WhenConnected_ReturnsTrue`
- `Disconnect_WhenConnected_DisconnectsSuccessfully`

### 2. Zarządzanie Dokumentami
- `AddDocument_ValidDocument_ReturnsValidDocumentId`
- `AddDocument_InvalidXML_ReturnsNegativeOne`
- `AddDocument_NullOrEmptyParameters_ReturnsNegativeOne`
- `GetDocument_ExistingDocument_ReturnsDocumentInfo`
- `GetDocument_NonExistentDocument_ReturnsNull`
- `GetDocumentByExhibitId_ExistingExhibit_ReturnsDocumentInfo`
- `UpdateDocument_ValidUpdate_ReturnsTrue`
- `UpdateDocument_InvalidXML_ReturnsFalse`
- `DeactivateDocument_ExistingDocument_ReturnsTrue`
- `DeleteDocument_ExistingDocument_ReturnsTrue`
- `DeleteDocumentByExhibitId_ExistingExhibit_ReturnsTrue`

### 3. Walidacja Dokumentów
- `ValidateDocument_ValidXML_ReturnsTrue`
- `ValidateDocument_InvalidXML_ReturnsFalse`
- `ValidateDocument_ComplexValidXML_ReturnsTrue`
- `ValidateDocument_NullOrEmptyXML_ReturnsFalse`

### 4. Operacje Wyszukiwania
- `SearchByCategory_ExistingCategory_ReturnsResults`
- `SearchByCategory_NonExistentCategory_ReturnsEmptyList`
- `SearchFullText_ExistingText_ReturnsResults`
- `SearchByPeriod_ExistingPeriod_ReturnsResults`
- `SearchByCondition_ExistingCondition_ReturnsResults`
- `SearchByXPath_ValidExpression_ReturnsResults`

### 5. Funkcje Raportowania
- `GenerateArchiveReport_WhenCalled_ReturnsReport`
- `GetCategoryStatistics_WhenCalled_ReturnsDictionary`
- `GetDocumentsRequiringConservation_WhenCalled_ReturnsList`
- `GetRecentDocuments_WithValidDays_ReturnsList`

### 6. Funkcje Użytkowe
- `CleanExpiredSearchCache_WhenCalled_RunsWithoutError`
- `GetDocumentOperationLog_ExistingDocument_ReturnsList`
- `ExportDocumentToFile_ExistingDocument_ReturnsTrue`
- `ImportDocumentFromFile_ValidFile_ReturnsValidDocumentId`

### 7. Obsługa Zdarzeń
- `DocumentAdded_EventFired_WhenDocumentAdded`

### 8. Testy Stanu Rozłączenia
- `AddDocument_WhenDisconnected_ReturnsNegativeOne`
- `GetDocument_WhenDisconnected_ReturnsNull`

## Dane Testowe
### Przykładowy Prawidłowy XML
```xml
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH001" status="Active">
    <BasicInfo>
        <Title>Starożytna Amfora Grecka</Title>
        <Category>Pottery</Category>
        <SubCategory>Amfora</SubCategory>
        <Creator>Nieznany Ceramik</Creator>
        <DateCreated>500 p.n.e. - 400 p.n.e.</DateCreated>
        <Period>Klasyczny</Period>
        <Culture>Grecka</Culture>
    </BasicInfo>
</Exhibit>
```

## Wyniki i Metryki

### Pokrycie uzyskane z coverlet

```bash
+--------------+-------+--------+--------+
| Module       | Line  | Branch | Method |
+--------------+------+--------+--------+
| MuseumXMLAPI | 85.9% | 77.9%  | 86.9%  |
+--------------+-------+--------+--------+

+---------+-------+--------+--------+
|         | Line  | Branch | Method |
+---------+-------+--------+--------+
| Total   | 85.9% | 77.9%  | 86.9%  |
+---------+-------+--------+--------+
| Average | 85.9% | 77.9%  | 86.9%  |
+---------+-------+--------+--------+
```

## Rekomendacje
- Uzupełnić testy o przypadki brzegowe (duże dokumenty, bardzo głębokie drzewo XML).
- Rozważyć testy integracyjne z front-endem.
- Zautomatyzować testy importu/eksportu w CI/CD.