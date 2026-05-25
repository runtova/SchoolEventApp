USE School_event;
GO

-- ── sp_Dashboard_GetStats: добавляем названия первого и последнего мероприятия ──

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
        MAX(e.date_e)                AS LatestDate,
        -- название первого мероприятия
        (SELECT TOP 1 e2.event_name
         FROM Eventt e2
         LEFT JOIN Participation p2 ON p2.event_cod = e2.event_cod
         WHERE (@DateFrom   IS NULL OR e2.date_e >= @DateFrom)
           AND (@DateTo     IS NULL OR e2.date_e <= @DateTo)
           AND (@TypeFilter IS NULL OR e2.type_e  = @TypeFilter)
         ORDER BY e2.date_e ASC)     AS EarliestName,
        -- название последнего мероприятия
        (SELECT TOP 1 e3.event_name
         FROM Eventt e3
         LEFT JOIN Participation p3 ON p3.event_cod = e3.event_cod
         WHERE (@DateFrom   IS NULL OR e3.date_e >= @DateFrom)
           AND (@DateTo     IS NULL OR e3.date_e <= @DateTo)
           AND (@TypeFilter IS NULL OR e3.type_e  = @TypeFilter)
         ORDER BY e3.date_e DESC)    AS LatestName
    FROM Eventt e
    LEFT JOIN Participation p ON p.event_cod = e.event_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter);
END
GO

-- ── sp_Dashboard_TopOrganizers: топ организаторов по числу участников ──────

CREATE OR ALTER PROCEDURE sp_Dashboard_TopOrganizers
    @DateFrom   DATE        = NULL,
    @DateTo     DATE        = NULL,
    @TypeFilter VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 8
        o.organizer_FIO                AS OrganizerName,
        COUNT(p.student_cod)           AS ParticipantCount
    FROM Organizer o
        JOIN Eventt       e ON e.organizer_cod = o.organizer_cod
        LEFT JOIN Participation p ON p.event_cod = e.event_cod
    WHERE (@DateFrom   IS NULL OR e.date_e >= @DateFrom)
      AND (@DateTo     IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e  = @TypeFilter)
    GROUP BY o.organizer_cod, o.organizer_FIO
    HAVING COUNT(p.student_cod) > 0
    ORDER BY ParticipantCount DESC;
END
GO

PRINT 'Процедуры дашборда обновлены.';
