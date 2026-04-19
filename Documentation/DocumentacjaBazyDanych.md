# Dokumentacja Bazy Danych Museum XML Archive

## Spis Treści
1. [Przegląd](#przegląd)
2. [Struktura Bazy Danych](#struktura-bazy-danych)
3. [Schemat XML](#schemat-xml)
4. [Tabele](#tabele)
5. [Widoki](#widoki)
6. [Procedury Składowane](#procedury-składowane)
7. [Bezpieczeństwo i Wydajność](#bezpieczeństwo-i-wydajność)
8. [Przykłady Użycia](#przykłady-użycia)

## Przegląd

Baza danych Museum XML Archive to wyspecjalizowany system przechowywania, zarządzania i wyszukiwania dokumentów muzealnych w formacie XML. System oferuje kompleksowe funkcje zarządzania cyklem życia dokumentów, walidacji, wyszukiwania oraz raportowania.

### Kluczowe Funkcje
- Natywne przechowywanie XML z walidacją schematu
- Możliwość wyszukiwania pełnotekstowego i strukturalnego
- Wersjonowanie dokumentów oraz śledzenie zmian (audit trail)
- Wydajna indeksacja XML
- Rozbudowane raportowanie i analizy
- Automatyczne procedury utrzymaniowe

## Struktura Bazy Danych

### Konfiguracja Bazy
- **Nazwa bazy:** MuseumXMLArchive  
- **Plik danych:** C:\Database\MuseumXMLArchive_Data.mdf  
- **Plik dziennika:** C:\Database\MuseumXMLArchive_Log.ldf  
- **Porządkowanie znaków:** Domyślne dla SQL Server  
- **Model odzyskiwania:** Full (zalecany dla produkcji)

## Schemat XML

Baza wykorzystuje kolekcję schematów XML (`ExhibitSchemaCollection`) definiującą strukturę dokumentów muzealnych.

### Namespace schematu
```xml
http://museum.example.com/exhibit
```

### Struktura elementu głównego
```xml
<Exhibit id="string" status="StatusType">
  <BasicInfo>...</BasicInfo>
  <Description>...</Description>
  <Technical>...</Technical>
  <Location onDisplay="boolean">...</Location>
  <History>...</History> <!-- Opcjonalne -->
  <Media>...</Media> <!-- Opcjonalne -->
</Exhibit>
```

### Typy enumerowane

#### CategoryType
- Painting (Malarstwo)
- Sculpture (Rzeźba)
- Pottery (Ceramika)
- Jewelry (Biżuteria)
- Textile (Tekstylia)
- Weapon (Broń)
- Tool (Narzędzie)
- Coin (Moneta)
- Manuscript (Manuskrypt)
- Photograph (Fotografia)
- Other (Inne)

#### StatusType
- Active (Aktywny)
- OnLoan (Wypożyczony)
- InConservation (W konserwacji)
- Deaccessioned (Wycofany)
- Missing (Zaginiony)

#### ConditionType
- Excellent (Doskonały)
- Good (Dobry)
- Fair (Zadowalający)
- Poor (Słaby)
- Critical (Krytyczny)

#### AcquisitionMethodType
- Purchase (Zakup)
- Donation (Darowizna)
- Bequest (Zapis)
- Exchange (Wymiana)
- Transfer (Transfer)
- Found (Znaleziony)

## Tabele

### 1. Documents (Tabela główna)

**Cel:** Przechowuje dokumenty XML reprezentujące eksponaty muzealne wraz z walidacją i metadanymi.

| Kolumna | Typ | Ograniczenia | Opis |
|---------|-----|--------------|------|
| DocumentID | INT IDENTITY(1,1) | PRIMARY KEY | Unikalny identyfikator, autoinkrementacja |
| ExhibitID | NVARCHAR(50) | NOT NULL, UNIQUE | Identyfikator biznesowy eksponatu |
| DocumentName | NVARCHAR(255) | NOT NULL | Nazwa dokumentu czytelna dla człowieka |
| XMLContent | XML(ExhibitSchemaCollection) | NOT NULL | Zawartość XML walidowana schematem |
| CreatedDate | DATETIME2 | DEFAULT GETDATE() | Data utworzenia dokumentu |
| ModifiedDate | DATETIME2 | DEFAULT GETDATE() | Data ostatniej modyfikacji |
| CreatedBy | NVARCHAR(100) | DEFAULT SYSTEM_USER | Użytkownik tworzący dokument |
| ModifiedBy | NVARCHAR(100) | DEFAULT SYSTEM_USER | Użytkownik modyfikujący dokument |
| IsActive | BIT | DEFAULT 1 | Flaga miękkiego usunięcia (soft delete) |
| ValidationStatus | NVARCHAR(20) | DEFAULT 'Valid' | Status walidacji XML |
| Notes | NVARCHAR(MAX) | NULL | Dodatkowe notatki lub komentarze |

**Triggery:**  
- `TR_Documents_UpdateModifiedDate`: Automatyczna aktualizacja ModifiedDate  
- `TR_Documents_LogOperations`: Logowanie operacji DML

### 2. DocumentOperationLog (Tabela audytowa)

**Cel:** Szczegółowy dziennik operacji na bazie.

| Kolumna | Typ | Opis |
|---------|-----|------|
| LogID | INT IDENTITY(1,1) | Klucz główny |
| DocumentID | INT | Odniesienie do Documents (może być NULL) |
| OperationType | NVARCHAR(50) | Typ operacji (INSERT, UPDATE, DELETE, SEARCH itd.) |
| OperationDescription | NVARCHAR(500) | Szczegółowy opis operacji |
| ExecutedBy | NVARCHAR(100) | Użytkownik wykonujący operację |
| ExecutedDate | DATETIME2 | Data i czas wykonania |
| Success | BIT | Status powodzenia operacji |
| ErrorMessage | NVARCHAR(MAX) | Szczegóły błędu w razie niepowodzenia |

### 3. SearchCache (Tabela wydajnościowa)

**Cel:** Buforuje wyniki wyszukiwania dla lepszej wydajności.

| Kolumna | Typ | Opis |
|---------|-----|-----|
| SearchID | INT IDENTITY(1,1) | Klucz główny |
| SearchQuery | NVARCHAR(MAX) | Wykonane zapytanie wyszukiwania |
| SearchType | NVARCHAR(50) | Typ wyszukiwania (XPath, XQuery, FullText) |
| ResultXML | XML | Buforowane wyniki w formacie XML |
| CreatedDate | DATETIME2 | Data utworzenia wpisu w cache |
| ExpirationDate | DATETIME2 | Data wygaśnięcia cache (domyślnie 24h) |

## Widoki

### VW_ExhibitSummary

**Cel:** Spłaszczony widok najważniejszych informacji o eksponatach, ułatwiający szybki dostęp i raportowanie.

**Kolumny:**  
DocumentID, ExhibitID, DocumentName  
Title, Category, Creator, Period  
Status, OnDisplay  
CreatedDate, ModifiedDate, ValidationStatus

**Zastosowanie:** Idealny do dashboardów, szybkich wyszukiwań i raportów bez konieczności parsowania XML.

## Procedury Składowane

### Procedury Zarządzania Dokumentami

#### SP_InsertDocument  
**Cel:** Wstawia nowy dokument XML z walidacją.

**Parametry:**  
- `@ExhibitID NVARCHAR(50)` – unikalny identyfikator eksponatu  
- `@DocumentName NVARCHAR(255)` – nazwa dokumentu  
- `@XMLContent XML` – zawartość XML  
- `@CreatedBy NVARCHAR(100)` – opcjonalny użytkownik tworzący  
- `@DocumentID INT OUTPUT` – zwraca ID nowego dokumentu

**Funkcje:**  
- Walidacja XML według schematu  
- Sprawdzanie duplikatów ExhibitID  
- Operacja w transakcji  
- Rozbudowana obsługa błędów

#### SP_UpdateDocument  
**Cel:** Aktualizuje istniejący dokument XML.

**Parametry:**  
- `@DocumentID INT` – ID dokumentu do aktualizacji  
- `@DocumentName NVARCHAR(255)` – opcjonalna nowa nazwa  
- `@XMLContent XML` – nowa zawartość XML  
- `@ModifiedBy NVARCHAR(100)` – opcjonalny użytkownik modyfikujący  
- `@Notes NVARCHAR(MAX)` – opcjonalne dodatkowe notatki

#### SP_GetDocumentById  
**Cel:** Pobiera dokument po ID.

**Parametry:**  
- `@DocumentID INT` – ID dokumentu  
- `@IncludeInactive BIT` – czy uwzględniać dezaktywowane

#### SP_GetDocumentByExhibitId  
**Cel:** Pobiera dokument po ExhibitID.

**Parametry:**  
- `@ExhibitID NVARCHAR(50)` – identyfikator eksponatu

#### SP_DeactivateDocument  
**Cel:** Miękkie usunięcie dokumentu (IsActive = 0).

**Parametry:**  
- `@DocumentID INT` – dokument do dezaktywacji  
- `@ModifiedBy NVARCHAR(100)` – opcjonalny użytkownik  
- `@Reason NVARCHAR(255)` – powód dezaktywacji

#### SP_DeleteDocumentByID  
**Cel:** Trwałe usunięcie dokumentu po ID.

**Parametry:**  
- `@DocumentID INT`

#### SP_DeleteDocumentsByExhibitID  
**Cel:** Trwałe usunięcie dokumentów po ExhibitID.

**Parametry:**  
- `@ExhibitID NVARCHAR(50)`

### Procedury Wyszukiwania

#### SP_SearchByCategory  
**Cel:** Wyszukiwanie dokumentów według kategorii eksponatu.

**Parametry:**  
- `@Category NVARCHAR(100)`  
- `@OnDisplayOnly BIT` – ograniczenie do eksponatów na ekspozycji

**Zwraca:** Zestaw wyników z podsumowaniem eksponatów.

#### SP_SearchFullText  
**Cel:** Wyszukiwanie pełnotekstowe w wielu polach XML.

**Parametry:**  
- `@SearchText NVARCHAR(500)

**Zwraca:** Dokumenty dopasowane do frazy.

### Procedury Administracyjne

#### SP_ValidateAllDocuments  
**Cel:** Przeprowadza walidację wszystkich dokumentów i generuje raport.

#### SP_RebuildIndexes  
**Cel:** Odbudowuje indeksy XML i pełnotekstowe dla optymalizacji wydajności.

## Bezpieczeństwo i Wydajność

- Kontrola dostępu oparta na rolach (RBAC) – definiowanie ról i uprawnień  
- Audyt operacji w DocumentOperationLog  
- Regularne backupy i plany odzyskiwania danych  
- Indeksy XML na elementach kluczowych (ExhibitID, Category, Status)  
- Wykorzystanie cache wyników wyszukiwania  
- Ustawienia izolacji transakcji dla operacji krytycznych

## Przykłady Użycia

### Dodanie nowego dokumentu

```sql
DECLARE @NewDocID INT;

EXEC SP_InsertDocument 
  @ExhibitID = 'EXH12345',
  @DocumentName = 'Mona Lisa Description',
  @XMLContent = @xmlData,
  @CreatedBy = 'Curator01',
  @DocumentID = @NewDocID OUTPUT;

SELECT @NewDocID AS NewDocumentID;
```

### Wyszukiwanie eksponatów malarstwa na ekspozycji

```sql
EXEC SP_SearchByCategory
  @Category = 'Painting',
  @OnDisplayOnly = 1;
```