-- =============================================
-- БАЗА ДАННЫХ: School_event
-- АВТОР: Рунтова Мария
-- ОПИСАНИЕ: Учёт проведения школьных мероприятий
-- =============================================

USE master;
GO

-- Удаляем БД, если существует
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'School_event')
BEGIN
    ALTER DATABASE School_event SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE School_event;
END
GO

-- Создаём БД
CREATE DATABASE School_event;
GO

USE School_event;
GO

-- =============================================
-- 1. ТАБЛИЦЫ С IDENTITY (авто-генерация кодов)
-- =============================================

-- Организаторы
CREATE TABLE Organizer (
    organizer_cod INT IDENTITY(10,1) PRIMARY KEY,
    organizer_FIO NVARCHAR(100) NOT NULL,
    job_name NVARCHAR(50) NOT NULL,
    email_0 NVARCHAR(100) NOT NULL UNIQUE,
    phone_number_o NVARCHAR(20) NOT NULL
);
GO

-- Участники
CREATE TABLE Participant (
    student_cod INT IDENTITY(201,1) PRIMARY KEY,
    student_FIO NVARCHAR(100) NOT NULL,
    class NVARCHAR(10) NOT NULL,
    email_s NVARCHAR(100) NOT NULL UNIQUE,
    phone_number_s NVARCHAR(20) NOT NULL
);
GO

-- Мероприятия
CREATE TABLE Eventt (
    event_cod INT IDENTITY(2001,1) PRIMARY KEY,
    event_name NVARCHAR(100) NOT NULL,
    date_e DATE NOT NULL,
    time_e TIME NOT NULL,
    place NVARCHAR(100) NOT NULL,
    organizer_cod INT NOT NULL FOREIGN KEY REFERENCES Organizer(organizer_cod),
    type_e NVARCHAR(50) NOT NULL,
    max_participants INT NULL,
    image_path NVARCHAR(500) NULL
);
GO

-- Участие (связь многие-ко-многим)
CREATE TABLE Participation (
    event_cod INT NOT NULL FOREIGN KEY REFERENCES Eventt(event_cod) ON DELETE CASCADE,
    student_cod INT NOT NULL FOREIGN KEY REFERENCES Participant(student_cod) ON DELETE CASCADE,
    registration_date DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (event_cod, student_cod)
);
GO

-- Пользователи (авторизация)
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(200) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    role NVARCHAR(20) NOT NULL DEFAULT 'user',
    email NVARCHAR(100) NULL,
    student_cod INT NULL FOREIGN KEY REFERENCES Participant(student_cod),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- 2. ЗАПОЛНЕНИЕ ДАННЫМИ
-- =============================================

