-- =============================================
-- Обновлённые процедуры, использующие представления
-- vw_EventsWithDetails и vw_ParticipantsWithStats
-- Выполнить в SQL Server Management Studio на базе School_event
-- =============================================

USE School_event;
GO

-- ─────────────────────────────────────────────
-- sp_Event_GetAll
-- Раньше: JOIN Eventt + Organizer + Participation
-- Теперь: SELECT из vw_EventsWithDetails
-- ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Event_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM vw_EventsWithDetails
    ORDER BY date_e DESC;
END
GO

-- ─────────────────────────────────────────────
-- sp_Event_Search
-- Раньше: JOIN Eventt + Organizer + Participation + WHERE
-- Теперь: фильтр по vw_EventsWithDetails
-- ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Event_Search
    @SearchText VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM vw_EventsWithDetails
    WHERE event_name LIKE '%' + @SearchText + '%'
       OR place      LIKE '%' + @SearchText + '%'
       OR type_e     LIKE '%' + @SearchText + '%'
    ORDER BY date_e DESC;
END
GO

-- ─────────────────────────────────────────────
-- sp_Report_AllEvents
-- Раньше: JOIN Eventt + Organizer + Participation + GROUP BY
-- Теперь: SELECT из vw_EventsWithDetails, данные уже сгруппированы
-- ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Report_AllEvents
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ROW_NUMBER() OVER (ORDER BY date_e, event_cod) AS Nomer,
        event_name                                      AS Nazvanie,
        CONVERT(VARCHAR, date_e, 104)                   AS Data,
        LEFT(CONVERT(VARCHAR, time_e, 108), 5)          AS Vremya,
        place                                           AS Mesto,
        type_e                                          AS Tip,
        organizer_FIO                                   AS Organizator,
        participants_count                              AS KolUchastnikov
    FROM vw_EventsWithDetails
    WHERE (@DateFrom IS NULL OR date_e >= @DateFrom)
      AND (@DateTo   IS NULL OR date_e <= @DateTo)
    ORDER BY date_e, event_cod;
END
GO

-- ─────────────────────────────────────────────
-- sp_Dashboard_GetStats
-- Раньше: JOIN Eventt + Participation + GROUP BY
-- Теперь: агрегация по vw_EventsWithDetails
-- ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Dashboard_GetStats
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(DISTINCT event_cod)   AS TotalEvents,
        SUM(participants_count)     AS TotalParticipants,
        MIN(date_e)                 AS EarliestDate,
        MAX(date_e)                 AS LatestDate
    FROM vw_EventsWithDetails
    WHERE (@DateFrom   IS NULL OR date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR type_e  = @TypeFilter);
END
GO

-- ─────────────────────────────────────────────
-- sp_Report_ParticipantActivity
-- Раньше: JOIN Participant + Participation + Eventt + GROUP BY
-- Теперь: SELECT из vw_ParticipantsWithStats
-- ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Report_ParticipantActivity
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ROW_NUMBER() OVER (ORDER BY events_count DESC) AS Mesto,
        student_FIO                                     AS Uchastnik,
        class                                           AS Klass,
        email_s                                         AS Email,
        events_count                                    AS KolMeropriyatiy
    FROM vw_ParticipantsWithStats
    ORDER BY events_count DESC;
END
GO

PRINT 'Процедуры обновлены — теперь используют представления!';
GO
