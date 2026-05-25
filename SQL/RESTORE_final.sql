USE School_event
GO

-- ── sp_Report_AllEvents ──────────────────────────────────────────────
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
        COUNT(p.student_cod)                                AS KolUchastnikov
    FROM Eventt e
    LEFT JOIN Organizer     o  ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p  ON p.event_cod     = e.event_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo   IS NULL OR e.date_e <= @DateTo)
    GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e,
             e.place, e.type_e, o.organizer_FIO
    ORDER BY e.date_e, e.event_cod;
END
GO

-- ── sp_Report_ParticipantActivity ─────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Report_ParticipantActivity
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ROW_NUMBER() OVER (ORDER BY COUNT(pr.event_cod) DESC)  AS Mesto,
        s.student_FIO                                           AS Uchastnik,
        s.class                                                 AS Klass,
        ISNULL(s.email_s, '')                                   AS Email,
        COUNT(pr.event_cod)                                     AS KolMeropriyatiy,
        ISNULL(CONVERT(VARCHAR, MIN(e.date_e), 104), '—')      AS PervoeUchastie,
        ISNULL(CONVERT(VARCHAR, MAX(e.date_e), 104), '—')      AS PosledneeUchastie
    FROM Participant s
    LEFT JOIN Participation pr ON pr.student_cod = s.student_cod
    LEFT JOIN Eventt e         ON e.event_cod    = pr.event_cod
                              AND (@DateFrom IS NULL OR e.date_e >= @DateFrom)
                              AND (@DateTo   IS NULL OR e.date_e <= @DateTo)
    GROUP BY s.student_cod, s.student_FIO, s.class, s.email_s
    ORDER BY KolMeropriyatiy DESC;
END
GO

-- ── sp_Report_EventParticipants ───────────────────────────────────────
-- Восстанавливаем с правильными именами столбцов
CREATE OR ALTER PROCEDURE sp_Report_EventParticipants
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.event_name                    AS Meropriyatie,
        CONVERT(VARCHAR, e.date_e, 104) AS Data,
        e.type_e                        AS Tip,
        e.place                         AS Mesto,
        o.organizer_FIO                 AS Organizator,
        s.student_FIO                   AS Uchastnik,
        s.class                         AS Klass,
        ISNULL(s.phone_number_s, '')    AS Telefon
    FROM Participation pr
    JOIN Eventt      e  ON e.event_cod   = pr.event_cod
    JOIN Participant s  ON s.student_cod = pr.student_cod
    LEFT JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter)
    ORDER BY e.date_e, e.event_name, s.student_FIO;
END
GO

-- ── sp_Dashboard_GetStats ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Dashboard_GetStats
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(DISTINCT e.event_cod)  AS TotalEvents,
        COUNT(p.student_cod)         AS TotalParticipants,
        MIN(e.date_e)                AS EarliestDate,
        MAX(e.date_e)                AS LatestDate
    FROM Eventt e
    LEFT JOIN Participation p ON p.event_cod = e.event_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter);
END
GO

-- ── sp_Dashboard_ByType ───────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Dashboard_ByType
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.type_e        AS EventType,
        COUNT(*)        AS EventCount
    FROM Eventt e
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter)
    GROUP BY e.type_e
    ORDER BY EventCount DESC;
END
GO

-- ── sp_Dashboard_ByDate ───────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Dashboard_ByDate
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.date_e        AS EventDate,
        COUNT(*)        AS EventCount
    FROM Eventt e
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter)
    GROUP BY e.date_e
    ORDER BY e.date_e;
END
GO

-- ── sp_Dashboard_GetTypes ─────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Dashboard_GetTypes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT type_e FROM Eventt WHERE type_e IS NOT NULL ORDER BY type_e;
END
GO

PRINT 'Все процедуры восстановлены успешно!';
GO
