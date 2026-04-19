
# Dokumentacja API XML Museum Archive

## Przegląd
API XML Muzeum to to interfejs oparty na .NET przeznaczona do zarządzania archiwami dokumentów XML w muzealnych systemach informatycznych. Oferuje funkcje przechowywania, pobierania, przeszukiwania oraz zarządzania dokumentacją eksponatów z pełną walidacją XML i integracją z bazą danych SQL.

## Architektura
API opiera się na klasie `MuseumXMLAPI`, implementującej interfejs `IMuseumXMLAPI`. Wykorzystuje SQL Server jako trwały magazyn danych oraz architekturę opartą na zdarzeniach do monitorowania operacji na dokumentach.

## Najważniejsze funkcje
- **Zarządzanie dokumentami**: pełne operacje CRUD na dokumentach XML.
- **Wyszukiwanie**: metody wyszukiwania oparte na XPath, pełnym tekście i kategoriach.
- **Walidacja**: wbudowana walidacja XML dla dokumentacji eksponatów.
- **Raportowanie**: funkcjonalność statystyk i generowania raportów.
- **System zdarzeń**: powiadomienia o operacjach na dokumentach w czasie rzeczywistym.
- **Cache**: buforowanie wyników wyszukiwania w celu poprawy wydajności.
- **Import/Eksport**: import i eksport dokumentów z/do plików.

## Typy Danych

### Modele Główne

#### `DocumentInfo`

Reprezentuje kompletny dokument muzealny wraz z metadanymi.

```csharp
public class DocumentInfo {
    public int DocumentId { get; set; }
    public string ExhibitId { get; set; }
    public string DocumentName { get; set; }
    public XmlDocument XMLContent { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
    public bool IsActive { get; set; }
}
```

#### `SearchResult`

Zawiera informacje podsumowujące wyniki zapytań wyszukiwania.

```csharp
public class SearchResult {
    public int DocumentId { get; set; }
    public string ExhibitId { get; set; }
    public string DocumentName { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public string Period { get; set; }
    public string Status { get; set; }
    public bool OnDisplay { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
```

#### `ArchiveReport`

Zawiera dane statystyczne o archiwum dokumentów.

```csharp
public class ArchiveReport {
    public int TotalDocuments { get; set; }
    public int ActiveDocuments { get; set; }
    public int InactiveDocuments { get; set; }
    public int DocumentsOnDisplay { get; set; }
    public int DocumentsInStorage { get; set; }
    public int DocumentsRequiringConservation { get; set; }
}
```

#### `OperationLog`

Rejestruje operacje wykonane na dokumentach.

```csharp
public class OperationLog {
    public int LogId { get; set; }
    public int DocumentId { get; set; }
    public string OperationType { get; set; }
    public string OperationDescription { get; set; }
    public string ExecutedBy { get; set; }
    public string ErrorMessage { get; set; }
}
```

### Argumenty Zdarzeń

#### `DocumentEventArgs`

```csharp
public class DocumentEventArgs : EventArgs {
    public int DocumentId { get; set; }
    public string ExhibitId { get; set; }
    public string DocumentName { get; set; }
}
```

#### `ErrorEventArgs`

```csharp
public class ErrorEventArgs : EventArgs {
    public string ErrorMessage { get; set; }
}
```

### Metody API

### Zarządzanie połączeniem

#### `Connect(string connectionString)`
Nawiazuje połączenie z bazą danych.

- **Parametry:**
  - `connectionString`: Łańcuch połączenia z bazą danych
- **Zwraca:** `bool` - `True` jeśli połączenie zostało nawiązane pomyślnie

```csharp
public bool Connect(string connectionString)
{
    try
    {
        _connectionString = connectionString;
        _connection = new SqlConnection(_connectionString);
        _connection.Open();
        _isConnected = true;
        return true;
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas nawiązywania połączenia: {ex.Message}"));
        return false;
    }
}
```

#### `IsConnected()`
Sprawdza czy połączenie jest aktywne.

- **Zwraca:** `bool` - `True` jeśli połączono

