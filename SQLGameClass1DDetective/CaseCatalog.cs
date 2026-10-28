using System.Collections.Generic;
using System.Linq;

namespace SQLGameClass1DDetective; // ← 自分のプロジェクトの名前空間に合わせる

/// <summary>メインの設問1つ分。前の設問に正解すると、次の設問が出る。</summary>
public record QuestionInfo(
    string Prompt,
    AnswerKind Kind,
    string[] Accepted,   // 正解として認める入力（種類ごとに表記ゆれは自動で吸収される）
    string Hint);        // この設問のヒント（1つ）

/// <summary>サブクエスト1つ分。任意の課題で、1周目の推理には関係させない。</summary>
public record SubQuestInfo(
    int No,
    string Label,          // 練習 / 依頼 / 調査
    string Giver,          // 依頼者（いなければ空）
    string Prompt,
    AnswerKind Kind,
    string[] Accepted,
    string? RevealTable,   // null: 最初から見える。文字列: このテーブル名を含むSQLが成功したら現れる（それまでは「???」）
    string? UnlockScript,  // 達成したときに流す追加SQL（Cases/ の中のファイル名。なければ null）
    string? UnlockMessage, // 解放したときの通知
    string RewardNote);    // カードに出す報酬の説明

/// <summary>1日分の事件の情報。</summary>
public record CaseInfo(
    int Day,
    string Title,
    string Situation,                       // 左パネルの「状況」
    string Order,                           // 左パネルの「教官からの命令」
    QuestionInfo[] Questions,
    SubQuestInfo[] SubQuests,
    string Explanation,                     // クリア後に教官が語る、事件のあらまし
    string[] CountComments,                 // サブクエストの達成数で追加するコメント（0個 / 1〜2個 / 全部）
    Dictionary<int, string> QuestComments); // 特定のサブクエストを達成したときの追加コメント（キーはサブクエスト番号）

public static class CaseCatalog
{
    public static readonly CaseInfo[] All =
    {
        new(
            Day: 1,
            Title: "教官への贈り物",
            Situation:
                "初登校日の朝、教官ホネストの机に、花を添えた手紙が置かれていた。\n" +
                "前夜、教官は実習室に封印をかけ、机の上に何もないことを確認している。\n" +
                "朝は門番ガルドが封印を解いて点検したが（このとき机の上は空だった）、" +
                "その後、見習いたちが順に登校し、始業の鐘と同時に教官が入室した。\n\n" +
                "教官の机には、魔導センサーが仕込まれている。物体が現れた時刻と、近づいた魔力が、記録される。",
            Order:
                "机の魔導センサーと入退室の記録から、手紙を置いた見習いを突き止めよ。\n" +
                "設問に答えていけば、犯人にたどり着ける。",
            Questions: new[]
            {
                new QuestionInfo(
                    "教官の机には魔導センサーが仕込まれている。机の上に物体が現れたと記録された時刻は?",
                    AnswerKind.Time,
                    new[] { "07:41" },
                    "「テーブル構造」で『机の魔力記録』を見つけて、中身を見てみよう。"),

                new QuestionInfo(
                    "入退室記録の上で、その時刻に実習室にいた見習いは何人?",
                    AnswerKind.Number,
                    new[] { "3" },
                    "4月8日の朝の記録だけを、時刻順に並べて、入室と退室を数え合わせよう。（WHERE と ORDER BY）人物IDが100以上なら見習い。"),

                new QuestionInfo(
                    "センサーには、近づいた魔力の種類も残っていた。その3人のうち、その種族に当てはまる見習いは誰?",
                    AnswerKind.Name,
                    new[] { "ゼリィ" },
                    "机の記録の「近接した魔力」と、学生名簿の「種族」を見比べよう。"),
            },
            SubQuests: new[]
            {
                new SubQuestInfo(
                    1, "練習", "",
                    "4月8日の朝、見習いで最後に実習室へ入ったのは誰?",
                    AnswerKind.Name, new[] { "ホロウ" },
                    null, null, null, "評価"),

                new SubQuestInfo(
                    2, "依頼", "事務員オルガ",
                    "「契約書や重要書類に、複数のサインが要る見習いがいたはず。備考に『サイン』と書かれた子を教えて」",
                    AnswerKind.Name, new[] { "ケル" },
                    null, "Day01_unlock2",
                    "テーブル「提出書類記録」が解放されました。事務室で管理している書類の記録です。",
                    "事務室の資料（新しいテーブル）"),

                new SubQuestInfo(
                    3, "調査", "",
                    "4月8日の出席簿で『出席』になっているのに、入退室記録に入室の記録がない見習いは誰?",
                    AnswerKind.Name, new[] { "ユウ" },
                    "出席簿", null, null, "評価"),
            },
            Explanation:
                "…そうだ。手紙を置いたのはゼリィ。鐘の前に実習室を出て、点呼には答えなかった。\n\n" +
                "花を添えた手紙だ。贈り物に見えただろう。だが、中身は脅迫状だった。" +
                "『隠しているものを差し出せ』とな。\n\n" +
                "ゼリィには、私から話をしておく。……記録は嘘をつかない。覚えておけ。",
            CountComments: new[]
            {
                "答えだけなら、誰でも出せる。記録は、もう一歩先まで読むものだ。",
                "悪くない。だが、まだ見えていないものがある。",
                "…よく見ている。見えすぎるのも、考えものだがな。",
            },
            QuestComments: new Dictionary<int, string>
            {
                [2] = "事務室の書類まで調べたのか。…熱心なことだ。",
                [3] = "出席簿と見比べたのか。ゴーストは、記録に残らん。それだけのことだ。（少し早口）",
            }),
    };

    public static int Count => All.Length;
    public static CaseInfo Get(int day) => All.First(c => c.Day == day);
    public static bool HasNext(int day) => All.Any(c => c.Day == day + 1);

    /// <summary>サブクエストの達成状況に応じた、教官の追加コメント。</summary>
    public static string BuildSubQuestComment(CaseInfo info, ISet<int> done)
    {
        // 達成数: 0個 → 0、1〜(全部-1)個 → 1、全部 → 2
        int bucket = done.Count == 0 ? 0 : done.Count >= info.SubQuests.Length ? 2 : 1;

        var lines = new List<string>();
        if (bucket < info.CountComments.Length)
            lines.Add(info.CountComments[bucket]);

        foreach (var no in done.OrderBy(n => n))
        {
            if (info.QuestComments.TryGetValue(no, out var comment))
                lines.Add(comment);
        }
        return string.Join("\n\n", lines);
    }
}