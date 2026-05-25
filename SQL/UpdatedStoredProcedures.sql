-- =============================================
-- Обновлённые процедуры аутентификации
-- Запусти в School_event
-- =============================================

USE School_event;
GO

-- Возвращает сохранённый хеш пароля для проверки на стороне клиента.
-- Клиент сам сравнивает PBKDF2 или SHA-256 — в базу пароль не передаётся.
CREATE OR ALTER PROCEDURE sp_User_GetHash
    @username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT password_hash
    FROM   Users
    WHERE  username = @username;
END
GO

-- Возвращает данные пользователя после успешной проверки пароля клиентом.
-- Хеш сюда НЕ передаётся — только username.
CREATE OR ALTER PROCEDURE sp_User_Login
    @username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT username, full_name, role
    FROM   Users
    WHERE  username = @username;
END
GO

-- Регистрация — принимает уже готовый хеш от клиента.
-- Логика определения роли остаётся в процедуре (по email организатора).
CREATE OR ALTER PROCEDURE sp_User_Register
    @username      VARCHAR(50),
    @password_hash VARCHAR(200),   -- теперь до 200 символов (PBKDF2 длиннее SHA-256)
    @full_name     VARCHAR(100),
    @email         VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверяем что username не занят
    IF EXISTS (SELECT 1 FROM Users WHERE username = @username)
    BEGIN
        RAISERROR('Пользователь с таким именем уже существует.', 16, 1);
        RETURN;
    END

    DECLARE @role VARCHAR(20) = 'user';

    -- Если email совпадает с email организатора — назначаем роль organizer
    IF @email IS NOT NULL AND EXISTS (
        SELECT 1 FROM Organizer WHERE email_0 = @email
    )
        SET @role = 'organizer';

    INSERT INTO Users (username, password_hash, full_name, role, email)
    VALUES (@username, @password_hash, @full_name, @role, @email);

    SELECT @role AS role;
END
GO

-- =============================================
-- ВАЖНО: если колонка password_hash в таблице Users
-- имеет тип VARCHAR(64) — расширь её:
--
--   ALTER TABLE Users ALTER COLUMN password_hash VARCHAR(200) NOT NULL;
--
-- Старые SHA-256 хеши (64 символа) остаются совместимы —
-- приложение проверяет длину и использует нужный алгоритм.
-- =============================================
