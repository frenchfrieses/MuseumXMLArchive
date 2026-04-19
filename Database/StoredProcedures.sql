-- =====================================================
-- Muzealne Archiwum Dokumentów XML - Procedury Sk³adowane
-- Autor: Bartosz Fryska
-- Data: 13 maja 2025
-- =====================================================

USE MuseumXMLArchive;
GO

-- Procedura do czyszczenia wygas³ego cache wyszukiwañ
CREATE PROCEDURE SP_CleanExpiredSearchCache
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DELETE FROM SearchCache 
        WHERE ExpirationDate < GETDATE();
        
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('MAINTENANCE', 'Expired search cache cleaned. Rows affected: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('MAINTENANCE', 'Failed to clean expired search cache', 0, ERROR_MESSAGE());
    END CATCH
END;
GO

-- Procedura do walidacji dokumentu XML
CREATE PROCEDURE SP_ValidateDocument
    @DocumentID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ValidationResult NVARCHAR(20) = 'Valid';
    DECLARE @ErrorMsg NVARCHAR(MAX) = NULL;
    
    BEGIN TRY
        -- Próba odczytania i walidacji dokumentu
        DECLARE @TestXML XML(ExhibitSchemaCollection);
        
        SELECT @TestXML = XMLContent 
        FROM Documents 
        WHERE DocumentID = @DocumentID;
        
        UPDATE Documents 
        SET ValidationStatus = @ValidationResult,
            ModifiedDate = GETDATE()
        WHERE DocumentID = @DocumentID;
        
    END TRY
    BEGIN CATCH
        SET @ValidationResult = 'Invalid';
        SET @ErrorMsg = ERROR_MESSAGE();
        
        UPDATE Documents 
        SET ValidationStatus = @ValidationResult,
            Notes = COALESCE(Notes + '; ', '') + 'Validation Error: ' + @ErrorMsg,
            ModifiedDate = GETDATE()
        WHERE DocumentID = @DocumentID;
    END CATCH
    
    INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
    VALUES (@DocumentID, 'VALIDATE', 'Document validation completed', 
            CASE WHEN @ValidationResult = 'Valid' THEN 1 ELSE 0 END, @ErrorMsg);
END;
GO

-- Procedura do pobierania dokumentu po ID
CREATE PROCEDURE SP_GetDocumentById
    @DocumentID INT,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        IF NOT EXISTS (
            SELECT 1 
            FROM Documents 
            WHERE DocumentID = @DocumentID 
            AND (@IncludeInactive = 1 OR IsActive = 1)
        )
        BEGIN
            INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
            VALUES (@DocumentID, 'SEARCH', 'Document retrieval failed - document not found', 0, 'Document not found or inactive');
            
            RAISERROR('Dokument o ID %d nie zosta³ znaleziony lub jest nieaktywny.', 16, 1, @DocumentID);
            RETURN;
        END
        
        SELECT 
            DocumentID,
            ExhibitID,
            DocumentName,
            CAST(XMLContent AS NVARCHAR(MAX)) AS XMLContent,
            CreatedDate,
            CreatedBy,
            ModifiedDate,
            ModifiedBy,
            IsActive
        FROM Documents
        WHERE DocumentID = @DocumentID
        AND (@IncludeInactive = 1 OR IsActive = 1);
        
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription)
        VALUES (@DocumentID, 'SEARCH', 'Document retrieved successfully');
        
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (@DocumentID, 'SEARCH', 'Document retrieval failed', 0, ERROR_MESSAGE());
        
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas pobierania dokumentu: %s', 16, 1, @ErrMsg);
        RETURN;
    END CATCH
END;
GO

