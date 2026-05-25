-- =============================================
-- ПАТЧ: хранимая процедура sp_Event_GetUpcoming
-- Возвращает только предстоящие мероприятия
-- (date_e >= сегодня). Используется опционально
-- вместо sp_Event_GetAll на стороне сервера.
-- =============================================

USE School_event;
GO

CREATE OR ALTER PROCEDURE sp_Event_GetUpcoming
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM   Eventt
    WHERE  date_e >= CAST(GETDATE() AS DATE)
    ORDER  BY date_e ASC;
END
GO

-- =============================================
-- ПАТЧ: sp_Participation_Insert — серверная
-- проверка даты. Отклоняет запись на прошедшее
-- мероприятие даже при прямом вызове процедуры.
-- =============================================

CREATE OR ALTER PROCEDURE sp_Participation_Insert
    @event_cod   SMALLINT,
    @student_cod SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY

        -- Проверка: мероприятие существует и ещё не прошло
        IF NOT EXISTS (
            SELECT 1 FROM Eventt
            WHERE event_cod = @event_cod
              AND date_e >= CAST(GETDATE() AS DATE)
        )
            THROW 50020, 'Нельзя записаться на прошедшее или несуществующее мероприятие.', 1;

        -- Проверка: участник ещё не записан
        IF EXISTS (
            SELECT 1 FROM Participation
            WHERE event_cod = @event_cod AND student_cod = @student_cod
        )
            THROW 50021, 'Участник уже записан на это мероприятие.', 1;

        INSERT INTO Participation (event_cod, student_cod)
        VALUES (@event_cod, @student_cod);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
