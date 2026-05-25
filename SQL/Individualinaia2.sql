create database School_event
go
use School_event
go

create type code from smallint
go
create type full_name from varchar(40)
go
create type email from varchar(30)
go
create type phone from varchar(9)
go

create table Organizer(organizer_cod code check(organizer_cod >9 and organizer_cod<150) primary key,
                        organizer_FIO full_name not null,
						job_name varchar(20) not null,
						email_0 email not null,
						phone_number_o phone not null
						)
go


create table Participant(student_cod code primary key,
                        student_FIO full_name not null,
						class varchar(3) not null,
						email_s email not null,
						phone_number_s phone not null
						)
go


create table Eventt(event_cod code primary key,
                    event_name varchar(30) not null,
					date_e date not null,
					time_e time not null,
					place varchar(20) not null,
					organizer_cod code check(organizer_cod >9 and organizer_cod<150) foreign key references Organizer(organizer_cod) not null,
					type_e varchar(20) not null
					)
go

create table Participation(event_cod code foreign key references Eventt(event_cod) not null,
                           student_cod code foreign key references Participant(student_cod) not null
						   )
go



Alter Authorization on database::School_event to SA

select * from Eventt

Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (10, 'Рунтова Антонина Федоровна', 'завуч','runtovaantonina@gmail.com', 079236964)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (11, 'Гавришко Светлана Федоровна', 'учитель математики','gavrishkosvetlana@gmail.com', 078954698)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (12, 'Марченко Лариса Петровна', 'учитель истории','marcenkolarisa@gmail.com', 077652398)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (13, 'Реуцкая Наталья Ивановна', 'учитель мл.классов','reutcaianatalia@gmail.com', 072097431)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (14, 'Попов Мария Константиновна', 'учитель русского яз.','popovmaria@gmail.com', 072398134)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (15, 'Вершина Таисия Федоровна', 'учитель физ.восп','vershinataisia@gmail.com', 072872309)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (16, 'Посмаг Адриана Пантелеевна', 'учитель техн.восп','posmagadriana@gmail.com', 075930971)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (17, 'Чулина Надежда Федосеевна', 'учитель франц. яз.','ciulinanadejda@gmail.com', 071209654)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (18, 'Куля Марчелла Дмитриевна', 'директор','culeamarcela@gmail.com', 071209456)
Insert into Organizer (organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
Values (19, 'Шаган Кристирна Гергевна', 'учитель информатики','shagankristina@gmail.com', 070923674)
SELECT * FROM Organizer;

INSERT INTO Organizer VALUES 
(20, 'Иванова Марина Сергеевна', 'учитель биологии','ivanova@gmail.com', 071111111),
(21, 'Петров Алексей Иванович', 'учитель химии','petrov@gmail.com', 072222222),
(22, 'Сидорова Анна Викторовна', 'учитель географии','sidorova@gmail.com', 073333333),
(23, 'Козлов Дмитрий Олегович', 'учитель физики','kozlov@gmail.com', 074444444),
(24, 'Николаева Ольга Петровна', 'учитель музыки','nikolaeva@gmail.com', 075555555),
(25, 'Федоров Максим Игоревич', 'учитель труда','fedorov@gmail.com', 076666666),
(26, 'Орлова Елена Сергеевна', 'психолог','orlova@gmail.com', 077777777),
(27, 'Морозов Артем Андреевич', 'соц.педагог','morozov@gmail.com', 078888888),
(28, 'Васильева Дарья Павловна', 'логопед','vasilieva@gmail.com', 079999999),
(29, 'Громов Илья Сергеевич', 'учитель истории','gromov@gmail.com', 070111111),

(30, 'Захарова Юлия Викторовна','учитель англ.яз','zah@gmail.com',071222222),
(31, 'Беляев Никита Олегович','учитель физры','bel@gmail.com',071333333),
(32, 'Тарасова Ирина Николаевна','учитель рус.яз','tar@gmail.com',071444444),
(33, 'Комаров Степан Ильич','учитель математики','kom@gmail.com',071555555),
(34, 'Лебедева Мария Олеговна','учитель нач.классов','leb@gmail.com',071666666),
(35, 'Семенов Кирилл Андреевич','учитель информатики','sem@gmail.com',071777777),
(36, 'Павлова Оксана Юрьевна','учитель географии','pav@gmail.com',071888888),
(37, 'Егоров Роман Викторович','учитель истории','ego@gmail.com',071999999),
(38, 'Крылова Алина Сергеевна','учитель биологии','kry@gmail.com',072111111),
(39, 'Соловьев Иван Андреевич','учитель физики','sol@gmail.com',072222223),

(40, 'Матвеева Татьяна Петровна','директор','mat@gmail.com',072333333),
(41, 'Андреев Павел Олегович','завуч','and@gmail.com',072444444),
(42, 'Гусева Виктория Сергеевна','учитель музыки','gus@gmail.com',072555555),
(43, 'Романов Артем Сергеевич','учитель труда','rom@gmail.com',072666666),
(44, 'Киселева Дарья Андреевна','учитель химии','kis@gmail.com',072777777),
(45, 'Чернов Максим Игоревич','учитель физры','che@gmail.com',072888888),
(46, 'Ларионова Елена Викторовна','психолог','lar@gmail.com',072999999),
(47, 'Зотов Алексей Андреевич','учитель информатики','zot@gmail.com',073111111),
(48, 'Калинина Ольга Сергеевна','учитель англ.яз','kal@gmail.com',073222222),
(49, 'Нестеров Иван Олегович','учитель математики','nes@gmail.com',073333333);

Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (201, 'Мунтяну Денис', '8Б','munteanudenis@gmail.com', 072323674)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (202, 'Рябикин Валентина', '9Б','reabikinvalea@gmail.com', 072321298)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (203, 'Рунтов Дмитрий', '7Б','dimaruntov@gmail.com', 072367574)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (204, 'Бурлаку Дина', '9Б','burlacudina@gmail.com', 062098674)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (205, 'Адвахова Алина', '9Б','advahovaalina@gmail.com', 072322398)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (206, 'Геоня Михаил', '8Б','mishagheonea@gmail.com', 06743674)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (207, 'Яким Олег', '8Б','olegiakim@gmail.com', 072323237)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (208, 'Бойко Анатолий', '9Б','boicoanatolii@gmail.com', 072343674)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (209, 'Кармалак Полина', '7Б','carmalacpolina@gmail.com', 073423674)
Insert into Participant (student_cod, student_FIO, class, email_s, phone_number_s)
Values (210, 'Обрежа Дамиана', '7Б','obrejadamiana@gmail.com', 072209674)
SELECT * FROM Participant;

INSERT INTO Participant VALUES
(211,'Иванов Павел','8А','p1@gmail.com',071111112),
(212,'Петрова Алина','9А','p2@gmail.com',071111113),
(213,'Сидоров Максим','7А','p3@gmail.com',071111114),
(214,'Кузнецова Мария','8Б','p4@gmail.com',071111115),
(215,'Орлов Никита','9Б','p5@gmail.com',071111116),
(216,'Фролова Анна','7Б','p6@gmail.com',071111117),
(217,'Зайцев Кирилл','8А','p7@gmail.com',071111118),
(218,'Смирнова Дарья','9А','p8@gmail.com',071111119),
(219,'Попов Артем','7А','p9@gmail.com',071111120),
(220,'Волкова Елена','8Б','p10@gmail.com',071111121),

(221,'Соколов Илья','9Б','p11@gmail.com',071111122),
(222,'Лебедев Роман','7Б','p12@gmail.com',071111123),
(223,'Козлова Виктория','8А','p13@gmail.com',071111124),
(224,'Морозова София','9А','p14@gmail.com',071111125),
(225,'Новиков Даниил','7А','p15@gmail.com',071111126),
(226,'Федотов Артем','8Б','p16@gmail.com',071111127),
(227,'Ершова Полина','9Б','p17@gmail.com',071111128),
(228,'Александров Кирилл','7Б','p18@gmail.com',071111129),
(229,'Тихонова Алина','8А','p19@gmail.com',071111130),
(230,'Белов Максим','9А','p20@gmail.com',071111131),

(231,'Григорьев Иван','7А','p21@gmail.com',071111132),
(232,'Баранова Мария','8Б','p22@gmail.com',071111133),
(233,'Дмитриев Никита','9Б','p23@gmail.com',071111134),
(234,'Максимова София','7Б','p24@gmail.com',071111135),
(235,'Павлов Артем','8А','p25@gmail.com',071111136),
(236,'Крылов Даниил','9А','p26@gmail.com',071111137),
(237,'Фомин Егор','7А','p27@gmail.com',071111138),
(238,'Быкова Анна','8Б','p28@gmail.com',071111139),
(239,'Гаврилов Кирилл','9Б','p29@gmail.com',071111140),
(240,'Никитина Алина','7Б','p30@gmail.com',071111141);

 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2001, 'День борьбы со спидом', '2024.12.12','13:10:00', 'актовый зал', 10, 'образовательное')
 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2002, 'Эрудит', '2024.12.15','15:10:00', 'актовый зал', 11, 'образовательное')
 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2003, 'Личности в истории', '2024.12.20','14:30:00', 'кабинет 113', 12, 'образовательное')
  Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2004, 'Новый год!', '2024.12.23','10:30:00', 'актовый зал', 13, 'культурное')
  Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2005, 'Литературный вечер', '2025.01.17','14:10:00', 'актовый зал', 14, 'образовательное')
  Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2006, 'Веселые старты', '2025.01.28','13:30:00', 'спорт зал', 15, 'спортивное')
  Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2007, 'Технический марафон', '2025.02.12','14:10:00', 'кабинет 100', 16, 'образовательное')
 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2008, 'День в Париже',  '2025.02.14','14:30:00', 'актовый зал', 17, 'культурное')
Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2009, 'Минута Славы',  '2025.03.14','12:30:00', 'актовый зал', 18, 'культурное')
 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2010, 'Инфознайка',  '2025.03.28','14:30:00', 'кабинет 205', 19, 'образовательное')
 Insert into Eventt(event_cod ,event_name, date_e, time_e, place, organizer_cod, type_e)
 Values (2011, 'Пасхальный фестиваль', '2025.04.15','12:10:00', 'актовый зал', 10, 'культурное')
 SELECT * FROM Eventt;

 INSERT INTO Eventt VALUES