-- Procedura do pobierania dokumentu po ID wystawy
CREATE PROCEDURE SP_GetDocumentByExhibitId
     @ExhibitID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DocumentID INT;

     BEGIN TRY
        SELECT TOP 1 @DocumentID = DocumentID
        FROM Documents
        WHERE ExhibitID = @ExhibitID AND IsActive = 1;

        IF @DocumentID IS NULL
        BEGIN
            INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
            VALUES (NULL, 'SEARCH', 'Document retrieval failed - document not found', 0, 'Document not found or inactive');
            
            RAISERROR('Dokument powi¹zany z ExhibitID = %s nie zosta³ znaleziony lub jest nieaktywny.', 16, 1, @ExhibitID);
            RETURN;
        END

        SELECT 
            DocumentID,
            ExhibitID,
            DocumentName,
            CAST(XMLContent AS NVARCHAR(MAX)) AS XMLContent,
            CreatedDate,
            CreatedBy,
            ModifiedDate,
            ModifiedBy,
            IsActive
        FROM Documents
        WHERE DocumentID = @DocumentID;

        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success)
        VALUES (@DocumentID, 'SEARCH', 'Document retrieved successfully', 1);
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (@DocumentID, 'SEARCH', 'Document retrieval failed', 0, ERROR_MESSAGE());
        
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas pobierania dokumentu: %s', 16, 1, @ErrMsg);
        RETURN;
    END CATCH
END;
GO

-- Procedura do wstawiania nowego dokumentu z walidacj¹
CREATE PROCEDURE SP_InsertDocument
    @ExhibitID NVARCHAR(50),
    @DocumentName NVARCHAR(255),
    @XMLContent XML,
    @CreatedBy NVARCHAR(100) = NULL,
    @DocumentID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorMsg NVARCHAR(MAX) = NULL;
    DECLARE @Success BIT = 1;
    
    -- Ustawienie domyœlnego u¿ytkownika
    IF @CreatedBy IS NULL
        SET @CreatedBy = SYSTEM_USER;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Sprawdzenie czy ExhibitID ju¿ istnieje
        IF EXISTS (SELECT 1 FROM Documents WHERE ExhibitID = @ExhibitID AND IsActive = 1)
        BEGIN
            RAISERROR('Dokument z ExhibitID %s ju¿ istnieje', 16, 1, @ExhibitID);
            RETURN;
        END
        
        -- Wstawienie dokumentu
        INSERT INTO Documents (ExhibitID, DocumentName, XMLContent, CreatedBy, ModifiedBy)
        VALUES (@ExhibitID, @DocumentName, @XMLContent, @CreatedBy, NULL);
        
        SET @DocumentID = SCOPE_IDENTITY();
        
        -- Walidacja dokumentu
        EXEC SP_ValidateDocument @DocumentID;
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @Success = 0;
        SET @ErrorMsg = ERROR_MESSAGE();
        SET @DocumentID = -1;
        
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (NULL, 'INSERT', 'Failed to insert document: ' + @DocumentName, 0, @ErrorMsg);
        
        RAISERROR('B³¹d podczas wstawiania dokumentu: %s', 16, 1, @ErrorMsg);
    END CATCH
END;
GO

-- Procedura do aktualizacji dokumentu
CREATE PROCEDURE SP_UpdateDocument
    @DocumentID INT,
    @DocumentName NVARCHAR(255) = NULL,
    @XMLContent XML,
    @ModifiedBy NVARCHAR(100) = NULL,
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorMsg NVARCHAR(MAX) = NULL;
    DECLARE @OldDocumentName NVARCHAR(255);
    
    -- Ustawienie domyœlnego u¿ytkownika
    IF @ModifiedBy IS NULL
        SET @ModifiedBy = SYSTEM_USER;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Sprawdzenie czy dokument istnieje i jest aktywny
        SELECT @OldDocumentName = DocumentName 
        FROM Documents 
        WHERE DocumentID = @DocumentID AND IsActive = 1;
        
        IF @OldDocumentName IS NULL
        BEGIN
            RAISERROR('Dokument o ID %d nie istnieje lub jest nieaktywny', 16, 1, @DocumentID);
            RETURN;
        END
        
        -- Aktualizacja dokumentu 
        UPDATE Documents 
        SET DocumentName = COALESCE(@DocumentName, DocumentName),
            XMLContent = @XMLContent,
            ModifiedBy = COALESCE(@ModifiedBy, SYSTEM_USER),
            ModifiedDate = GETDATE(),
            Notes = COALESCE(@Notes, Notes)
        WHERE DocumentID = @DocumentID;
        
        -- Walidacja dokumentu po aktualizacji XML
        EXEC SP_ValidateDocument @DocumentID;
        
        -- Sprawdzenie statusu walidacji
        DECLARE @ValidationStatus NVARCHAR(20);
        SELECT @ValidationStatus = ValidationStatus FROM Documents WHERE DocumentID = @DocumentID;
        
        IF @ValidationStatus <> 'Valid'
        BEGIN
            RAISERROR('Dokument XML nie przeszed³ walidacji', 16, 1);
            RETURN;
        END
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @ErrorMsg = ERROR_MESSAGE();
        
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (@DocumentID, 'UPDATE', 'Nieudana aktualizacja dokumentu', 0, @ErrorMsg);
        
        RAISERROR('B³¹d podczas aktualizacji dokumentu: %s', 16, 1, @ErrorMsg);
    END CATCH
