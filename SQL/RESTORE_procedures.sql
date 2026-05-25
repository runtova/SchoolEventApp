-- =====================================================================
-- ШАГ 1: Запустите этот блок чтобы увидеть имена ваших таблиц
-- =====================================================================
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- =====================================================================
-- ШАГ 2: Найдите в результатах:
--   - таблицу с мероприятиями  (что-то вроде Event, Meropriyatiya...)
--   - таблицу с участниками    (Participant, Uchastnik...)
--   - таблицу связи            (EventParticipant, Participation...)
--   - таблицу организаторов    (Organizer...)
-- и замените имена ниже перед выполнением ШАГ 3.
-- =====================================================================

-- =====================================================================
-- ШАГ 3: Восстановление процедур (замените имена таблиц если нужно)
-- =====================================================================

USE School_event
GO

-- Посмотрите какие столбцы есть в таблицах:
-- SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ВАШ_НАЗВАНИЕ_ТАБЛИЦЫ'

-- ── sp_Report_AllEvents ──────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Report_AllEvents
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- ЗАМЕНИТЕ имена таблиц если нужно:
    SELECT
        ROW_NUMBER() OVER (ORDER BY e.date_e, e.event_cod) AS Nomer,
        e.event_name                                        AS Nazvanie,
        CONVERT(VARCHAR, e.date_e, 104)                     AS Data,
        LEFT(CONVERT(VARCHAR, e.time_e, 108), 5)            AS Vremya,
        e.place                                             AS Mesto,
        e.type_e                                            AS Tip,
        o.organizer_FIO                                     AS Organizator,
        COUNT(ep.participant_cod)                           AS KolUchastnikov
    FROM Event e                              -- << ЗАМЕНИТЕ на ваше имя
    LEFT JOIN Organizer  o  ON o.organizer_cod  = e.organizer_cod   -- << ЗАМЕНИТЕ
    LEFT JOIN Participation ep ON ep.event_cod = e.event_cod         -- << ЗАМЕНИТЕ
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
        ROW_NUMBER() OVER (ORDER BY COUNT(ep.event_cod) DESC) AS Mesto,
        p.participant_FIO                                      AS Uchastnik,
        p.class_p                                             AS Klass,
        ISNULL(p.email, '')                                   AS Email,
        COUNT(ep.event_cod)                                   AS KolMeropriyatiy,
        ISNULL(CONVERT(VARCHAR, MIN(e.date_e), 104), '—')    AS PervoeUchastie,
        ISNULL(CONVERT(VARCHAR, MAX(e.date_e), 104), '—')    AS PosledneeUchastie
    FROM Participant p                                  -- << ЗАМЕНИТЕ
    LEFT JOIN Participation ep ON ep.participant_cod = p.participant_cod  -- << ЗАМЕНИТЕ
    LEFT JOIN Event e          ON e.event_cod        = ep.event_cod       -- << ЗАМЕНИТЕ
                              AND (@DateFrom IS NULL OR e.date_e >= @DateFrom)
                              AND (@DateTo   IS NULL OR e.date_e <= @DateTo)
    GROUP BY p.participant_cod, p.participant_FIO, p.class_p, p.email
    ORDER BY KolMeropriyatiy DESC;
END
GO

PRINT 'Готово!';
GO



USE School_event
GO
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Eventt' ORDER BY ORDINAL_POSITION;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Organizer' ORDER BY ORDINAL_POSITION;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Participant' ORDER BY ORDINAL_POSITION;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Participation' ORDER BY ORDINAL_POSITION;