(2012,'Математический бой','2025-04-20','13:00','каб.101',20,'образовательное'),
(2013,'Физический квест','2025-04-22','14:00','каб.102',21,'образовательное'),
(2014,'Географическая викторина','2025-04-25','12:00','каб.103',22,'образовательное'),
(2015,'Научное шоу','2025-04-27','15:00','актовый зал',23,'образовательное'),
(2016,'Музыкальный конкурс','2025-05-01','13:30','актовый зал',24,'культурное'),
(2017,'Трудовой день','2025-05-03','10:00','двор',25,'социальное'),
(2018,'День психологии','2025-05-05','12:00','каб.110',26,'образовательное'),
(2019,'Спортивный турнир','2025-05-07','14:00','спортзал',27,'спортивное'),
(2020,'Логопедический тренинг','2025-05-10','11:00','каб.111',28,'образовательное'),
(2021,'Исторический квест','2025-05-12','13:00','каб.113',29,'образовательное'),

(2022,'Английский клуб','2025-05-15','14:00','каб.114',30,'образовательное'),
(2023,'Футбол','2025-05-18','16:00','стадион',31,'спортивное'),
(2024,'Диктант','2025-05-20','10:00','каб.115',32,'образовательное'),
(2025,'Олимпиада','2025-05-22','12:00','каб.116',33,'образовательное'),
(2026,'Концерт','2025-05-25','15:00','актовый зал',34,'культурное'),
(2027,'IT конкурс','2025-05-27','14:00','каб.205',35,'образовательное'),
(2028,'Гео игра','2025-05-30','13:00','каб.103',36,'образовательное'),
(2029,'Исторический вечер','2025-06-01','14:00','актовый зал',37,'культурное'),
(2030,'Биология','2025-06-03','12:00','каб.120',38,'образовательное'),
(2031,'Физика шоу','2025-06-05','15:00','каб.121',39,'образовательное'),