END;
GO

-- Procedura do "miêkkiego" usuwania dokumentu
CREATE PROCEDURE SP_DeactivateDocument
    @DocumentID INT,
    @ModifiedBy NVARCHAR(100) = NULL,
    @Reason NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorMsg NVARCHAR(MAX) = NULL;
    DECLARE @DocumentName NVARCHAR(255);
    
    -- Ustawienie domyœlnego u¿ytkownika
    IF @ModifiedBy IS NULL
        SET @ModifiedBy = SYSTEM_USER;
    
    BEGIN TRY
        -- Sprawdzenie czy dokument istnieje
        SELECT @DocumentName = DocumentName 
        FROM Documents 
        WHERE DocumentID = @DocumentID;
        
        IF @DocumentName IS NULL
        BEGIN
            RAISERROR('Dokument o ID %d nie istnieje lub jest ju¿ nieaktywny', 16, 1, @DocumentID);
            RETURN;
        END
        
        -- "Miêkkie" usuniêcie
        UPDATE Documents 
        SET IsActive = 0,
            ModifiedBy = @ModifiedBy,
            ModifiedDate = GETDATE(),
            Notes = COALESCE(Notes + '; ', '') + 
                   'Deactivated on ' + CONVERT(NVARCHAR(20), GETDATE(), 120) + 
                   CASE WHEN @Reason IS NOT NULL THEN '. Reason: ' + @Reason ELSE '' END
        WHERE DocumentID = @DocumentID;
        
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription)
        VALUES (@DocumentID, 'DEACTIVATE', 'Document deactivated: ' + @DocumentName + 
                CASE WHEN @Reason IS NOT NULL THEN '. Reason: ' + @Reason ELSE '' END);
        RETURN @@ROWCOUNT;
        
    END TRY
    BEGIN CATCH
        SET @ErrorMsg = ERROR_MESSAGE();
        
        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (@DocumentID, 'DEACTIVATE', 'Failed to deactivate document', 0, @ErrorMsg);
        
        RAISERROR('B³¹d podczas deaktywacji dokumentu: %s', 16, 1, @ErrorMsg);
    END CATCH
END;
GO

-- =======================================================
-- PROCEDURY WYSZUKIWANIA
-- =======================================================