```csharp
public bool IsConnected()
{
    return _isConnected && _connection != null && _connection.State == ConnectionState.Open;
}
```

#### `Disconnect()`
Zamyka połączenie z bazą danych.

```csharp
public void Disconnect()
{
    try
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
        _isConnected = false;
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas zamykania połączenia: {ex.Message}"));
    }
}
```

---

### Zarządzanie dokumentami

#### `AddDocument(string exhibitId, string documentName, string xmlContent, string createdBy)`
Dodaje nowy dokument XML do archiwum.

- **Parametry:**
  - `exhibitId`: Identyfikator eksponatu
  - `documentName`: Nazwa dokumentu
  - `xmlContent`: Zawartość XML jako string
  - `createdBy`: Autor dokumentu
- **Zwraca:** `int` - ID utworzonego dokumentu lub `-1` w przypadku błędu

```csharp
public int AddDocument(string exhibitId, string documentName, string xmlContent, string createdBy)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return -1;
    }

    try
    {
        // Walidacja XML
        if (!ValidateDocument(xmlContent))
        {
            OnErrorOccurred(new ErrorEventArgs("Dokument XML nie jest poprawny"));
            return -1;
        }

        // Walidacja pozostałych parametrów
        if (exhibitId == null)
        {
            OnErrorOccurred(new ErrorEventArgs("exhibitId nie może mieć wartości null"));
            return -1;
        }

        if (documentName == null || documentName == "")
        {
            OnErrorOccurred(new ErrorEventArgs("documentName nie może być puste"));
            return -1;
        }

        using (var cmd = new SqlCommand("SP_InsertDocument", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);
            cmd.Parameters.AddWithValue("@DocumentName", documentName);
            cmd.Parameters.AddWithValue("@XMLContent", xmlContent);
            cmd.Parameters.AddWithValue("@CreatedBy", createdBy);

            var outputParam = new SqlParameter("@DocumentID", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputParam);

            cmd.ExecuteNonQuery();

            int documentId = (int)outputParam.Value;

            // Wywołanie eventu
            OnDocumentAdded(new DocumentEventArgs(documentId, exhibitId, documentName));

            return documentId;
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas dodawania dokumentu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"General Error: {ex.Message}");
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas dodawania dokumentu: {ex.Message}"));
    }
    return -1;
}
```

#### `GetDocument(int documentId)`
Pobiera dokument po jego ID.

- **Parametry:**
  - `documentId`: ID dokumentu do pobrania
- **Zwraca:** `DocumentInfo` - Informacje o dokumencie lub `null` w przypadku błędu

```csharp
public DocumentInfo GetDocument(int documentId, bool getDeactivated = false)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return null;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GetDocumentById", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DocumentID", documentId);
            if( getDeactivated ) cmd.Parameters.AddWithValue("@IncludeInactive", getDeactivated);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return CreateDocumentInfoFromReader(reader);
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentu: {ex.Message}"));
    }

    return null;
}
```

#### `GetDocumentByExhibitId(string exhibitId)`
Pobiera dokument po ID eksponatu.

- **Parametry:**
  - `exhibitId`: ID eksponatu do wyszukania
- **Zwraca:** `DocumentInfo` - Informacje o dokumencie lub `null` w przypadku błędu

```csharp
public DocumentInfo GetDocumentByExhibitId(string exhibitId)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return null;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GetDocumentByExhibitID", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return CreateDocumentInfoFromReader(reader);
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentu: {ex.Message}"));
    }

    return null;
}
```

#### `UpdateDocument(int documentId, string documentName, string xmlContent, string modifiedBy)`
Aktualizuje istniejący dokument.

- **Parametry:**
  - `documentId`: ID dokumentu do aktualizacji
  - `documentName`: Nowa nazwa dokumentu
  - `xmlContent`: Nowa zawartość XML
  - `modifiedBy`: Autor modyfikacji
- **Zwraca:** `bool` - `True` jeśli aktualizacja się powiodła

