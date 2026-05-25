
-- =============================================
-- ОТЧЁТЫ ДЛЯ ReportsWindow
-- =============================================

-- Отчёт 1: Все мероприятия с количеством участников
CREATE OR ALTER PROCEDURE sp_Report_AllEvents
AS
BEGIN
    SELECT
        ROW_NUMBER() OVER (ORDER BY e.date_e DESC) AS Nomer,
        e.event_name     AS Nazvanie,
        CONVERT(VARCHAR(10), e.date_e, 104)  AS Data,
        CONVERT(VARCHAR(5),  e.time_e, 108)  AS Vremya,
        e.place          AS Mesto,
        e.type_e         AS Tip,
        ISNULL(o.organizer_FIO, '—') AS Organizator,
        COUNT(p.student_cod)         AS KolUchastnikov
    FROM Eventt e
    LEFT JOIN Organizer     o ON e.organizer_cod = o.organizer_cod
    LEFT JOIN Participation p ON e.event_cod    = p.event_cod
    GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e,
             e.place, e.type_e, o.organizer_FIO
    ORDER BY e.date_e DESC
END
GO

-- Отчёт 2: Участники с фильтром по типу и датам
CREATE OR ALTER PROCEDURE sp_Report_EventParticipants
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(20) = NULL
AS
BEGIN
    SELECT
        e.event_name   AS Meropriyatie,
        CONVERT(VARCHAR(10), e.date_e, 104) AS Data,
        e.type_e       AS Tip,
        e.place        AS Mesto,
        ISNULL(o.organizer_FIO, '—') AS Organizator,
        s.student_FIO  AS Uchastnik,
        s.class        AS Klass,
        s.phone_number_s AS Telefon
    FROM Participation p
    JOIN Eventt      e ON p.event_cod   = e.event_cod
    JOIN Participant s ON p.student_cod = s.student_cod
    LEFT JOIN Organizer o ON e.organizer_cod = o.organizer_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR @TypeFilter = '' OR e.type_e = @TypeFilter)
    ORDER BY e.date_e DESC, e.event_name, s.student_FIO
END
GO

-- Отчёт 3: Рейтинг активности участников
CREATE OR ALTER PROCEDURE sp_Report_ParticipantActivity
AS
BEGIN
    SELECT
        ROW_NUMBER() OVER (ORDER BY COUNT(p.event_cod) DESC) AS Mesto,
        s.student_FIO  AS Uchastnik,
        s.class        AS Klass,
        s.email_s      AS Email,
        COUNT(p.event_cod) AS KolMeropriyatiy,
        ISNULL(CONVERT(VARCHAR(10), MIN(e.date_e), 104), '—') AS PervoeUchastie,
        ISNULL(CONVERT(VARCHAR(10), MAX(e.date_e), 104), '—') AS PosledneeUchastie
    FROM Participant s
    LEFT JOIN Participation p ON s.student_cod = p.student_cod
    LEFT JOIN Eventt e        ON p.event_cod   = e.event_cod
    GROUP BY s.student_cod, s.student_FIO, s.class, s.email_s
    ORDER BY KolMeropriyatiy DESC, s.student_FIO
END
GO

USE School_event;
EXEC sp_Participation_GetAll;

USE School_event;
EXEC sp_helptext 'sp_Organizer_Insert';

USE School_event;
SELECT OBJECT_DEFINITION(OBJECT_ID('sp_Organizer_Insert'));

USE School_event;
GO

CREATE OR ALTER PROCEDURE sp_Organizer_Insert
    @organizer_cod   SMALLINT,
    @organizer_FIO   VARCHAR(30),
    @job_name        VARCHAR(30),
    @phone_number_o  VARCHAR(15),
    @email_0         VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO Organizer (organizer_cod, organizer_FIO, job_name, phone_number_o, email_0)
        VALUES (@organizer_cod, @organizer_FIO, @job_name, @phone_number_o, @email_0);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

SELECT OBJECT_DEFINITION(OBJECT_ID('sp_Organizer_Update'));



CREATE OR ALTER PROCEDURE sp_Organizer_Update
    @organizer_cod   SMALLINT,
    @organizer_FIO   VARCHAR(30),
    @job_name        VARCHAR(30),
    @phone_number_o  VARCHAR(15),
    @email_o         VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE Organizer
        SET organizer_FIO  = @organizer_FIO,
            job_name       = @job_name,
            phone_number_o = @phone_number_o,
            email_0        = @email_o
        WHERE organizer_cod = @organizer_cod;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

USE School_event;
EXEC sp_Organizer_GetAll;

USE School_event;
SELECT * FROM Users;