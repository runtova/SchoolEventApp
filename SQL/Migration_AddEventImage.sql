-- =============================================
-- Миграция: добавление поля image_path для мероприятий
-- Выполнить один раз на существующей базе School_event
-- =============================================

use School_event

-- 1. Добавляем колонку в таблицу (если ещё не существует)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Eventt') AND name = 'image_path'
)
BEGIN
    ALTER TABLE Eventt ADD image_path NVARCHAR(500) NULL;
    PRINT 'Колонка image_path добавлена.';
END
ELSE
    PRINT 'Колонка image_path уже существует.';
GO

-- 2. Обновляем sp_Event_GetAll — включаем image_path в выборку
CREATE OR ALTER PROCEDURE sp_Event_GetAll
AS
BEGIN
    SELECT
        event_cod,
        event_name,
        date_e,
        time_e,
        place,
        organizer_cod,
        type_e,
        image_path
    FROM Eventt
    ORDER BY date_e DESC;
END
GO

-- 3. Обновляем sp_Event_Insert — принимаем image_path
CREATE OR ALTER PROCEDURE sp_Event_Insert
    @event_cod     SMALLINT,
    @event_name    VARCHAR(30),
    @date_e        DATE,
    @time_e        TIME,
    @place         VARCHAR(20),
    @organizer_cod SMALLINT,
    @type_e        VARCHAR(20),
    @image_path    NVARCHAR(500) = NULL
AS
BEGIN
    INSERT INTO Eventt (event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, image_path)
    VALUES (@event_cod, @event_name, @date_e, @time_e, @place, @organizer_cod, @type_e, @image_path);
END
GO

-- 4. Обновляем sp_Event_Update — принимаем image_path
CREATE OR ALTER PROCEDURE sp_Event_Update
    @event_cod     SMALLINT,
    @event_name    VARCHAR(30),
    @date_e        DATE,
    @time_e        TIME,
    @place         VARCHAR(20),
    @organizer_cod SMALLINT,
    @type_e        VARCHAR(20),
    @image_path    NVARCHAR(500) = NULL
AS
BEGIN
    UPDATE Eventt
    SET
        event_name    = @event_name,
        date_e        = @date_e,
        time_e        = @time_e,
        place         = @place,
        organizer_cod = @organizer_cod,
        type_e        = @type_e,
        image_path    = @image_path
    WHERE event_cod = @event_cod;
END
GO

-- 5. Обновляем sp_Event_Search — включаем image_path
CREATE OR ALTER PROCEDURE sp_Event_Search
    @SearchText VARCHAR(100)
AS
BEGIN
    SELECT
        event_cod,
        event_name,
        date_e,
        time_e,
        place,
        organizer_cod,
        type_e,
        image_path
    FROM Eventt
    WHERE event_name    LIKE '%' + @SearchText + '%'
       OR place         LIKE '%' + @SearchText + '%'
       OR type_e        LIKE '%' + @SearchText + '%'
    ORDER BY date_e DESC;
END
GO

-- Запусти это в SSMS чтобы найти таблицу мероприятий
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

select * from Eventt

USE School_event;
SELECT event_name, image_path FROM Eventt;

USE School_event;
GO

CREATE OR ALTER PROCEDURE sp_Participation_GetAll
AS
BEGIN
    SELECT
        p.event_cod,
        p.student_cod,
        e.event_name,
        e.date_e,
        e.time_e,
        e.place,
        e.type_e
    FROM Participation p
    JOIN Eventt e ON p.event_cod = e.event_cod
    ORDER BY e.date_e DESC;
END
GO