-- =====================================================
-- Миграция: добавление поля max_participants
-- Запустить один раз в базе данных School_event
-- =====================================================

USE School_event;
GO

-- 1. Добавляем столбец (если ещё нет)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Eventt' AND COLUMN_NAME = 'max_participants'
)
BEGIN
    ALTER TABLE Eventt ADD max_participants INT NULL;
    PRINT 'Столбец max_participants добавлен.';
END
ELSE
    PRINT 'Столбец max_participants уже существует.';
GO

-- 2. Обновляем sp_Event_Insert — добавляем параметр
CREATE OR ALTER PROCEDURE sp_Event_Insert
    @event_cod       SMALLINT,
    @event_name      VARCHAR(30),
    @date_e          DATE,
    @time_e          TIME,
    @place           VARCHAR(20),
    @organizer_cod   SMALLINT,
    @type_e          VARCHAR(20),
    @max_participants INT = NULL,
    @image_path      VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO Eventt (event_cod, event_name, date_e, time_e, place,
                            organizer_cod, type_e, max_participants, image_path)
        VALUES (@event_cod, @event_name, @date_e, @time_e, @place,
                @organizer_cod, @type_e, @max_participants, @image_path);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 3. Обновляем sp_Event_Update — добавляем параметр
CREATE OR ALTER PROCEDURE sp_Event_Update
    @event_cod       SMALLINT,
    @event_name      VARCHAR(30),
    @date_e          DATE,
    @time_e          TIME,
    @place           VARCHAR(20),
    @organizer_cod   SMALLINT,
    @type_e          VARCHAR(20),
    @max_participants INT = NULL,
    @image_path      VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE Eventt
        SET event_name      = @event_name,
            date_e          = @date_e,
            time_e          = @time_e,
            place           = @place,
            organizer_cod   = @organizer_cod,
            type_e          = @type_e,
            max_participants = @max_participants,
            image_path      = @image_path
        WHERE event_cod = @event_cod;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'Миграция завершена успешно.';
