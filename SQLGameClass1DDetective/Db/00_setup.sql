-- 起動のたびに実行される。何度実行しても安全なように、すべて「なければ作る」形にしている。

-- ===== データベース =====
-- 事件データのDBは日本語の照合順序にする。
-- （N なしの日本語、たとえば WHERE 役職 = '見習い' でも検索できるようにするため）
-- 以前の照合順序で作られていた場合は、作り直す（事件データは起動時に入れ直すので消えてよい）
IF DB_ID('SchoolCase') IS NOT NULL
   AND CONVERT(sysname, DATABASEPROPERTYEX('SchoolCase', 'Collation')) <> N'Japanese_CI_AS'
BEGIN
    ALTER DATABASE SchoolCase SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SchoolCase;
END
GO
IF DB_ID('SchoolCase') IS NULL CREATE DATABASE SchoolCase COLLATE Japanese_CI_AS;
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