```csharp
public bool UpdateDocument(int documentId, string documentName, string xmlContent, string modifiedBy)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return false;
    }

    try
    {
        // Walidacja XML
        if (!ValidateDocument(xmlContent))
        {
            OnErrorOccurred(new ErrorEventArgs("Dokument XML nie jest poprawny"));
            return false;
        }

        using (var cmd = new SqlCommand("SP_UpdateDocument", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DocumentID", documentId);
            cmd.Parameters.AddWithValue("@DocumentName", documentName);
            cmd.Parameters.AddWithValue("@XMLContent", xmlContent);
            if (string.IsNullOrWhiteSpace(modifiedBy))
            {
                cmd.Parameters.AddWithValue("@ModifiedBy", DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            }
            cmd.ExecuteNonQuery();

            var doc = GetDocument(documentId);
            OnDocumentUpdated(new DocumentEventArgs(documentId, doc?.ExhibitId ?? "", documentName));
            return true;
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas aktualizacji dokumentu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas aktualizacji dokumentu: {ex.Message}"));
    }

    return false;
}
```

#### `DeactivateDocument(int documentId, string deactivatedBy, string reason = null)`
Dezaktywuje dokument (oznacza jako nieaktywny).

- **Parametry:**
  - `documentId`: ID dokumentu do dezaktywacji
  - `deactivatedBy`: Osoba dezaktywująca dokument
  - `reason`: Powód dezaktywacji (opcjonalny)
- **Zwraca:** `bool` - `True` jeśli dezaktywacja się powiodła

```csharp
public bool DeactivateDocument(int documentId, string deactivatedBy, string reason = null)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return false;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_DeactivateDocument", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            Console.WriteLine($"Executing SP with DocumentID: {documentId}, ModifiedBy: {deactivatedBy}");

            cmd.Parameters.AddWithValue("@DocumentID", documentId);
            cmd.Parameters.AddWithValue("@ModifiedBy", deactivatedBy);

            if (!string.IsNullOrEmpty(reason))
                cmd.Parameters.AddWithValue("@Reason", reason);

            int rowsAffected = cmd.ExecuteNonQuery();
            return true;
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas dezaktywacji dokumentu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"General Error: {ex.Message}");
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas dezaktywacji dokumentu: {ex.Message}"));
    }
    return false;
}
```

#### `DeleteDocument(int documentId)`
Usuwa dokument po ID.

- **Parametry:**
  - `documentId`: ID dokumentu do usunięcia
- **Zwraca:** `bool` - `True` jeśli usunięcie się powiodło

```csharp
public bool ValidateDocument(string xmlContent)
{
    _xmlValidator = new XMLValidator();
    bool result = _xmlValidator.ValidateExhibitXML(xmlContent);
    Console.WriteLine(_xmlValidator.GetValidationReport());
    return result;
}
```

#### `DeleteDocumentByExhibitId(string exhibitId)`
Usuwa dokumenty powiązane z eksponatem.

- **Parametry:**
  - `exhibitId`: ID eksponatu, którego dokumenty mają zostać usunięte
- **Zwraca:** `bool` - `True` jeśli operacja się powiodła

```csharp
public bool DeleteDocumentByExhibitId(string exhibitId)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return false;
    }

    try
    {
        int documentId = -1;
        using (var cmd = new SqlCommand("SP_DeleteDocumentsByExhibitID", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);

            cmd.ExecuteNonQuery();

            OnDocumentDeleted(new DocumentEventArgs(documentId, null, null));

            return true;
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas usuwania dokumentów eksponatu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"General Error: {ex.Message}");
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas usuwania dokumentów eksponatu: {ex.Message}"));
    }

    return false;
}
```

#### `ValidateDocument(string xmlContent)`
Sprawdza poprawność dokumentu XML.

- **Parametry:**
  - `xmlContent`: Zawartość XML do walidacji
- **Zwraca:** `bool` - `True` jeśli XML jest poprawny

```csharp
public bool ValidateDocument(string xmlContent)
{
    _xmlValidator = new XMLValidator();
    bool result = _xmlValidator.ValidateExhibitXML(xmlContent);
    Console.WriteLine(_xmlValidator.GetValidationReport());
    return result;
}
```