-- 2.1 Организаторы (коды 10-52)
SET IDENTITY_INSERT Organizer ON;
GO
INSERT INTO Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o) VALUES
(10, 'Рунтова Антонина Федоровна', 'завуч', 'runtovaantonina@gmail.com', '079236964'),
(11, 'Гавришко Светлана Федоровна', 'учитель математики', 'gavrishkosvetlana@gmail.com', '078954698'),
(12, 'Марченко Лариса Петровна', 'учитель истории', 'marcenkolarisa@gmail.com', '077652398'),
(13, 'Реуцкая Наталья Ивановна', 'учитель мл.классов', 'reutcaianatalia@gmail.com', '072097431'),
(14, 'Попов Мария Константиновна', 'учитель русского яз.', 'popovmaria@gmail.com', '072398134'),
(15, 'Вершина Таисия Федоровна', 'учитель физ.восп', 'vershinataisia@gmail.com', '072872309'),
(16, 'Посмаг Адриана Пантелеевна', 'учитель техн.восп', 'posmagadriana@gmail.com', '075930971'),
(17, 'Чулина Надежда Федосеевна', 'учитель франц. яз.', 'ciulinanadejda@gmail.com', '071209654'),
(18, 'Куля Марчелла Дмитриевна', 'директор', 'culeamarcela@gmail.com', '071209456'),
(19, 'Шаган Кристина Гергевна', 'учитель информатики', 'shagankristina@gmail.com', '070923674'),
(20, 'Иванова Марина Сергеевна', 'учитель биологии', 'ivanova@gmail.com', '071111111'),
(21, 'Петров Алексей Иванович', 'учитель химии', 'petrov@gmail.com', '072222222'),
(22, 'Сидорова Анна Викторовна', 'учитель географии', 'sidorova@gmail.com', '073333333'),
(23, 'Козлов Дмитрий Олегович', 'учитель физики', 'kozlov@gmail.com', '074444444'),
(24, 'Николаева Ольга Петровна', 'учитель музыки', 'nikolaeva@gmail.com', '075555555'),
(25, 'Федоров Максим Игоревич', 'учитель труда', 'fedorov@gmail.com', '076666666'),
(26, 'Орлова Елена Сергеевна', 'психолог', 'orlova@gmail.com', '077777777'),
(27, 'Морозов Артем Андреевич', 'соц.педагог', 'morozov@gmail.com', '078888888'),
(28, 'Васильева Дарья Павловна', 'логопед', 'vasilieva@gmail.com', '079999999'),
(29, 'Громов Илья Сергеевич', 'учитель истории', 'gromov@gmail.com', '070111111'),
(30, 'Захарова Юлия Викторовна', 'учитель англ.яз', 'zah@gmail.com', '071222222'),
(31, 'Беляев Никита Олегович', 'учитель физры', 'bel@gmail.com', '071333333'),
(32, 'Тарасова Ирина Николаевна', 'учитель рус.яз', 'tar@gmail.com', '071444444'),
(33, 'Комаров Степан Ильич', 'учитель математики', 'kom@gmail.com', '071555555'),
(34, 'Лебедева Мария Олеговна', 'учитель нач.классов', 'leb@gmail.com', '071666666'),
(35, 'Семенов Кирилл Андреевич', 'учитель информатики', 'sem@gmail.com', '071777777'),
(36, 'Павлова Оксана Юрьевна', 'учитель географии', 'pav@gmail.com', '071888888'),
(37, 'Егоров Роман Викторович', 'учитель истории', 'ego@gmail.com', '071999999'),
(38, 'Крылова Алина Сергеевна', 'учитель биологии', 'kry@gmail.com', '072111111'),
(39, 'Соловьев Иван Андреевич', 'учитель физики', 'sol@gmail.com', '072222223'),
(40, 'Матвеева Татьяна Петровна', 'директор', 'mat@gmail.com', '072333333'),
(41, 'Андреев Павел Олегович', 'завуч', 'and@gmail.com', '072444444'),
(42, 'Гусева Виктория Сергеевна', 'учитель музыки', 'gus@gmail.com', '072555555'),
(43, 'Романов Артем Сергеевич', 'учитель труда', 'rom@gmail.com', '072666666'),
(44, 'Киселева Дарья Андреевна', 'учитель химии', 'kis@gmail.com', '072777777'),
(45, 'Чернов Максим Игоревич', 'учитель физры', 'che@gmail.com', '072888888'),
(46, 'Ларионова Елена Викторовна', 'психолог', 'lar@gmail.com', '072999999'),
(47, 'Зотов Алексей Андреевич', 'учитель информатики', 'zot@gmail.com', '073111111'),
(48, 'Калинина Ольга Сергеевна', 'учитель англ.яз', 'kal@gmail.com', '073222222'),
(49, 'Нестеров Иван Олегович', 'учитель математики', 'nes@gmail.com', '073333333'),
(50, 'Дюндик Снежана Ивановна', 'учитель рус.яз', 'dyundik@gmail.com', '078236865'),
(51, 'Меше Татьяна Ивановна', 'учитель географии', 'meshe@gmail.com', '079236765'),
(52, 'Тестовый Организатор', 'тест', 'test@test.com', '071234567');
SET IDENTITY_INSERT Organizer OFF;
GO

-- 2.2 Участники (коды 201-210)
SET IDENTITY_INSERT Participant ON;
GO
INSERT INTO Participant (student_cod, student_FIO, class, email_s, phone_number_s) VALUES
(201, 'Мунтяну Денис', '8Б', 'munteanudenis@gmail.com', '072323674'),
(202, 'Рябикин Валентина', '9Б', 'reabikinvalea@gmail.com', '072321298'),
(203, 'Рунтов Дмитрий', '7Б', 'dimaruntov@gmail.com', '072367574'),
(204, 'Бурлаку Дина', '9Б', 'burlacudina@gmail.com', '062098674'),
(205, 'Адвахова Алина', '9Б', 'advahovaalina@gmail.com', '072322398'),
(206, 'Геоня Михаил', '8Б', 'mishagheonea@gmail.com', '06743674'),
(207, 'Яким Олег', '8Б', 'olegiakim@gmail.com', '072323237'),
(208, 'Бойко Анатолий', '9Б', 'boicoanatolii@gmail.com', '072343674'),
(209, 'Кармалак Полина', '7Б', 'carmalacpolina@gmail.com', '073423674'),
(210, 'Обрежа Дамиана', '7Б', 'obrejadamiana@gmail.com', '072209674');
SET IDENTITY_INSERT Participant OFF;
GO

