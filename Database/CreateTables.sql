-- =====================================================
-- Muzealne Archiwum Dokumentów XML - Tworzenie Tabel
-- Autor: Bartosz Fryska
-- Data: 13 maja 2025
-- =====================================================

USE MuseumXMLArchive;
GO

-- Tabela g³ówna przechowuj¹ca dokumenty XML
CREATE TABLE Documents (
    DocumentID INT IDENTITY(1,1) PRIMARY KEY,
    ExhibitID NVARCHAR(50) NOT NULL UNIQUE, -- ID eksponatu z dokumentu XML
    DocumentName NVARCHAR(255) NOT NULL,
    XMLContent XML(ExhibitSchemaCollection) NOT NULL, -- Natywny typ XML z walidacj¹
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ModifiedDate DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100) DEFAULT SYSTEM_USER,
    ModifiedBy NVARCHAR(100) DEFAULT SYSTEM_USER,
    IsActive BIT DEFAULT 1,
    ValidationStatus NVARCHAR(20) DEFAULT 'Valid' CHECK (ValidationStatus IN ('Valid', 'Invalid', 'Pending')),
    Notes NVARCHAR(MAX) NULL
);
GO

-- Tabela dla logowania operacji
CREATE TABLE DocumentOperationLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    DocumentID INT NULL,
    OperationType NVARCHAR(50) NOT NULL, -- INSERT, UPDATE, DELETE, SEARCH
    OperationDescription NVARCHAR(500),
    ExecutedBy NVARCHAR(100) DEFAULT SYSTEM_USER,
    ExecutedDate DATETIME2 DEFAULT GETDATE(),
    Success BIT DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL
);
GO

-- Tabela dla przechowywania wyników wyszukiwañ (cache)
CREATE TABLE SearchCache (
    SearchID INT IDENTITY(1,1) PRIMARY KEY,
    SearchQuery NVARCHAR(MAX) NOT NULL,
    SearchType NVARCHAR(50) NOT NULL, -- XPath, XQuery, FullText
    ResultXML XML NULL,
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ExpirationDate DATETIME2 DEFAULT DATEADD(HOUR, 24, GETDATE())
);
GO

-- INDEKSY DLA WYDAJNOŒCI
-- Indeks XML dla szybszego wyszukiwania
CREATE PRIMARY XML INDEX PXML_Documents_XMLContent 
ON Documents (XMLContent);
GO

CREATE XML INDEX SXML_Documents_XMLContent_PATH 
ON Documents (XMLContent)
USING XML INDEX PXML_Documents_XMLContent
FOR PATH;
GO

CREATE XML INDEX SXML_Documents_XMLContent_PROPERTY 
ON Documents (XMLContent)
USING XML INDEX PXML_Documents_XMLContent
FOR PROPERTY;
GO

-- Indeksy tradycyjne
CREATE NONCLUSTERED INDEX IX_Documents_ExhibitID 
ON Documents (ExhibitID);
GO

CREATE NONCLUSTERED INDEX IX_Documents_CreatedDate 
ON Documents (CreatedDate DESC);
GO

CREATE NONCLUSTERED INDEX IX_Documents_ModifiedDate 
ON Documents (ModifiedDate DESC);
GO

CREATE NONCLUSTERED INDEX IX_DocumentOperationLog_DocumentID 
ON DocumentOperationLog (DocumentID);
GO

-- TRIGGERY
-- Trigger do automatycznej aktualizacji ModifiedDate
CREATE TRIGGER TR_Documents_UpdateModifiedDate
ON Documents
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Documents 
    SET ModifiedDate = GETDATE(),
        ModifiedBy = SYSTEM_USER
    FROM Documents d
    INNER JOIN inserted i ON d.DocumentID = i.DocumentID;
END;
GO

-- Trigger do logowania operacji
CREATE TRIGGER TR_Documents_LogOperations
ON Documents
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OperationType NVARCHAR(50);
    
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @OperationType = 'UPDATE';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @OperationType = 'INSERT';
    ELSE
        SET @OperationType = 'DELETE';
    
    INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription)
    SELECT 
        COALESCE(i.DocumentID, d.DocumentID),
        @OperationType,
        CASE 
            WHEN @OperationType = 'INSERT' THEN 'Document added: ' + i.DocumentName
            WHEN @OperationType = 'UPDATE' THEN 'Document updated: ' + i.DocumentName
            WHEN @OperationType = 'DELETE' THEN 'Document deleted: ' + d.DocumentName
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.DocumentID = d.DocumentID;
END;
GO

-- WIDOKI
-- Widok z podstawowymi informacjami o eksponatach
CREATE VIEW VW_ExhibitSummary AS
WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns)
SELECT 
    d.DocumentID,
    d.ExhibitID,
    d.DocumentName,
    d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)') as Title,
    d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]', 'NVARCHAR(100)') as Category,
    d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Creator)[1]', 'NVARCHAR(255)') as Creator,
    d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Period)[1]', 'NVARCHAR(100)') as Period,
    d.XMLContent.value('(/ns:Exhibit/@status)[1]', 'NVARCHAR(50)') as Status,
    d.XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') as OnDisplay,
    d.CreatedDate,
    d.ModifiedDate,
    d.ValidationStatus
FROM Documents d
WHERE d.IsActive = 1;
GO

PRINT 'Tabele, indeksy, triggery i widoki zosta³y utworzone pomyœlnie!';
GO