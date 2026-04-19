
-- =====================================================
-- Muzealne Archiwum Dokumentów XML - Skrypt Instalacji
-- Autor: Bartosz Fryska
-- Data: 13 maja 2025
-- 
-- Ten skrypt instaluje kompletną bazę danych MuseumXMLArchive
-- Uruchom ten plik aby utworzyć całą strukturę bazy danych
-- =====================================================

PRINT '=== MUZEUM XML ARCHIVE - INSTALACJA BAZY DANYCH ===';
PRINT 'Rozpoczęto instalację: ' + CONVERT(NVARCHAR(20), GETDATE(), 120);
PRINT '';

-- 1. TWORZENIE BAZY DANYCH I SCHEMATU XML
PRINT '1. Tworzenie bazy danych i schematu XML...';
GO
:r CreateDatabase.sql
PRINT '    Baza danych i schemat XML utworzone';
PRINT '';

-- 2. TWORZENIE TABEL, INDEKSÓW I TRIGGERÓW
PRINT '2. Tworzenie tabel, indeksów i triggerów...';
GO
:r CreateTables.sql
PRINT '    Tabele, indeksy i triggery utworzone';
PRINT '';

-- 3. TWORZENIE PROCEDUR SKŁADOWANYCH
PRINT '3. Tworzenie procedur składowanych...';
GO
:r StoredProcedures.sql
PRINT '    Procedury składowane utworzone';
PRINT '';

-- 4. WSTAWIANIE DANYCH TESTOWYCH
PRINT '4. Wstawianie danych testowych...';
GO
:r SampleData.sql
PRINT '    Dane testowe wstawione';
PRINT '';

-- 5. SPRAWDZENIE INSTALACJI
PRINT '5. Sprawdzenie poprawności instalacji...';
USE MuseumXMLArchive;
GO

DECLARE @DatabaseExists BIT = 0;
DECLARE @TablesCount INT = 0;
DECLARE @ProceduresCount INT = 0;
DECLARE @DocumentsCount INT = 0;

-- Sprawdzenie bazy danych
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'MuseumXMLArchive')
    SET @DatabaseExists = 1;

-- Sprawdzenie tabel
SELECT @TablesCount = COUNT(*) 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

-- Sprawdzenie procedur
SELECT @ProceduresCount = COUNT(*) 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE';

-- Sprawdzenie dokumentów
SELECT @DocumentsCount = COUNT(*) FROM Documents;

PRINT '=== RAPORT INSTALACJI ===';
PRINT 'Baza danych istnieje: ' + CASE WHEN @DatabaseExists = 1 THEN 'TAK' ELSE 'NIE' END;
PRINT 'Liczba tabel: ' + CAST(@TablesCount AS NVARCHAR(10));
PRINT 'Liczba procedur: ' + CAST(@ProceduresCount AS NVARCHAR(10));
PRINT 'Liczba dokumentów testowych: ' + CAST(@DocumentsCount AS NVARCHAR(10));
PRINT '';

IF @DatabaseExists = 1 AND @TablesCount >= 3 AND @ProceduresCount >= 5 AND @DocumentsCount >= 5
BEGIN
    PRINT ' INSTALACJA ZAKOŃCZONA POMYŚLNIE!';
    PRINT '';
    PRINT 'Baza danych MuseumXMLArchive jest gotowa do użycia.';
    PRINT 'Możesz teraz:';
    PRINT '- Przeglądać eksponaty: SELECT * FROM VW_ExhibitSummary';
    PRINT '- Wyszukiwać po kategorii: EXEC SP_SearchByCategory @Category = ''Pottery''';
    PRINT '- Dodawać nowe dokumenty: EXEC SP_InsertDocument ...';
    PRINT '- Sprawdzać logi operacji: SELECT * FROM DocumentOperationLog';
END
ELSE
BEGIN
    PRINT ' INSTALACJA NIE POWIODŁA SIĘ!';
    PRINT 'Sprawdź komunikaty błędów powyżej i spróbuj ponownie.';
END

PRINT '';
PRINT 'Instalacja zakończona: ' + CONVERT(NVARCHAR(20), GETDATE(), 120);
PRINT '=== KONIEC INSTALACJI ===';
GO