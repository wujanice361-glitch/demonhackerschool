-- 起動のたびに実行される。何度実行しても安全なように、すべて「なければ作る」形にしている。

-- ===== データベース =====
IF DB_ID('SchoolCase') IS NULL CREATE DATABASE SchoolCase;
GO
IF DB_ID('SchoolCaseAnswer') IS NULL CREATE DATABASE SchoolCaseAnswer;
GO

-- ===== プレイヤー用の読み取り専用ユーザー（ログインなし） =====
USE SchoolCase;
GO
IF USER_ID('player_reader') IS NULL CREATE USER player_reader WITHOUT LOGIN;
GO
ALTER ROLE db_datareader ADD MEMBER player_reader;
GO

-- ===== 進行状況（player_reader には権限を与えない） =====
USE SchoolCaseAnswer;
GO
IF OBJECT_ID('dbo.Progress') IS NULL
CREATE TABLE dbo.Progress (
    DayNo          INT          NOT NULL PRIMARY KEY,
    Cleared        BIT          NOT NULL DEFAULT 0,
    ClearedAt      DATETIME2(0) NULL,
    ElapsedSeconds INT          NULL,
    HintsUsed      INT          NOT NULL DEFAULT 0
);
GO
