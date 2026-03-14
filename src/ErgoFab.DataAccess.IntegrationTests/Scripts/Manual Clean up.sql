DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql += 'DROP DATABASE [' + name + '];' + CHAR(10)
FROM sys.databases
WHERE name LIKE 'Ergo Fab Test%';

PRINT @sql;
EXEC sp_executesql @sql;