-- 2.3 Мероприятия (коды 2001-2015)
SET IDENTITY_INSERT Eventt ON;
GO
INSERT INTO Eventt (event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, max_participants) VALUES
(2001, 'День борьбы со спидом', '2024-12-12', '13:10:00', 'актовый зал', 10, 'образовательное', NULL),
(2002, 'Эрудит', '2024-12-15', '15:10:00', 'актовый зал', 11, 'образовательное', NULL),
(2003, 'Личности в истории', '2024-12-20', '14:30:00', 'кабинет 113', 12, 'образовательное', NULL),
(2004, 'Новый год!', '2024-12-23', '10:30:00', 'актовый зал', 13, 'культурное', NULL),
(2005, 'Литературный вечер', '2025-01-17', '14:10:00', 'актовый зал', 14, 'образовательное', NULL),
(2006, 'Веселые старты', '2025-01-28', '13:30:00', 'спорт зал', 15, 'спортивное', NULL),
(2007, 'Технический марафон', '2025-02-12', '14:10:00', 'кабинет 100', 16, 'образовательное', NULL),
(2008, 'День в Париже', '2025-02-14', '14:30:00', 'актовый зал', 17, 'культурное', NULL),
(2009, 'Минута Славы', '2025-03-14', '12:30:00', 'актовый зал', 18, 'культурное', NULL),
(2010, 'Инфознайка', '2025-03-28', '14:30:00', 'кабинет 205', 19, 'образовательное', NULL),
(2011, 'Пасхальный фестиваль', '2025-04-15', '12:10:00', 'актовый зал', 10, 'культурное', NULL),
(2012, 'Математический бой', '2025-04-20', '13:00:00', 'каб.101', 20, 'образовательное', NULL),
(2013, 'Физический квест', '2025-04-22', '14:00:00', 'каб.102', 21, 'образовательное', NULL),
(2014, 'Географическая викторина', '2025-04-25', '12:00:00', 'каб.103', 22, 'образовательное', NULL),
(2015, 'Научное шоу', '2025-04-27', '15:00:00', 'актовый зал', 23, 'образовательное', NULL);
SET IDENTITY_INSERT Eventt OFF;
GO

-- 2.4 Участие
INSERT INTO Participation (event_cod, student_cod) VALUES
(2001, 201), (2002, 202), (2003, 203), (2004, 204), (2005, 205),
(2006, 206), (2007, 207), (2008, 208), (2009, 209), (2010, 210),
(2011, 201), (2011, 202);
GO