---

### Operacje wyszukiwania

#### `SearchByCategory(string category)`
Wyszukuje dokumenty po kategorii.

- **Parametry:**
  - `category`: Kategoria eksponatu do wyszukania
- **Zwraca:** `List<SearchResult>` - Lista pasujących dokumentów

```csharp
public List<SearchResult> SearchByCategory(string category)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return new List<SearchResult>();
    }

    var results = new List<SearchResult>();

    try
    {
        using (var cmd = new SqlCommand("SP_SearchByCategory", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Category", category);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(CreateSearchResultFromReader(reader));
                }
            }
        }

        // Dodaj do cache
        _searchCache[$"category_{category}"] = DateTime.Now;
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po kategorii: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po kategorii: {ex.Message}"));
    }

    return results;
}
```

#### `SearchByXPath(string xpathExpression)`
Wyszukuje dokumenty używając wyrażenia XPath.

- **Parametry:**
  - `xpathExpression`: Zapytanie XPath
- **Zwraca:** `List<SearchResult>` - Lista pasujących dokumentów

```csharp
public List<SearchResult> SearchByXPath(string xpathExpression)
 {
     if (!IsConnected())
     {
         OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
         return new List<SearchResult>();
     }

     var results = new List<SearchResult>();

     try
     {
         using (var cmd = new SqlCommand("SP_SearchByXPath", _connection))
         {
             cmd.CommandType = CommandType.StoredProcedure;
             cmd.Parameters.AddWithValue("@XPathExpression", xpathExpression);

             using (var reader = cmd.ExecuteReader())
             {
                 while (reader.Read())
                 {
                     results.Add(CreateSearchResultFromReader(reader));
                 }
             }
         }

         _searchCache[$"xpath_{xpathExpression.GetHashCode()}"] = DateTime.Now;
     }
     catch (SqlException sqlEx)
     {
         Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
         Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
         OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania XPath: {sqlEx.Message}"));
     }
     catch (Exception ex)
     {
         OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania XPath: {ex.Message}"));
     }

     return results;
 }
```

#### `SearchFullText(string searchText)`
Wykonuje wyszukiwanie pełnotekstowe.

- **Parametry:**
  - `searchText`: Tekst do wyszukania
- **Zwraca:** `List<SearchResult>` - Lista pasujących dokumentów

```csharp
public List<SearchResult> SearchFullText(string searchText)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return new List<SearchResult>();
    }

    var results = new List<SearchResult>();

    try
    {
        using (var cmd = new SqlCommand("SP_SearchFullText", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@SearchText", searchText);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(CreateSearchResultFromReader(reader));
                }
            }
        }

        _searchCache[$"fulltext_{searchText}"] = DateTime.Now;
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania pełnotekstowego: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania pełnotekstowego: {ex.Message}"));
    }

    return results;
}
```

#### `SearchByPeriod(string period)`
Wyszukuje dokumenty po okresie historycznym.

- **Parametry:**
  - `period`: Okres historyczny do wyszukania
- **Zwraca:** `List<SearchResult>` - Lista pasujących dokumentów

```csharp
public List<SearchResult> SearchByPeriod(string period)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return new List<SearchResult>();
    }

    var results = new List<SearchResult>();

    try
    {
        using (var cmd = new SqlCommand("SP_SearchByPeriod", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Period", period);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(CreateSearchResultFromReader(reader));
                }
            }
        }

        _searchCache[$"period_{period}"] = DateTime.Now;
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po okresie: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po okresie: {ex.Message}"));
    }

    return results;
}
```

#### `SearchByCondition(string condition)`
Wyszukuje dokumenty po stanie zachowania.

- **Parametry:**
  - `condition`: Stan zachowania do wyszukania
- **Zwraca:** `List<SearchResult>` - Lista pasujących dokumentów

```csharp
public List<SearchResult> SearchByCondition(string condition)
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return new List<SearchResult>();
    }

    var results = new List<SearchResult>();

    try
    {
        using (var cmd = new SqlCommand("SP_SearchByCondition", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Condition", condition);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(CreateSearchResultFromReader(reader));
                }
            }
        }

        _searchCache[$"condition_{condition}"] = DateTime.Now;
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po stanie: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po stanie: {ex.Message}"));
    }

    return results;
}
```