(2032,'Выпускной','2025-06-10','18:00','актовый зал',40,'культурное'),
(2033,'Совещание','2025-06-12','10:00','каб.100',41,'служебное'),
(2034,'Концерт','2025-06-15','16:00','актовый зал',42,'культурное'),
(2035,'Труд день','2025-06-18','09:00','двор',43,'социальное'),
(2036,'Химия шоу','2025-06-20','13:00','каб.122',44,'образовательное'),
(2037,'Соревнования','2025-06-22','14:00','спортзал',45,'спортивное'),
(2038,'Тренинг','2025-06-25','11:00','каб.123',46,'образовательное'),
(2039,'Программирование','2025-06-27','15:00','каб.205',47,'образовательное'),
(2040,'Английский','2025-06-30','13:00','каб.114',48,'образовательное'),
(2041,'Математика','2025-07-02','12:00','каб.116',49,'образовательное');

 Insert into Participation(event_cod, student_cod)
 Values (2001, 201)
  Insert into Participation(event_cod, student_cod)
 Values (2002, 205)
  Insert into Participation(event_cod, student_cod)
 Values (2003, 204)
  Insert into Participation(event_cod, student_cod)
 Values (2004, 202)
  Insert into Participation(event_cod, student_cod)
 Values (2005, 208)
  Insert into Participation(event_cod, student_cod)
 Values (2006, 207)
  Insert into Participation(event_cod, student_cod)
 Values (2007, 203)
  Insert into Participation(event_cod, student_cod)
 Values (2008, 209)
  Insert into Participation(event_cod, student_cod)
 Values (2009, 210)
  Insert into Participation(event_cod, student_cod)
 Values (2010, 206)
 Insert into Participation(event_cod, student_cod)
 Values (2011, 206)
 Insert into Participation(event_cod, student_cod)
 Values (2011, 204)
 Insert into Participation(event_cod, student_cod)
 Values (2010, 203)
 SELECT * FROM Participation;

 SELECT TOP 1 * FROM Eventt

 INSERT INTO Participation VALUES