-- Procedura do wyszukiwania dokumentów po kategorii
CREATE PROCEDURE SP_SearchByCategory
    @Category NVARCHAR(100),
    @OnDisplayOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchQuery NVARCHAR(MAX) = 'Category: ' + @Category + 
                                        CASE WHEN @OnDisplayOnly = 1 THEN ' (On Display Only)' ELSE '' END;
    
    -- Zapisanie zapytania w cache
    DECLARE @ResultXML XML;
    
    WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns)
    SELECT @ResultXML = (
        SELECT 
            DocumentID,
            ExhibitID,
            DocumentName,
            XMLContent
         FROM Documents d
         WHERE d.IsActive = 1
           AND d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]', 'NVARCHAR(100)') = @Category
           AND (@OnDisplayOnly = 0 OR d.XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') = 1)
         FOR XML PATH('Document'), ROOT('SearchResults')
    );
    
    INSERT INTO SearchCache (SearchQuery, SearchType, ResultXML)
    VALUES (@SearchQuery, 'Category Search', @ResultXML);
    
    -- Zwrócenie wyników
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
        WHERE d.IsActive = 1
      AND d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]', 'NVARCHAR(100)') = @Category
      AND (@OnDisplayOnly = 0 OR d.XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') = 1)
    ORDER BY d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)');
    
    -- Logowanie wyszukiwania
    INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
    VALUES ('SEARCH', 'Category search executed: ' + @SearchQuery + '. Results: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
END;
GO

-- Procedura do wyszukiwania pe³notekstowego
CREATE PROCEDURE SP_SearchFullText
    @SearchText NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchQuery NVARCHAR(MAX) = 'FullText: ' + @SearchText;
    
    BEGIN TRY
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
        WHERE d.IsActive = 1
          AND (
              d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)') LIKE '%' + @SearchText + '%'
              OR d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Creator)[1]', 'NVARCHAR(255)') LIKE '%' + @SearchText + '%'
              OR d.XMLContent.value('(/ns:Exhibit/ns:Description/ns:ShortDescription)[1]', 'NVARCHAR(MAX)') LIKE '%' + @SearchText + '%'
              OR d.XMLContent.value('(/ns:Exhibit/ns:Description/ns:DetailedDescription)[1]', 'NVARCHAR(MAX)') LIKE '%' + @SearchText + '%'
              OR d.DocumentName LIKE '%' + @SearchText + '%'
          )
        ORDER BY d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)');
        
        -- Logowanie wyszukiwania
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('SEARCH', 'Full text search executed: ' + @SearchQuery + '. Results: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
        
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('SEARCH', 'Full text search failed: ' + @SearchQuery, 0, ERROR_MESSAGE());

		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();        
        RAISERROR('B³¹d podczas wyszukiwania pe³notekstowego: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do wyszukiwania po okresie
CREATE PROCEDURE SP_SearchByPeriod
    @Period NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchQuery NVARCHAR(MAX) = 'Period: ' + @Period;
    
    BEGIN TRY
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
        WHERE d.IsActive = 1
          AND d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Period)[1]', 'NVARCHAR(100)') = @Period
        ORDER BY d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)');
        
        -- Logowanie wyszukiwania
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('SEARCH', 'Period search executed: ' + @SearchQuery + '. Results: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
        
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('SEARCH', 'Period search failed: ' + @SearchQuery, 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();   
        RAISERROR('B³¹d podczas wyszukiwania po okresie: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do wyszukiwania XPath
CREATE PROCEDURE SP_SearchByXPath
    @XPathExpression NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchQuery NVARCHAR(MAX) = 'XPath: ' + @XPathExpression;
    DECLARE @SQL NVARCHAR(MAX);
    
    BEGIN TRY
        -- Dynamiczne tworzenie zapytania SQL z XPath
        SET @SQL = N'
        WITH XMLNAMESPACES(''http://museum.example.com/exhibit'' as ns)
        SELECT 
            d.DocumentID,
            d.ExhibitID,
            d.DocumentName,
            d.XMLContent.value(''(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]'', ''NVARCHAR(255)'') as Title,
            d.XMLContent.value(''(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]'', ''NVARCHAR(100)'') as Category,
            d.XMLContent.value(''(/ns:Exhibit/ns:BasicInfo/ns:Creator)[1]'', ''NVARCHAR(255)'') as Creator,
            d.XMLContent.value(''(/ns:Exhibit/ns:BasicInfo/ns:Period)[1]'', ''NVARCHAR(100)'') as Period,
            d.XMLContent.value(''(/ns:Exhibit/@status)[1]'', ''NVARCHAR(50)'') as Status,
            d.XMLContent.value(''(/ns:Exhibit/ns:Location/@onDisplay)[1]'', ''BIT'') as OnDisplay,
            d.CreatedDate,
            d.ModifiedDate,
            d.ValidationStatus
        FROM Documents d
        WHERE d.IsActive = 1
          AND d.XMLContent.exist(''' + @XPathExpression + ''') = 1
        ORDER BY d.XMLContent.value(''(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]'', ''NVARCHAR(255)'')';
        
        EXEC sp_executesql @SQL;
        
        -- Logowanie wyszukiwania
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('SEARCH', 'XPath search executed: ' + @SearchQuery);
        
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('SEARCH', 'XPath search failed: ' + @SearchQuery, 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas wyszukiwania XPath: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do wyszukiwania po stanie zachowania
CREATE PROCEDURE SP_SearchByCondition
    @Condition NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchQuery NVARCHAR(MAX) = 'Condition: ' + @Condition;
    
    BEGIN TRY
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
        WHERE d.IsActive = 1
          AND d.XMLContent.value('(/ns:Exhibit/ns:Technical/ns:Condition)[1]', 'NVARCHAR(50)') = @Condition
        ORDER BY d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)');
        
        -- Logowanie wyszukiwania
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('SEARCH', 'Condition search executed: ' + @SearchQuery + '. Results: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
        
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('SEARCH', 'Condition search failed: ' + @SearchQuery, 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas wyszukiwania po stanie: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- =======================================================
-- PROCEDURY RAPORTOWANIA I STATYSTYK
-- =======================================================

-- Procedura do generowania raportu archiwum
CREATE PROCEDURE SP_GenerateArchiveReport
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns),
        Stats AS (
            SELECT 
                COUNT(*) as TotalDocuments,
                SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) as ActiveDocuments,
                SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) as InactiveDocuments,
                SUM(CASE WHEN IsActive = 1 AND XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') = 1 THEN 1 ELSE 0 END) as DocumentsOnDisplay,
                SUM(CASE WHEN IsActive = 1 AND XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') = 0 THEN 1 ELSE 0 END) as DocumentsInStorage,
                SUM(CASE WHEN IsActive = 1 AND XMLContent.value('(/ns:Exhibit/ns:Technical/ns:Condition)[1]', 'NVARCHAR(50)') IN ('Poor', 'Critical') THEN 1 ELSE 0 END) as DocumentsRequiringConservation
            FROM Documents
        )
        SELECT 
            TotalDocuments,
            ActiveDocuments,
            InactiveDocuments,
            DocumentsOnDisplay,
            DocumentsInStorage,
            DocumentsRequiringConservation,
            GETDATE() as ReportGeneratedDate
        FROM Stats;
        
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('REPORT', 'Archive report generated successfully');
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('REPORT', 'Failed to generate archive report', 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas generowania raportu archiwum: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do pobierania statystyk kategorii
CREATE PROCEDURE SP_GetCategoryStatistics
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns),
        CategoryExtract AS (
            SELECT 
                d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]', 'NVARCHAR(100)') as Category
            FROM Documents d
            WHERE d.IsActive = 1
        )
        SELECT 
            Category,
            COUNT(*) as Count
        FROM CategoryExtract
        GROUP BY Category
        ORDER BY COUNT(*) DESC;
        
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('STATISTICS', 'Category statistics generated successfully');
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('STATISTICS', 'Failed to generate category statistics', 0, ERROR_MESSAGE());
        
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas generowania statystyk kategorii: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do pobierania dokumentów wymagaj¹cych konserwacji
CREATE PROCEDURE SP_GetDocumentsRequiringConservation
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns)
        SELECT 
            d.DocumentID,
            d.ExhibitID,
            d.DocumentName,
            d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)') as Title,
            d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Category)[1]', 'NVARCHAR(100)') as Category,
            d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Creator)[1]', 'NVARCHAR(255)') as Creator,
            d.XMLContent.value('(/ns:Exhibit/ns:Technical/ns:Condition)[1]', 'NVARCHAR(50)') as Condition,
            d.XMLContent.value('(/ns:Exhibit/ns:Technical/ns:ConservationNotes)[1]', 'NVARCHAR(MAX)') as ConservationNotes,
            d.XMLContent.value('(/ns:Exhibit/@status)[1]', 'NVARCHAR(50)') as Status,
            d.CreatedDate,
            d.ModifiedDate,
            d.ValidationStatus
        FROM Documents d
        WHERE d.IsActive = 1
          AND d.XMLContent.value('(/ns:Exhibit/ns:Technical/ns:Condition)[1]', 'NVARCHAR(50)') IN ('Poor', 'Critical')
        ORDER BY 
            CASE d.XMLContent.value('(/ns:Exhibit/ns:Technical/ns:Condition)[1]', 'NVARCHAR(50)')
                WHEN 'Critical' THEN 1
                WHEN 'Poor' THEN 2
                ELSE 3
            END,
            d.XMLContent.value('(/ns:Exhibit/ns:BasicInfo/ns:Title)[1]', 'NVARCHAR(255)');
        
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('QUERY', 'Documents requiring conservation retrieved. Count: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('QUERY', 'Failed to retrieve documents requiring conservation', 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas pobierania dokumentów wymagaj¹cych konserwacji: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do pobierania ostatnio dodanych dokumentów
CREATE PROCEDURE SP_GetRecentDocuments
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns)
        SELECT 
            DocumentID,
            ExhibitID,
            DocumentName,
            CAST(XMLContent AS NVARCHAR(MAX)) AS XMLContent,
            CreatedDate,
            CreatedBy,
            ModifiedDate,
            ModifiedBy,
            IsActive
        FROM Documents d
        WHERE d.IsActive = 1
          AND d.CreatedDate >= DATEADD(DAY, -@Days, GETDATE())
        ORDER BY d.CreatedDate DESC;
        
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription)
        VALUES ('QUERY', 'Recent documents retrieved for last ' + CAST(@Days AS NVARCHAR(10)) + ' days. Count: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)));
    END TRY
    BEGIN CATCH
        INSERT INTO DocumentOperationLog (OperationType, OperationDescription, Success, ErrorMessage)
        VALUES ('QUERY', 'Failed to retrieve recent documents', 0, ERROR_MESSAGE());
        
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas pobierania ostatnich dokumentów: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do pobierania logów operacji na dokumencie
CREATE PROCEDURE SP_GetDocumentOperationLog
    @DocumentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT 
            LogID,
            DocumentID,
            OperationType,
            OperationDescription,
            ExecutedBy,
            ExecutedDate,
            Success,
            ErrorMessage
        FROM DocumentOperationLog
        WHERE (@DocumentID IS NULL OR DocumentID = @DocumentID)
        ORDER BY ExecutedDate DESC;
    END TRY
    BEGIN CATCH
		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('B³¹d podczas pobierania logów operacji: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- Procedura do usuwania dokumentu z bazy po ID dokumentu
CREATE PROCEDURE SP_DeleteDocumentByID
    @DocumentID INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(MAX) = NULL;
    DECLARE @Deleted BIT = 0;

    BEGIN TRY
        DELETE FROM Documents
        WHERE DocumentID = @DocumentID;

        SET @Deleted = @@ROWCOUNT;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH;

    INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
    VALUES (
        @DocumentID,
        'DELETE',
        'Attempted deletion of document by DocumentID',
        @Deleted,
        @ErrorMessage
    );
END;
GO

-- Procedura do usuwania dokumentu z bazy po ID Wystawy
CREATE PROCEDURE SP_DeleteDocumentsByExhibitID
    @ExhibitID NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(MAX) = NULL;

    BEGIN TRY
        DECLARE @DocIDs TABLE (DocumentID INT);

        INSERT INTO @DocIDs (DocumentID)
        SELECT DocumentID FROM Documents WHERE ExhibitID = @ExhibitID;

        DELETE FROM Documents WHERE ExhibitID = @ExhibitID;

        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        SELECT
            DocumentID,
            'DELETE',
            'Deleted due to ExhibitID=' + @ExhibitID,
            1,
            NULL
        FROM @DocIDs;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = ERROR_MESSAGE();

        INSERT INTO DocumentOperationLog (DocumentID, OperationType, OperationDescription, Success, ErrorMessage)
        VALUES (
            NULL,
            'DELETE',
            'Bulk delete by ExhibitID failed',
            0,
            @ErrorMessage
        );
    END CATCH;
END;
GO

PRINT 'Procedury sk³adowane zosta³y utworzone pomyœlnie!';
GO