---

### Raporty

#### `GenerateArchiveReport()`
Generuje raport archiwum.

- **Zwraca:** `ArchiveReport` - Raport zawierający statystyki archiwum

```csharp
public ArchiveReport GenerateArchiveReport()
{
    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return null;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GenerateArchiveReport", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new ArchiveReport
                    {
                        TotalDocuments = reader.GetInt32(reader.GetOrdinal("TotalDocuments")),
                        ActiveDocuments = reader.GetInt32(reader.GetOrdinal("ActiveDocuments")),
                        InactiveDocuments = reader.GetInt32(reader.GetOrdinal("InactiveDocuments")),
                        DocumentsOnDisplay = reader.GetInt32(reader.GetOrdinal("DocumentsOnDisplay")),
                        DocumentsInStorage = reader.GetInt32(reader.GetOrdinal("DocumentsInStorage")),
                        DocumentsRequiringConservation = reader.GetInt32(reader.GetOrdinal("DocumentsRequiringConservation"))
                    };
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas generowania raportu: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas generowania raportu: {ex.Message}"));
    }

    return null;
}
```

#### `GetCategoryStatistics()`
Pobiera liczbę dokumentów w kategoriach.

- **Zwraca:** `Dictionary<string, int>` - Słownik z liczbą dokumentów w każdej kategorii

```csharp
public Dictionary<string, int> GetCategoryStatistics()
{
    var statistics = new Dictionary<string, int>();

    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return statistics;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GetCategoryStatistics", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    statistics[reader.GetString(reader.GetOrdinal("Category"))] = reader.GetInt32(reader.GetOrdinal("Count"));
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania statystyk: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania statystyk: {ex.Message}"));
    }

    return statistics;
}
```

#### `GetDocumentsRequiringConservation()`
Pobiera dokumenty wymagające konserwacji.

- **Zwraca:** `List<DocumentInfo>` - Lista dokumentów wymagających konserwacji

```csharp
 public List<DocumentInfo> GetDocumentsRequiringConservation()
 {
     var results = new List<DocumentInfo>();

     if (!IsConnected())
     {
         OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
         return results;
     }

     try
     {
         using (var cmd = new SqlCommand("SP_GetDocumentByIdsRequiringConservation", _connection))
         {
             cmd.CommandType = CommandType.StoredProcedure;

             using (var reader = cmd.ExecuteReader())
             {
                 while (reader.Read())
                 {
                     results.Add(CreateDocumentInfoFromReader(reader));
                 }
             }
         }
     }
     catch (SqlException sqlEx)
     {
         Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
         Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
         OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentów po konserwacji: {sqlEx.Message}"));
     }
     catch (Exception ex)
     {
         OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentów do konserwacji: {ex.Message}"));
     }

     return results;
 }
```

#### `GetRecentDocuments(int days = 30)`
Pobiera ostatnio dodane dokumenty.

- **Parametry:**
  - `days`: Liczba dni wstecz (domyślnie `30`)
- **Zwraca:** `List<DocumentInfo>` - Lista ostatnio dodanych dokumentów

```csharp
public List<DocumentInfo> GetRecentDocuments(int days = 30)
{
    var results = new List<DocumentInfo>();

    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return results;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GetRecentDocuments", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Days", days);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(CreateDocumentInfoFromReader(reader));
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania ostatnich dokumentów: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania ostatnich dokumentów: {ex.Message}"));
    }

    return results;
}
```

---

### Metody pomocnicze

#### `CleanExpiredSearchCache(int maxAge = 60)`
Czyści wygasłe wpisy z cache wyszukiwania.

- **Parametry:**
  - `maxAge`: Maksymalny wiek wpisu w minutach (domyślnie `60`)