-- 2.5 Пользователи (пароль admin123)
INSERT INTO Users (username, password_hash, full_name, role, email, student_cod) VALUES
('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Администратор', 'admin', NULL, NULL),
('user', 'b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05a8a7', 'Обычный пользователь', 'user', NULL, NULL),
('runtova', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Рунтова Антонина Федоровна', 'organizer', 'runtovaantonina@gmail.com', NULL),
('denis', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Мунтяну Денис', 'user', 'munteanudenis@gmail.com', 201);
GO

-- =============================================
-- 3. ИНДЕКСЫ
-- =============================================
CREATE INDEX idx_event_date ON Eventt(date_e);
CREATE INDEX idx_users_username ON Users(username);
GO

-- =============================================
-- 4. ПРЕДСТАВЛЕНИЯ (VIEW)
-- =============================================

CREATE OR ALTER VIEW vw_EventsWithDetails AS
SELECT 
    e.event_cod, e.event_name, e.date_e, e.time_e, e.place, e.type_e, 
    e.image_path, e.max_participants, e.organizer_cod,
    ISNULL(o.organizer_FIO, '') AS organizer_FIO,
    COUNT(p.student_cod) AS participants_count
FROM Eventt e
LEFT JOIN Organizer o ON o.organizer_cod = e.organizer_cod
LEFT JOIN Participation p ON p.event_cod = e.event_cod
GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e, e.place, e.type_e, 
         e.image_path, e.max_participants, e.organizer_cod, o.organizer_FIO;
GO

CREATE OR ALTER VIEW vw_ParticipantsWithStats AS
SELECT 
    s.student_cod, s.student_FIO, s.class, s.phone_number_s, s.email_s,
    COUNT(p.event_cod) AS events_count
FROM Participant s
LEFT JOIN Participation p ON p.student_cod = s.student_cod
GROUP BY s.student_cod, s.student_FIO, s.class, s.phone_number_s, s.email_s;
GO

-- =============================================
-- 5. ХРАНИМЫЕ ПРОЦЕДУРЫ
-- =============================================

-- 5.1 Организаторы
CREATE OR ALTER PROCEDURE sp_Organizer_GetAll AS
BEGIN SET NOCOUNT ON;
    SELECT organizer_cod, organizer_FIO, job_name, email_0, phone_number_o 
    FROM Organizer ORDER BY organizer_FIO;
END
GO

CREATE OR ALTER PROCEDURE sp_Organizer_Insert
    @organizer_FIO NVARCHAR(100), @job_name NVARCHAR(50), 
    @email_0 NVARCHAR(100), @phone_number_o NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Organizer (organizer_FIO, job_name, email_0, phone_number_o)
    VALUES (@organizer_FIO, @job_name, @email_0, @phone_number_o);
    SELECT SCOPE_IDENTITY() AS organizer_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Organizer_Update
    @organizer_cod INT, @organizer_FIO NVARCHAR(100), @job_name NVARCHAR(50),
    @email_0 NVARCHAR(100), @phone_number_o NVARCHAR(20)
AS
BEGIN
    UPDATE Organizer SET organizer_FIO = @organizer_FIO, job_name = @job_name,
        email_0 = @email_0, phone_number_o = @phone_number_o
    WHERE organizer_cod = @organizer_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Organizer_Delete @organizer_cod INT AS
BEGIN DELETE FROM Organizer WHERE organizer_cod = @organizer_cod; END
GO

-- 5.2 Участники
CREATE OR ALTER PROCEDURE sp_Participant_GetAll AS
BEGIN SET NOCOUNT ON;
    SELECT student_cod, student_FIO, class, email_s, phone_number_s 
    FROM Participant ORDER BY student_FIO;
END
GO

CREATE OR ALTER PROCEDURE sp_Participant_Insert
    @student_FIO NVARCHAR(100), @class NVARCHAR(10), @email_s NVARCHAR(100), @phone_number_s NVARCHAR(20)
AS
BEGIN
    INSERT INTO Participant (student_FIO, class, email_s, phone_number_s)
    VALUES (@student_FIO, @class, @email_s, @phone_number_s);
    SELECT SCOPE_IDENTITY() AS student_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Participant_Update
    @student_cod INT, @student_FIO NVARCHAR(100), @class NVARCHAR(10),
    @email_s NVARCHAR(100), @phone_number_s NVARCHAR(20)
AS
BEGIN
    UPDATE Participant SET student_FIO = @student_FIO, class = @class,
        email_s = @email_s, phone_number_s = @phone_number_s
    WHERE student_cod = @student_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Participant_Delete @student_cod INT AS
BEGIN DELETE FROM Participant WHERE student_cod = @student_cod; END
GO

-- 5.3 Мероприятия
CREATE OR ALTER PROCEDURE sp_Event_GetAll AS
BEGIN
    SELECT e.event_cod, e.event_name, e.date_e, e.time_e, e.place, 
           e.organizer_cod, e.type_e, e.image_path, e.max_participants,
           ISNULL(o.organizer_FIO, '') AS organizer_FIO,
           COUNT(p.student_cod) AS participants_count
    FROM Eventt e
    LEFT JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p ON p.event_cod = e.event_cod
    GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e, e.place, 
             e.organizer_cod, e.type_e, e.image_path, e.max_participants, o.organizer_FIO
    ORDER BY e.date_e DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Insert
    @event_name NVARCHAR(100), @date_e DATE, @time_e TIME, @place NVARCHAR(100),
    @organizer_cod INT, @type_e NVARCHAR(50), @max_participants INT = NULL, @image_path NVARCHAR(500) = NULL
AS
BEGIN
    INSERT INTO Eventt (event_name, date_e, time_e, place, organizer_cod, type_e, max_participants, image_path)
    VALUES (@event_name, @date_e, @time_e, @place, @organizer_cod, @type_e, @max_participants, @image_path);
    SELECT SCOPE_IDENTITY() AS event_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Update
    @event_cod INT, @event_name NVARCHAR(100), @date_e DATE, @time_e TIME, @place NVARCHAR(100),
    @organizer_cod INT, @type_e NVARCHAR(50), @max_participants INT = NULL, @image_path NVARCHAR(500) = NULL
AS
BEGIN
    UPDATE Eventt SET event_name = @event_name, date_e = @date_e, time_e = @time_e, place = @place,
        organizer_cod = @organizer_cod, type_e = @type_e, max_participants = @max_participants,
        image_path = CASE WHEN @image_path IS NULL THEN image_path ELSE @image_path END
    WHERE event_cod = @event_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Delete @event_cod INT AS
BEGIN DELETE FROM Participation WHERE event_cod = @event_cod; DELETE FROM Eventt WHERE event_cod = @event_cod; END
GO

-- 5.4 Участие
CREATE OR ALTER PROCEDURE sp_Participation_Insert @event_cod INT, @student_cod INT AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Eventt WHERE event_cod = @event_cod)
        THROW 50020, 'Мероприятие не существует.', 1;
    IF EXISTS (SELECT 1 FROM Participation WHERE event_cod = @event_cod AND student_cod = @student_cod)
        THROW 50021, 'Участник уже записан.', 1;
    INSERT INTO Participation (event_cod, student_cod) VALUES (@event_cod, @student_cod);
END
GO

CREATE OR ALTER PROCEDURE sp_Participation_Delete @event_cod INT, @student_cod INT AS
BEGIN DELETE FROM Participation WHERE event_cod = @event_cod AND student_cod = @student_cod; END
GO

CREATE OR ALTER PROCEDURE sp_Participation_GetAll AS
BEGIN
    SELECT p.event_cod, p.student_cod, e.event_name, e.date_e, e.time_e, e.place, e.type_e,
           s.student_FIO, s.class, s.email_s, s.phone_number_s
    FROM Participation p
    JOIN Eventt e ON e.event_cod = p.event_cod
    JOIN Participant s ON s.student_cod = p.student_cod
    ORDER BY e.date_e DESC;
END
GO

-- 5.5 Авторизация
CREATE OR ALTER PROCEDURE sp_User_GetHash @username NVARCHAR(50) AS
BEGIN SELECT password_hash FROM Users WHERE username = @username; END
GO

CREATE OR ALTER PROCEDURE sp_User_Login @username NVARCHAR(50) AS
BEGIN SELECT username, full_name, role, student_cod FROM Users WHERE username = @username; END
GO

CREATE OR ALTER PROCEDURE sp_User_Register
    @username NVARCHAR(50), @password_hash NVARCHAR(200), @full_name NVARCHAR(100), @email NVARCHAR(100) = NULL
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
        THROW 50000, 'Пользователь уже существует', 1;
    DECLARE @role NVARCHAR(20) = 'user', @student_cod INT = NULL;
    IF @email IS NOT NULL AND EXISTS (SELECT 1 FROM Organizer WHERE email_0 = @email) SET @role = 'organizer';
    IF @email IS NOT NULL SELECT @student_cod = student_cod FROM Participant WHERE email_s = @email;
    INSERT INTO Users (username, password_hash, full_name, role, email, student_cod)
    VALUES (@username, @password_hash, @full_name, @role, @email, @student_cod);
    SELECT @role AS role, @student_cod AS student_cod;
END
GO

-- 5.6 Отчёты
CREATE OR ALTER PROCEDURE sp_Report_AllEvents @DateFrom DATE = NULL, @DateTo DATE = NULL AS
BEGIN
    SELECT ROW_NUMBER() OVER (ORDER BY e.date_e, e.event_cod) AS Nomer,
           e.event_name AS Nazvanie, CONVERT(VARCHAR, e.date_e, 104) AS Data,
           LEFT(CONVERT(VARCHAR, e.time_e, 108), 5) AS Vremya, e.place AS Mesto,
           e.type_e AS Tip, ISNULL(o.organizer_FIO, '') AS Organizator,
           COUNT(p.student_cod) AS KolUchastnikov
    FROM Eventt e
    LEFT JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    LEFT JOIN Participation p ON p.event_cod = e.event_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom) AND (@DateTo IS NULL OR e.date_e <= @DateTo)
    GROUP BY e.event_cod, e.event_name, e.date_e, e.time_e, e.place, e.type_e, o.organizer_FIO
    ORDER BY e.date_e, e.event_cod;
