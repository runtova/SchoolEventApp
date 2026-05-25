-- =============================================
-- АВТОРИЗАЦИЯ — выполнить в SQL Server
-- =============================================

USE School_event
GO

-- Таблица пользователей
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        user_id       INT IDENTITY(1,1) PRIMARY KEY,
        username      VARCHAR(50)  NOT NULL UNIQUE,
        password_hash VARCHAR(256) NOT NULL,
        full_name     VARCHAR(100) NOT NULL,
        role          VARCHAR(20)  NOT NULL DEFAULT 'user',
        created_at    DATETIME     DEFAULT GETDATE()
    )
END
GO

-- Тестовые пользователи
-- admin / admin123
IF NOT EXISTS (SELECT 1 FROM Users WHERE username = 'admin')
    INSERT INTO Users (username, password_hash, full_name, role)
    VALUES ('admin',
            '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
            'Администратор', 'admin')
GO
-- user / user123
IF NOT EXISTS (SELECT 1 FROM Users WHERE username = 'user')
    INSERT INTO Users (username, password_hash, full_name, role)
    VALUES ('user',
            'b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05a8a7',
            'Пользователь', 'user')
GO

-- ХП: Вход
CREATE OR ALTER PROCEDURE sp_User_Login
    @username      VARCHAR(50),
    @password_hash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT user_id, username, full_name, role
    FROM Users
    WHERE username = @username AND password_hash = @password_hash
END
GO

-- ХП: Регистрация
CREATE OR ALTER PROCEDURE sp_User_Register
    @username      VARCHAR(50),
    @password_hash VARCHAR(256),
    @full_name     VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
            THROW 50010, 'Пользователь с таким именем уже существует.', 1;
        INSERT INTO Users (username, password_hash, full_name, role)
        VALUES (@username, @password_hash, @full_name, 'user');
        COMMIT TRANSACTION;
        SELECT user_id, username, full_name, role
        FROM Users WHERE username = @username;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

select * from Organizer


CREATE OR ALTER PROCEDURE sp_User_Register
    @username      NVARCHAR(30),
    @password_hash NVARCHAR(256),
    @full_name     NVARCHAR(100),
    @email         NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка: логин уже занят?
    IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
    BEGIN
        RAISERROR('Пользователь с таким именем уже существует.', 16, 1);
        RETURN;
    END

    -- Определяем роль: если email совпадает с организатором — organizer, иначе user
    DECLARE @role NVARCHAR(20) = 'user';

    IF @email IS NOT NULL AND EXISTS (
        SELECT 1 FROM Organizers WHERE email = @email  -- или своя таблица
    )
        SET @role = 'organizer';

    -- Создаём пользователя
    INSERT INTO Users (username, password_hash, full_name, email, role)
    VALUES (@username, @password_hash, @full_name, @email, @role);

    -- Возвращаем роль (C# её читает)
    SELECT @role AS role;
END

-- Найти все таблицы в БД
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'

SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Organizer'


CREATE OR ALTER PROCEDURE sp_User_Register
    @username      NVARCHAR(30),
    @password_hash NVARCHAR(256),
    @full_name     NVARCHAR(100),
    @email         NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
    BEGIN
        RAISERROR('Пользователь с таким именем уже существует.', 16, 1);
        RETURN;
    END

    DECLARE @role NVARCHAR(20) = 'user';

    IF @email IS NOT NULL AND EXISTS (
        SELECT 1 FROM Organizer WHERE email_0 = @email  -- ✅ правильное имя
    )
        SET @role = 'organizer';

    INSERT INTO Users (username, password_hash, full_name, email, role)
    VALUES (@username, @password_hash, @full_name, @email, @role);

    SELECT @role AS role;
END

select * from Participant


SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'


CREATE OR ALTER PROCEDURE sp_User_Register
    @username      NVARCHAR(30),
    @password_hash NVARCHAR(256),
    @full_name     NVARCHAR(100),
    @email         NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Логин уже занят?
    IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
    BEGIN
        RAISERROR('Пользователь с таким именем уже существует.', 16, 1);
        RETURN;
    END

    DECLARE @role NVARCHAR(20) = 'user';
    DECLARE @student_cod SMALLINT = NULL;

    -- Ищем участника по email в Participant
    IF @email IS NOT NULL
    BEGIN
        SELECT @student_cod = student_cod 
        FROM Participant 
        WHERE email_s = @email;  -- email_s — судя по колонкам из скрина
    END

    -- Ищем организатора по email
    IF @email IS NOT NULL AND EXISTS (
        SELECT 1 FROM Organizer WHERE email_0 = @email
    )
        SET @role = 'organizer';

    INSERT INTO Users (username, password_hash, full_name, email, role, student_cod)
    VALUES (@username, @password_hash, @full_name, @email, @role, @student_cod);

    SELECT @role AS role;
END


-- Что реально лежит в Users для этого пользователя
SELECT username, full_name, email, student_cod 
FROM Users 
WHERE full_name LIKE '%Krilov%' OR full_name LIKE '%Kryl%'

-- Что в Participant
SELECT student_cod, student_FIO, email_s 
FROM Participant 
WHERE student_FIO LIKE '%Крыл%'

EXEC sp_helptext 'sp_User_Login'


CREATE OR ALTER PROCEDURE sp_User_Login
    @username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT username, full_name, role, student_cod
    FROM   Users
    WHERE  username = @username;
END

SELECT name
FROM sys.procedures
WHERE name NOT LIKE 'sp_%diagram%'
ORDER BY name;

EXEC sp_helptext 'sp_Event_Update';



CREATE PROCEDURE sp_UpdateOrganizer
    @Cod   SMALLINT,
    @FIO   NVARCHAR(40),
    @Job   NVARCHAR(20),
    @Email NVARCHAR(30),
    @Phone NVARCHAR(9)
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка: организатор существует
    IF NOT EXISTS (SELECT 1 FROM Organizer WHERE organizer_cod = @Cod)
    BEGIN
        RAISERROR(N'Организатор с указанным кодом не найден.', 16, 1);
        RETURN;
    END

    -- Обновление
    UPDATE Organizer
    SET
        organizer_FIO  = @FIO,
        job_name       = @Job,
        email_0        = @Email,
        phone_number_o = @Phone
    WHERE
        organizer_cod  = @Cod;

END;

select * from Eventt


CREATE OR ALTER PROCEDURE sp_Event_Update
    @event_cod        SMALLINT,
    @event_name       NVARCHAR(30),
    @date_e           DATE,
    @time_e           TIME,
    @place            NVARCHAR(20),
    @organizer_cod    SMALLINT,
    @type_e           NVARCHAR(20),
    @max_participants INT           = NULL,
    @image_data       NVARCHAR(260) = NULL  -- NULL = не менять; '' = очистить
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Eventt
    SET
        event_name       = @event_name,
        date_e           = @date_e,
        time_e           = @time_e,
        place            = @place,
        organizer_cod    = @organizer_cod,
        type_e           = @type_e,
        max_participants = @max_participants,
        image_path       = CASE
                               WHEN @image_data IS NULL THEN image_path  -- не трогаем
                               WHEN @image_data = ''    THEN NULL        -- очищаем
                               ELSE @image_data                          -- новый путь
                           END
    WHERE event_cod = @event_cod;
END


SELECT event_cod, event_name, image_path
FROM Eventt;

UPDATE Eventt
SET image_path = NULL
WHERE image_path NOT LIKE '%.png'
  AND image_path NOT LIKE '%.jpg'
  AND image_path NOT LIKE '%.jpeg'
  AND image_path NOT LIKE '%.bmp';