```csharp
public void CleanExpiredSearchCache(int maxAge = 60)
{
    try
    {
        var expiredKeys = _searchCache
            .Where(kvp => DateTime.Now.Subtract(kvp.Value).TotalMinutes > maxAge)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _searchCache.Remove(key);
        }
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas czyszczenia cache: {ex.Message}"));
    }
}
```

#### `GetDocumentOperationLog(int documentId)`
Pobiera log operacji na dokumencie.

- **Parametry:**
  - `documentId`: ID dokumentu
- **Zwraca:** `List<OperationLog>` - Lista operacji wykonanych na dokumencie

```csharp
public List<OperationLog> GetDocumentOperationLog(int documentId)
{
    var operations = new List<OperationLog>();

    if (!IsConnected())
    {
        OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
        return operations;
    }

    try
    {
        using (var cmd = new SqlCommand("SP_GetDocumentByIdOperationLog", _connection))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DocumentID", documentId);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    operations.Add(new OperationLog
                    {
                        LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                        DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                        OperationType = reader.GetString(reader.GetOrdinal("OperationType")),
                        OperationDescription = reader.GetString(reader.GetOrdinal("OperationDescription")),
                        ExecutedBy = reader.GetString(reader.GetOrdinal("ExecutedBy")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage"))
                    });
                }
            }
        }
    }
    catch (SqlException sqlEx)
    {
        Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
        Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
        OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania logu operacji: {sqlEx.Message}"));
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania logu operacji: {ex.Message}"));
    }

    return operations;
}
```

#### `ExportDocumentToFile(int documentId, string filePath)`
Eksportuje dokument do pliku.

- **Parametry:**
  - `documentId`: ID dokumentu do eksportu
  - `filePath`: Ścieżka do pliku
- **Zwraca:** `bool` - `True` jeśli eksport się powiódł

```csharp
public bool ExportDocumentToFile(int documentId, string filePath)
{
    try
    {
        var document = GetDocument(documentId);
        if (document == null)
        {
            OnErrorOccurred(new ErrorEventArgs("Nie znaleziono dokumentu"));
            return false;
        }

        File.WriteAllText(filePath, document.ToFormattedXmlString(), Encoding.UTF8);
        return true;
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas eksportu dokumentu: {ex.Message}"));
        return false;
    }
}
```

#### `ImportDocumentFromFile(string filePath, string exhibitId, string documentName, string createdBy)`
Importuje dokument z pliku.

- **Parametry:**
  - `filePath`: Ścieżka do pliku
  - `exhibitId`: ID eksponatu
  - `documentName`: Nazwa dokumentu
  - `createdBy`: Autor importu
- **Zwraca:** `int` - ID utworzonego dokumentu lub `-1` w przypadku błędu

```csharp
public int ImportDocumentFromFile(string filePath, string exhibitId, string documentName, string createdBy)
{
    try
    {
        if (!File.Exists(filePath))
        {
            OnErrorOccurred(new ErrorEventArgs("Plik nie istnieje"));
            return -1;
        }

        string xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
        return AddDocument(exhibitId, documentName, xmlContent, createdBy);
    }
    catch (Exception ex)
    {
        OnErrorOccurred(new ErrorEventArgs($"Błąd podczas importu dokumentu: {ex.Message}"));
        return -1;
    }
}
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

## Wymagania

- .NET Framework 4.5+
- System.Data.SqlClient
- System.Xml, System.Xml.Linq

## Bezpieczeństwo wątków

Implementacja nie jest thread-safe – wymagane osobne instancje lub synchronizacja.

## Literatura
* Elliotte Rusty Harold, W. Scott Means, „XML in a Nutshell”, O'Reilly, 2004.
* David Hunter et al., „Beginning XML”, Wrox Press, 2007.
* Anne Gentle, „Docs Like Code”, Leanpub, 2017.
* Thomas M. Connolly, Carolyn E. Begg, „Systemy baz danych”, Warszawa, RM 2004.
* Ramez Elmasri, Shamkant B. Navathe, Wprowadzenie do systemów baz danych. Wydanie VII, Helion 2019
* Adam Pelikant, MS SQL Server. Zaawansowane metody programowania, Helion 2014