(2012,211),(2013,212),(2014,213),(2015,214),(2016,215),
(2017,216),(2018,217),(2019,218),(2020,219),(2021,220),
(2022,221),(2023,222),(2024,223),(2025,224),(2026,225),
(2027,226),(2028,227),(2029,228),(2030,229),(2031,230),
(2032,231),(2033,232),(2034,233),(2035,234),(2036,235),
(2037,236),(2038,237),(2039,238),(2040,239),(2041,240);


create index id_event_date on Eventt(date_e);
create index id_event_name on Eventt(event_name);
go

update Organizer 
set email_0 = 'antoninaruntova@gmail.com', phone_number_o = '079999999' 
where organizer_cod = 10;
go

update Participant 
set email_s = 'denism2010@gmail.com', phone_number_s = '078888888' 
where student_cod = 201;
go

update Eventt 
set date_e = '2025-01-10', time_e = '16:00:00', place = 'кабинет 101' 
where event_cod = 2002;
go

update Eventt 
set type_e = 'образовательное' 
where event_cod = 2008;
go

delete from Participation 
where event_cod = 2001 AND student_cod = 201;
go

Insert Into Participation (event_cod, student_cod) 
Values (2002, 202);
go



--Подзапросы:
--Подзапрос, который вичисляет участников, которые посещают больше одного мероприятия:
select student_FIO
from Participant
where student_cod in (
    select student_cod
    from Participation
    group by student_cod
    having count(event_cod) > 1
)
go

--Подзапрос для получения самого позднего мероприятия по дате:
select event_name, date_e, time_e
from Eventt
where date_e = (
      select max(date_e) 
      from Eventt
	  )
go

--Найти все мероприятия, которые организовал определённый учитель
select event_name
from Eventt
where organizer_cod = (
    select organizer_cod
    from Organizer
    where organizer_FIO = 'Рунтова Антонина Федоровна'
)
go

--Найти участников, которые посетили определённое мероприятие
select student_FIO, class
from Participant
where student_cod in (
    select student_cod
   from Participation
    where event_cod = (
        select event_cod
        from Eventt
        where event_name = 'Пасхальный фестиваль'
    )
)
go



--Запросы на группировку и агрегацию:
--Количество мероприятий, организованных каждым учителем:
select O.organizer_FIO, count(E.event_cod) as event_count
from Organizer O
left join Eventt E on O.organizer_cod = E.organizer_cod
group by O.organizer_FIO
go

--Количество студентов, участвующих в мероприятиях, из каждого класса
select P.class, count(distinct Pa.student_cod) as number_of_participants
from Participant P
inner join Participation Pa ON P.student_cod = Pa.student_cod
group by P.class
go

--Общее количество мероприятий, проведённых в каждом месяце
select month(date_e) as event_month,count(event_cod) as number_of_events
from Eventt
group by month(date_e)
order by event_month
go



--Представление для отображения мероприятий с их организаторами:
create view V_OrganizerEvents as
select
    E.event_cod, 
    E.event_name, 
    E.date_e, 
    E.time_e, 
    E.place, 
    O.organizer_FIO as organizer_FIO, 
    O.job_name as organizer_job
from Eventt E
inner join Organizer O on E.organizer_cod = O.organizer_cod
go

select * from V_OrganizerEvents
go

--Запрос с использованием представления
--Список организаторов, которые провели более 1 мероприятий
select organizer_FIO, count(event_cod) as number_of_events
from V_OrganizerEvents
group by organizer_FIO
having count(event_cod) > 1
go

--информация о всех мероприятиях, организованных определенным организатором, отсортированных по дате
select
    event_cod, 
    event_name, 
    date_e, 
    time_e, 
    place, 
    organizer_FIO, 
    organizer_job
from V_OrganizerEvents
where organizer_FIO = 'Рунтова Антонина Федоровна'
order by date_e
go


--Сложные запросы: 
--Найти класс с наибольшим числом участников в мероприятиях
select P.class,count(P.student_cod) as student_count
from Participant P
inner join Participation Pa on P.student_cod = Pa.student_cod
group by P.class
order by student_count desc
go

--Запрос, который вычисляет учеников и мероприятий, на которых они присутствуют, по определенному типу
select P.student_FIO, P.class, E.event_name,  E.type_e
from Participant P
inner join Participation Pa ON P.student_cod = Pa.student_cod
inner join Eventt E ON Pa.event_cod = E.event_cod
where E.type_e = 'культурное'
order by P.student_FIO
go

--запрос, который вычисляет участников, которые посещают больше одного мероприятия:
select P.student_FIO, count(Pa.event_cod) as event_count
from Participant P
inner join Participation Pa on P.student_cod = Pa.student_cod
group by P.student_FIO
having count(Pa.event_cod) > 1
go




--ГЕНЕРАЦИЯ ПК
CREATE FUNCTION dbo.GenerateOrganizerCode()
RETURNS smallint
AS
BEGIN
    DECLARE @NewCode smallint;

    -- Берем максимум текущих кодов и добавляем 1
    SELECT @NewCode = ISNULL(MAX(organizer_cod), 0) + 1
    FROM Organizer;

    RETURN @NewCode;
END
GO
--пример 
INSERT INTO Organizer(organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
VALUES (dbo.GenerateOrganizerCode(), 'Иванова Анна', 'учитель', 'anna@gmail.com', '079123456');
select * from Organizer

--Функция генерации кода мероприятия
/*CREATE FUNCTION dbo.GenerateEventCode(@OrganizerCode code)
RETURNS smallint
AS
BEGIN
    DECLARE @NewCode code, @MaxSuffix smallint;

    -- Находим максимальный suffix для мероприятий данного организатора
    SELECT @MaxSuffix = ISNULL(MAX(event_cod % 100), 0)
    FROM Eventt
    WHERE organizer_cod = @OrganizerCode;

    -- Генерация нового кода
    SET @NewCode = @OrganizerCode * 100 + @MaxSuffix + 1;

    RETURN @NewCode;
END
GO
--пример 
INSERT INTO Eventt(event_cod, event_name, date_e, time_e, place, organizer_cod, type_e)
VALUES (dbo.GenerateEventCode(2), 'Эрудит', '2024-12-15', '15:10:00', 'актовый зал', 2, 'образовательное');*/

--Функция генерации кода ученика по классу
/*CREATE FUNCTION dbo.GenerateStudentCode(@Class varchar(3))
RETURNS smallint
AS
BEGIN
    DECLARE @ClassNum smallint;
    DECLARE @NewCode smallint, @MaxSuffix smallint;

    -- Берем только числовую часть класса (например '7Б' -> 7)
    SET @ClassNum = CAST(LEFT(@Class, LEN(@Class)-1) AS smallint);

    -- Находим максимальный suffix для этого класса
    SELECT @MaxSuffix = ISNULL(MAX(student_cod % 1000), 0)
    FROM Participant
    WHERE CAST(student_cod / 1000 AS smallint) = @ClassNum;

    -- Генерируем новый код
    SET @NewCode = @ClassNum * 1000 + @MaxSuffix + 1;

    RETURN @NewCode;
END
GO
--пример
INSERT INTO Participant(student_cod, student_FIO, class, email_s, phone_number_s)
VALUES (dbo.GenerateStudentCode('7Б'), 'Иванов Пётр', '7Б', 'ivanovp@gmail.com', '072123456');


SELECT SERVERPROPERTY('MachineName') AS MachineName,
       SERVERPROPERTY('ServerName') AS ServerName,
       SERVERPROPERTY('InstanceName') AS InstanceName;

       SELECT @@SERVERNAME;*/

--Процедуры

create procedure add_Organizer(
                 @organizer_cod code,
                 @organizer_FIO full_name,
                 @job_name varchar(20),
                 @email_0 email,
                 @phone_number_o phone
                 )
as
begin
set @organizer_FIO = ltrim(rtrim(@organizer_FIO))
set @email_0 = ltrim(rtrim(@email_0))
insert into Organizer(organizer_cod, organizer_FIO, job_name, email_0, phone_number_o)
values (@organizer_cod, @organizer_FIO, @job_name, @email_0, @phone_number_o)
select * from Organizer where organizer_cod = @organizer_cod
end
go

ALTER TABLE Eventt ADD image_path VARCHAR(200) NULL

UPDATE Eventt SET image_path = 'Images/' + CAST(event_cod AS VARCHAR) + '.jpg'



CREATE OR ALTER PROCEDURE sp_Event_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, image_path
    FROM Eventt
    ORDER BY date_e DESC
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Search
    @SearchText NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, image_path
    FROM Eventt
    WHERE event_name LIKE '%' + @SearchText + '%'
       OR place      LIKE '%' + @SearchText + '%'
       OR type_e     LIKE '%' + @SearchText + '%'
    ORDER BY date_e DESC
END
GO



CREATE OR ALTER PROCEDURE sp_Event_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, image_path
    FROM Eventt
    ORDER BY date_e DESC
END
GO

CREATE OR ALTER PROCEDURE sp_Event_Search
    @SearchText NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT event_cod, event_name, date_e, time_e, place, organizer_cod, type_e, image_path
    FROM Eventt
    WHERE event_name LIKE '%' + @SearchText + '%'
       OR place      LIKE '%' + @SearchText + '%'
       OR type_e     LIKE '%' + @SearchText + '%'
    ORDER BY date_e DESC
END
GO

EXEC sp_Event_GetAll