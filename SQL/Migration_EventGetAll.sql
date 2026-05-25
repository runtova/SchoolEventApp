-- =============================================
-- Обновление sp_Event_GetAll и sp_Event_Search
-- Теперь возвращают organizer_FIO и participants_count
-- одним запросом — без лишних обращений из C#.
-- Выполнить один раз в SSMS на базе School_event.
-- =============================================

USE School_event;
GO

CREATE OR ALTER PROCEDURE sp_Event_GetAll
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
        ISNULL(o.organizer_FIO, '')   AS organizer_FIO,
        COUNT(p.student_cod)          AS participants_count
    FROM Eventt e
    LEFT JOIN Organizer     o ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p ON p.event_cod     = e.event_cod
    GROUP BY
        e.event_cod, e.event_name, e.date_e, e.time_e,
        e.place, e.organizer_cod, e.type_e, e.image_path,
        e.max_participants, o.organizer_FIO
    ORDER BY e.date_e DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Search
    @SearchText VARCHAR(100)
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
        ISNULL(o.organizer_FIO, '')   AS organizer_FIO,
        COUNT(p.student_cod)          AS participants_count
    FROM Eventt e
    LEFT JOIN Organizer     o ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p ON p.event_cod     = e.event_cod
    WHERE e.event_name LIKE '%' + @SearchText + '%'
       OR e.place      LIKE '%' + @SearchText + '%'
       OR e.type_e     LIKE '%' + @SearchText + '%'
    GROUP BY
        e.event_cod, e.event_name, e.date_e, e.time_e,
        e.place, e.organizer_cod, e.type_e, e.image_path,
        e.max_participants, o.organizer_FIO
    ORDER BY e.date_e DESC;
END
GO

PRINT 'sp_Event_GetAll и sp_Event_Search обновлены успешно!';
GO

select * from Eventt


EXEC sp_helptext 'dbo.vw_EventsWithDetails';
EXEC sp_helptext 'dbo.vw_ParticipantsWithStats';

  
-- Представление 1: мероприятия с организатором и числом участников  
CREATE   VIEW vw_EventsWithDetails AS  
    SELECT  
        e.event_cod,  
        e.event_name,  
        e.date_e,  
        e.time_e,  
        e.place,  
        e.type_e,  
        e.image_path,  
        e.max_participants,  
        e.organizer_cod,  
        ISNULL(o.organizer_FIO, '') AS organizer_FIO,  
        COUNT(p.student_cod)        AS participants_count  
    FROM Eventt e  
    LEFT JOIN Organizer     o ON o.organizer_cod = e.organizer_cod  
    LEFT JOIN Participation p ON p.event_cod     = e.event_cod  
    GROUP BY  
        e.event_cod, e.event_name, e.date_e, e.time_e,  
        e.place, e.type_e, e.image_path,  
        e.max_participants, e.organizer_cod, o.organizer_FIO;  

GO

select * from vw_EventsWithDetails

  
-- Представление 2: участники с количеством посещённых мероприятий  
CREATE   VIEW vw_ParticipantsWithStats AS  
    SELECT  
        s.student_cod,  
        s.student_FIO,  
        s.class,  
        s.phone_number_s,  
        s.email_s,  
        COUNT(p.event_cod) AS events_count  
    FROM Participant s  
    LEFT JOIN Participation p ON p.student_cod = s.student_cod  
    GROUP BY  
        s.student_cod, s.student_FIO, s.class,  
        s.phone_number_s, s.email_s;  
GO

select * from vw_ParticipantsWithStats


SELECT name
FROM sys.procedures
WHERE name NOT LIKE 'sp_%diagram%'
ORDER BY name;

SELECT 
    name,
    OBJECT_DEFINITION(object_id) AS procedure_code
FROM sys.procedures
WHERE name NOT LIKE 'sp_%diagram%'
ORDER BY name;



