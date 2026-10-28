-- Day 1（2周目用）の隠しテーブル。2周目で事件データを入れるときに、Day01.sql のあとに流す。
-- ・記録修正履歴: 1周目の終了時に開示する入口のテーブル（このテーブルだけ先に流してもよい）
-- ・机の魔力記録_原本: 修正前の記録（必須の隠しテーブル）

CREATE TABLE 記録修正履歴 (
    修正ID       INT IDENTITY(1,1) PRIMARY KEY,
    対象テーブル NVARCHAR(30)  NOT NULL,
    対象記録ID   INT           NOT NULL,
    修正者ID     INT           NOT NULL,
    修正日時     DATETIME2(0)  NOT NULL,
    理由         NVARCHAR(50)  NULL,
    原本の保管先 NVARCHAR(50)  NULL
);
GO

CREATE TABLE 机の魔力記録_原本 (
    記録ID       INT           NOT NULL PRIMARY KEY,
    日時         DATETIME2(0)  NOT NULL,
    内容         NVARCHAR(60)  NOT NULL,
    近接した魔力 NVARCHAR(20)  NULL
);
GO

INSERT INTO 記録修正履歴 (対象テーブル, 対象記録ID, 修正者ID, 修正日時, 理由, 原本の保管先) VALUES
    (N'机の魔力記録', 3, 1, '2026-04-08 08:12:00', N'記録の整理', N'机の魔力記録_原本');
GO

INSERT INTO 机の魔力記録_原本 (記録ID, 日時, 内容, 近接した魔力) VALUES
    (1, '2026-04-07 18:10:00', N'封印を施錠。机の上に物体なし',             N'骸骨族の魔力'),
    (2, '2026-04-08 07:31:00', N'封印を解除。点検の結果、机の上に物体なし',  N'岩人形族の魔力'),
    (3, '2026-04-08 07:49:00', N'机の上に物体が出現（軽量の紙と花）',        N'三頭犬族の魔力'),
    (4, '2026-04-08 08:00:00', N'机の上に物体あり。教官が確認',              NULL);
GO