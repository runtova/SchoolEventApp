-- =====================================================================
-- ОБНОВЛЕНИЕ ХРАНИМЫХ ПРОЦЕДУР: фильтрация по учебному году
-- Параметры @DateFrom / @DateTo уже существуют во всех процедурах —
-- приложение просто передаёт 01.09.YYYY и 31.08.(YYYY+1).
-- Этот скрипт нужен ТОЛЬКО если в ваших процедурах фильтр по дате
-- ещё не реализован. Выполните в SQL Server Management Studio.
-- =====================================================================

USE School_event
GO

-- ── sp_Report_AllEvents ──────────────────────────────────────────────
-- Добавляем параметры @DateFrom / @DateTo (если их не было)
CREATE OR ALTER PROCEDURE sp_Report_AllEvents
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ROW_NUMBER() OVER (ORDER BY e.date_e, e.event_cod) AS Nomer,
        e.event_name                                        AS Nazvanie,
        CONVERT(VARCHAR, e.date_e, 104)                     AS Data,
        LEFT(CONVERT(VARCHAR, e.time_e, 108), 5)            AS Vremya,
        e.place                                             AS Mesto,
        e.type_e                                            AS Tip,
        o.organizer_FIO                                     AS Organizator,
        COUNT(ep.participant_cod)                           AS KolUchastnikov
    FROM Events e
    LEFT JOIN Organizers  o  ON o.organizer_cod  = e.organizer_cod
    LEFT JOIN EventParticipants ep ON ep.event_cod = e.event_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo   IS NULL OR e.date_e <= @DateTo)
    GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e,
             e.place, e.type_e, o.organizer_FIO
    ORDER BY e.date_e, e.event_cod;
END
GO

-- ── sp_Report_ParticipantActivity ────────────────────────────────────
-- Рейтинг считается по мероприятиям в выбранном периоде
CREATE OR ALTER PROCEDURE sp_Report_ParticipantActivity
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ROW_NUMBER() OVER (ORDER BY COUNT(ep.event_cod) DESC) AS Mesto,
        p.participant_FIO                                      AS Uchastnik,
        p.class_p                                             AS Klass,
        ISNULL(p.email, '')                                   AS Email,
        COUNT(ep.event_cod)                                   AS KolMeropriyatiy,
        ISNULL(CONVERT(VARCHAR, MIN(e.date_e), 104), '—')    AS PervoeUchastie,
        ISNULL(CONVERT(VARCHAR, MAX(e.date_e), 104), '—')    AS PosledneeUchastie
    FROM Participants p
    LEFT JOIN EventParticipants ep ON ep.participant_cod = p.participant_cod
    LEFT JOIN Events e             ON e.event_cod        = ep.event_cod
                                  AND (@DateFrom IS NULL OR e.date_e >= @DateFrom)
                                  AND (@DateTo   IS NULL OR e.date_e <= @DateTo)
    GROUP BY p.participant_cod, p.participant_FIO, p.class_p, p.email
    ORDER BY KolMeropriyatiy DESC;
END
GO

-- ── sp_Report_EventParticipants (уже имеет @DateFrom/@DateTo) ────────
-- Если в вашей версии их нет — раскомментируйте и выполните:
/*
CREATE OR ALTER PROCEDURE sp_Report_EventParticipants
    @DateFrom   DATE         = NULL,
    @DateTo     DATE         = NULL,
    @TypeFilter VARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.event_name  AS Meropriyatie,
        CONVERT(VARCHAR, e.date_e, 104) AS Data,
        e.type_e      AS Tip,
        e.place       AS Mesto,
        o.organizer_FIO AS Organizator,
        p.participant_FIO AS Uchastnik,
        p.class_p     AS Klass,
        ISNULL(p.phone, '') AS Telefon
    FROM EventParticipants ep
    JOIN Events      e ON e.event_cod       = ep.event_cod
    JOIN Participants p ON p.participant_cod = ep.participant_cod
    LEFT JOIN Organizers o ON o.organizer_cod = e.organizer_cod
    WHERE (@DateFrom   IS NULL OR e.date_e  >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e  <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e   = @TypeFilter)
    ORDER BY e.date_e, e.event_name, p.participant_FIO;
END
GO
*/

-- ── sp_Dashboard_GetStats (уже имеет фильтры) ────────────────────────
-- Убедитесь, что в вашей версии есть @DateFrom, @DateTo, @TypeFilter.
-- Если нет — раскомментируйте:
/*
CREATE OR ALTER PROCEDURE sp_Dashboard_GetStats
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(DISTINCT e.event_cod)       AS TotalEvents,
        COUNT(ep.participant_cod)          AS TotalParticipants,
        MIN(e.date_e)                      AS EarliestDate,
        MAX(e.date_e)                      AS LatestDate
    FROM Events e
    LEFT JOIN EventParticipants ep ON ep.event_cod = e.event_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter);
END
GO
*/

-- ── sp_Dashboard_ByType / sp_Dashboard_ByDate / sp_Dashboard_GetTypes ─
-- Аналогично — убедитесь, что принимают @DateFrom, @DateTo, @TypeFilter.
-- Приложение уже передаёт эти параметры, поэтому если процедуры
-- их принимают (пусть игнорируют) — всё заработает без правок SQL.

PRINT 'Готово. Процедуры обновлены для фильтрации по учебному году.';
GO

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME
