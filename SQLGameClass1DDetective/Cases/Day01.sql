-- Day 1: 教官への贈り物
-- 初登校日の朝、教官の机に花を添えた手紙が置かれていた。
-- 日本語の文字列は N'...' を付けること（付けないと文字化けする）

-- 在籍者の名簿（教官・門番・見習い）
CREATE TABLE Members (
    MemberId INT          NOT NULL PRIMARY KEY,
    Name     NVARCHAR(50) NOT NULL,
    Species  NVARCHAR(30) NULL,          -- 種族。ネムだけ記載なし（後の伏線）
    Role     VARCHAR(12)  NOT NULL       -- Instructor / Gatekeeper / Apprentice
);
GO

-- 実習室の入退室記録（魔力紋の認証ログ）
CREATE TABLE EntryLog (
    LogId     INT IDENTITY(1,1) PRIMARY KEY,
    MemberId  INT          NOT NULL REFERENCES Members(MemberId),
    Direction VARCHAR(3)   NOT NULL CHECK (Direction IN ('IN','OUT')),
    LoggedAt  DATETIME2(0) NOT NULL
);
GO

-- 出席簿（点呼の結果）
CREATE TABLE Attendance (
    MemberId  INT         NOT NULL REFERENCES Members(MemberId),
    ClassDate DATE        NOT NULL,
    Status    VARCHAR(10) NOT NULL CHECK (Status IN ('Present','Absent')),
    PRIMARY KEY (MemberId, ClassDate)
);
GO

INSERT INTO Members (MemberId, Name, Species, Role) VALUES
    (1, N'ホネスト', N'スケルトン',   'Instructor'),
    (2, N'ガルド',   N'ゴーレム',     'Gatekeeper'),
    (3, N'ネム',     NULL,            'Apprentice'),
    (4, N'ガル',     N'ゴブリン',     'Apprentice'),
    (5, N'ピピ',     N'インプ',       'Apprentice'),
    (6, N'ゼリィ',   N'スライム',     'Apprentice'),
    (7, N'モルグ',   N'オーガ',       'Apprentice'),
    (8, N'クロエ',   N'ヴァンパイア', 'Apprentice');
GO

INSERT INTO EntryLog (MemberId, Direction, LoggedAt) VALUES
    -- 前の晩: 教官が点検して実習室を出て、封印をかけた
    (1, 'IN',  '2026-04-07 17:30:00'),
    (1, 'OUT', '2026-04-07 18:10:00'),
    -- 初登校日の朝
    (2, 'IN',  '2026-04-08 07:30:00'),   -- 門番が封印を解いて点検
    (2, 'OUT', '2026-04-08 07:32:00'),
    (3, 'IN',  '2026-04-08 07:36:00'),   -- ネム: 一人きりになる
    (4, 'IN',  '2026-04-08 07:38:00'),
    (5, 'IN',  '2026-04-08 07:40:00'),
    (3, 'OUT', '2026-04-08 07:42:00'),   -- ネム: 鐘の前に退室
    (6, 'IN',  '2026-04-08 07:44:00'),
    (7, 'IN',  '2026-04-08 07:51:00'),
    (8, 'IN',  '2026-04-08 07:57:00'),
    (1, 'IN',  '2026-04-08 08:00:00');   -- 始業の鐘と同時に教官が入室
GO

INSERT INTO Attendance (MemberId, ClassDate, Status) VALUES
    (3, '2026-04-08', 'Absent'),
    (4, '2026-04-08', 'Present'),
    (5, '2026-04-08', 'Present'),
    (6, '2026-04-08', 'Present'),
    (7, '2026-04-08', 'Present'),
    (8, '2026-04-08', 'Present');
GO
