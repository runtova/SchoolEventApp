-- ============================================================
-- Миграция: замена image_data (VarBinary) на image_path (NVARCHAR)
-- База: School_event
-- Запускать ОДИН РАЗ в SQL Server Management Studio
-- ============================================================

USE School_event;
GO

-- 1. Удаляем старую колонку с байтами (если есть)
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Event' AND COLUMN_NAME = 'image_data'
)
BEGIN
    ALTER TABLE Event DROP COLUMN image_data;
    PRINT 'Колонка image_data удалена.';
END
GO

-- 2. Добавляем новую колонку для пути к файлу (если ещё нет)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Event' AND COLUMN_NAME = 'image_path'
)
BEGIN
    ALTER TABLE Eventt ADD image_path NVARCHAR(260) NULL;
    PRINT 'Колонка image_path добавлена.';
END
GO

-- ============================================================
-- 3. Обновляем хранимую процедуру вставки
-- ============================================================
IF OBJECT_ID('sp_Event_Insert', 'P') IS NOT NULL
    DROP PROCEDURE sp_Event_Insert;
GO

CREATE PROCEDURE sp_Event_Insert
    @event_cod        SMALLINT,
    @event_name       NVARCHAR(30),
    @date_e           DATE,
    @time_e           TIME,
    @place            NVARCHAR(20),
    @organizer_cod    SMALLINT,
    @type_e           NVARCHAR(20),
    @max_participants INT           = NULL,
    @image_path       NVARCHAR(260) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Event
        (event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, max_participants, image_path)
    VALUES
        (@event_cod, @event_name, @date_e, @time_e, @place, @organizer_cod, @type_e, @max_participants, @image_path);
END
GO

-- ============================================================
-- 4. Обновляем хранимую процедуру обновления
--    Если @image_path передан как NULL — поле НЕ меняется (картинка осталась прежней).
--    Если передана пустая строка ('') — картинка очищается.
-- ============================================================
IF OBJECT_ID('sp_Event_Update', 'P') IS NOT NULL
    DROP PROCEDURE sp_Event_Update;
GO

CREATE PROCEDURE sp_Event_Update
    @event_cod        SMALLINT,
    @event_name       NVARCHAR(30),
    @date_e           DATE,
    @time_e           TIME,
    @place            NVARCHAR(20),
    @organizer_cod    SMALLINT,
    @type_e           NVARCHAR(20),
    @max_participants INT           = NULL,
    @image_path       NVARCHAR(260) = NULL   -- NULL = не менять; '' = очистить
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Eventt SET
        event_name       = @event_name,
        date_e           = @date_e,
        time_e           = @time_e,
        place            = @place,
        organizer_cod    = @organizer_cod,
        type_e           = @type_e,
        max_participants = @max_participants,
        image_path       = CASE
                               WHEN @image_path IS NULL THEN image_path  -- не трогаем
                               WHEN @image_path = ''    THEN NULL        -- очищаем
                               ELSE @image_path                          -- новый путь
                           END
    WHERE event_cod = @event_cod;
END
GO

PRINT 'Миграция выполнена успешно.';
GO


SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Eventt';


ALTER TABLE Eventt DROP COLUMN image_data;


EXEC sp_helptext 'sp_Event_GetAll';



ALTER PROCEDURE sp_Event_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.event_cod,
        e.event_name,
        e.date_e,
        e.time_e,
        e.place,
        e.organizer_cod,
        e.type_e,
        e.image_path,
        e.max_participants,
        ISNULL(o.organizer_FIO, '')  AS organizer_FIO,
        COUNT(p.student_cod)         AS participants_count
    FROM Eventt e
    LEFT JOIN Organizer     o ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p ON p.event_cod     = e.event_cod
    GROUP BY
        e.event_cod, e.event_name, e.date_e, e.time_e,
        e.place, e.organizer_cod, e.type_e, e.image_path,
        e.max_participants, o.organizer_FIO
    ORDER BY e.date_e DESC;
END