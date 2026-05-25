-- =====================================================
-- Миграция: процедуры для организатора
-- Запустить один раз в базе данных School_event
-- =====================================================

USE School_event;
GO


SELECT organizer_cod, organizer_FIO, job_name FROM Organizer

-- Посмотреть у кого пустая должность
SELECT organizer_cod, organizer_FIO, job_name 
FROM Organizer 
WHERE job_name IS NULL OR job_name = ''
ORDER BY organizer_cod

EXEC sp_helptext 'sp_Organizer_GetAll'


ALTER PROCEDURE sp_Organizer_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT organizer_cod, organizer_FIO, job_name, phone_number_o, email_0
    FROM Organizer
    ORDER BY organizer_FIO
END





-- 1. Получить все мероприятия конкретного организатора по ФИО
CREATE OR ALTER PROCEDURE sp_Event_GetByOrganizerFIO
    @OrganizerFIO VARCHAR(100)
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
        e.image_path
    FROM Eventt e
    INNER JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    WHERE o.organizer_FIO = @OrganizerFIO
    ORDER BY e.date_e DESC;
END
GO

-- 2. Поиск мероприятий организатора по тексту
CREATE OR ALTER PROCEDURE sp_Event_SearchByOrganizerFIO
    @OrganizerFIO VARCHAR(100),
    @SearchText   VARCHAR(100)
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
        e.image_path
    FROM Eventt e
    INNER JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    WHERE o.organizer_FIO = @OrganizerFIO
      AND (
            e.event_name LIKE '%' + @SearchText + '%'
         OR e.place      LIKE '%' + @SearchText + '%'
         OR e.type_e     LIKE '%' + @SearchText + '%'
          )
    ORDER BY e.date_e DESC;
END
GO

PRINT 'Процедуры sp_Event_GetByOrganizerFIO и sp_Event_SearchByOrganizerFIO созданы.';
GO
