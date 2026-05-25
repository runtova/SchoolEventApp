-- ============================================================
--  SchoolEventApp — Резервное копирование и восстановление
--  База данных : School_event  |  Сервер: .\SQLEXPRESS
--
--  ШАГ 1: Замените путь в переменной @BackupPath (строка ~25)
--          на реальный путь к папке SQL\Backup вашего проекта.
--          Папка должна существовать — создайте её вручную
--          или запустите в cmd: mkdir "C:\ВашПуть\SQL\Backup"
--
--  ШАГ 2: Выделите нужную секцию и нажмите F5
-- ============================================================


-- ============================================================
--  СЕКЦИЯ 1: BACKUP — создание резервной копии
--  Выделите весь блок до GO и нажмите F5
-- ============================================================

USE master;
GO

DECLARE @BackupPath NVARCHAR(512);
DECLARE @BackupFile NVARCHAR(512);
DECLARE @BackupName NVARCHAR(512);
DECLARE @Ts        NVARCHAR(20);

-- !! ЗАМЕНИТЕ ПУТЬ НА СВОЙ !!
SET @BackupPath = N'C:\Projects\SchoolEventApp\SchoolEventApp\SQL\Backup\';

SET @Ts         = REPLACE(REPLACE(REPLACE(
                      CONVERT(NVARCHAR(20), GETDATE(), 120),
                  '-',''), ' ','_'), ':','');
SET @BackupFile = @BackupPath + N'School_event_' + @Ts + N'.bak';
SET @BackupName = N'School_event Full Backup ' + @Ts;

PRINT N'Создаём резервную копию: ' + @BackupFile;

BACKUP DATABASE [School_event]
    TO DISK = @BackupFile
    WITH
        FORMAT,
        INIT,
        NAME        = @BackupName,
        COMPRESSION,
        STATS       = 10,
        CHECKSUM;

PRINT N'>>> Готово. Файл: ' + @BackupFile;
GO


-- ============================================================
--  СЕКЦИЯ 2: VERIFY — проверка целостности
--  Замените путь к .bak файлу и запустите
-- ============================================================

-- Замените путь на актуальный .bak файл:
DECLARE @BakFile NVARCHAR(512) =
    N'C:\Projects\SchoolEventApp\SchoolEventApp\SQL\Backup\School_event_20260521_124456.bak';

-- 2а: информация о резервной копии
RESTORE HEADERONLY FROM DISK = @BakFile;

-- 2б: список файлов внутри .bak
RESTORE FILELISTONLY FROM DISK = @BakFile;

-- 2в: проверка контрольной суммы (без изменения базы)
RESTORE VERIFYONLY FROM DISK = @BakFile WITH CHECKSUM;

PRINT N'>>> Файл резервной копии прошёл проверку целостности.';
GO


-- ============================================================
--  СЕКЦИЯ 3: RESTORE — восстановление
--  !! Раскомментируйте и запустите ТОЛЬКО при необходимости !!
--  !! Закройте приложение SchoolEventApp перед запуском     !!
-- ============================================================
/*
USE master;
GO

DECLARE @BakFile NVARCHAR(512) =
    N'C:\Projects\SchoolEventApp\SchoolEventApp\SQL\Backup\School_event_20260521_124456.bak';

-- Отключить все активные соединения
ALTER DATABASE [School_event] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE [School_event]
    FROM DISK = @BakFile
    WITH REPLACE, STATS = 10, RECOVERY;

-- Вернуть в обычный режим
ALTER DATABASE [School_event] SET MULTI_USER;

PRINT N'>>> База данных School_event успешно восстановлена.';
GO
*/

SELECT physical_name 
FROM sys.master_files 
WHERE database_id = DB_ID('School_event');