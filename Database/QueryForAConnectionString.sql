DECLARE @ConnectionString NVARCHAR(MAX);
DECLARE @IsLocalDb BIT = 0;

-- Detect LocalDB by instance name pattern
IF CAST(SERVERPROPERTY('InstanceName') AS NVARCHAR) LIKE 'LOCALDB#%'
    SET @IsLocalDb = 1;

IF @IsLocalDb = 1
BEGIN
    SET @ConnectionString = 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MuseumXMLArchive;Integrated Security=True';
END
ELSE
BEGIN
    DECLARE @ServerName NVARCHAR(128) = CAST(SERVERPROPERTY('MachineName') AS NVARCHAR(128));
    DECLARE @InstanceName NVARCHAR(128) = CAST(SERVERPROPERTY('InstanceName') AS NVARCHAR(128));

    SET @ConnectionString = 'Data Source=' + @ServerName
        + ISNULL(CASE WHEN @InstanceName IS NOT NULL THEN '\' + @InstanceName ELSE '' END, '')
        + ';Initial Catalog=MuseumXMLArchive;Integrated Security=True';
END

-- Output the connection string
SELECT @ConnectionString AS ConnectionString;