END
GO

CREATE OR ALTER PROCEDURE sp_Report_EventParticipants 
    @DateFrom DATE = NULL, @DateTo DATE = NULL, @TypeFilter NVARCHAR(50) = NULL AS
BEGIN
    SELECT e.event_name AS Meropriyatie, CONVERT(VARCHAR, e.date_e, 104) AS Data,
           e.type_e AS Tip, e.place AS Mesto, ISNULL(o.organizer_FIO, '') AS Organizator,
           s.student_FIO AS Uchastnik, s.class AS Klass, ISNULL(s.phone_number_s, '') AS Telefon
    FROM Participation p
    JOIN Eventt e ON e.event_cod = p.event_cod
    JOIN Participant s ON s.student_cod = p.student_cod
    LEFT JOIN Organizer o ON o.organizer_cod = e.organizer_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom) AND (@DateTo IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR @TypeFilter = '' OR e.type_e = @TypeFilter)
    ORDER BY e.date_e DESC, e.event_name, s.student_FIO;
END
GO

CREATE OR ALTER PROCEDURE sp_Report_ParticipantActivity @DateFrom DATE = NULL, @DateTo DATE = NULL AS
BEGIN
    SELECT ROW_NUMBER() OVER (ORDER BY COUNT(p.event_cod) DESC) AS Mesto,
           s.student_FIO AS Uchastnik, s.class AS Klass, ISNULL(s.email_s, '') AS Email,
           COUNT(p.event_cod) AS KolMeropriyatiy,
           ISNULL(CONVERT(VARCHAR, MIN(e.date_e), 104), '—') AS PervoeUchastie,
           ISNULL(CONVERT(VARCHAR, MAX(e.date_e), 104), '—') AS PosledneeUchastie
    FROM Participant s
    LEFT JOIN Participation p ON p.student_cod = s.student_cod
    LEFT JOIN Eventt e ON e.event_cod = p.event_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom) AND (@DateTo IS NULL OR e.date_e <= @DateTo)
    GROUP BY s.student_cod, s.student_FIO, s.class, s.email_s
    ORDER BY KolMeropriyatiy DESC;
END
GO

-- 5.7 Дашборд
CREATE OR ALTER PROCEDURE sp_Dashboard_GetStats @DateFrom DATE = NULL, @DateTo DATE = NULL, @TypeFilter NVARCHAR(50) = NULL AS
BEGIN
    SELECT COUNT(DISTINCT e.event_cod) AS TotalEvents, COUNT(p.student_cod) AS TotalParticipants,
           MIN(e.date_e) AS EarliestDate, MAX(e.date_e) AS LatestDate
    FROM Eventt e
    LEFT JOIN Participation p ON p.event_cod = e.event_cod
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom) AND (@DateTo IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e = @TypeFilter);
END
GO

CREATE OR ALTER PROCEDURE sp_Dashboard_ByType @DateFrom DATE = NULL, @DateTo DATE = NULL, @TypeFilter NVARCHAR(50) = NULL AS
BEGIN
    SELECT e.type_e AS EventType, COUNT(*) AS EventCount
    FROM Eventt e
    WHERE (@DateFrom IS NULL OR e.date_e >= @DateFrom) AND (@DateTo IS NULL OR e.date_e <= @DateTo)
      AND (@TypeFilter IS NULL OR e.type_e = @TypeFilter)
    GROUP BY e.type_e ORDER BY EventCount DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Dashboard_GetTypes AS
BEGIN SELECT DISTINCT type_e FROM Eventt WHERE type_e IS NOT NULL ORDER BY type_e; END
GO

-- =============================================
-- 6. ВЫВОД ИНФОРМАЦИИ
-- =============================================
PRINT '============================================';
PRINT 'БАЗА ДАННЫХ School_event УСПЕШНО СОЗДАНА!';
PRINT '============================================';
PRINT 'Организаторов: ' + CAST((SELECT COUNT(*) FROM Organizer) AS VARCHAR);
PRINT 'Участников: ' + CAST((SELECT COUNT(*) FROM Participant) AS VARCHAR);
PRINT 'Мероприятий: ' + CAST((SELECT COUNT(*) FROM Eventt) AS VARCHAR);
PRINT 'Записей участия: ' + CAST((SELECT COUNT(*) FROM Participation) AS VARCHAR);
PRINT 'Пользователей: ' + CAST((SELECT COUNT(*) FROM Users) AS VARCHAR);
PRINT '============================================';
PRINT 'Данные для входа:';
PRINT '  admin / admin123 (администратор)';
PRINT '  user / user123 (пользователь)';
PRINT '  runtova / admin123 (организатор)';
PRINT '  denis / admin123 (участник)';
PRINT '============================================';