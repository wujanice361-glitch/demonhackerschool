using System.Linq;

namespace SQLGameClass1DDetective; // ← 自分のプロジェクトの名前空間に合わせる

/// <summary>1日分の事件の情報。画面の文章・ヒント・正解・教官の解説をここに集める。</summary>
public record CaseInfo(
    int Day,
    string Title,
    string Situation,        // 左パネルの「状況」
    string Objective,        // 左パネルの「目的」
    string[] Hints,          // メニューのヒント（1つずつ開示）
    string[] AcceptedAnswers,// 正解として認める入力（表記ゆれ対策で複数書いておく）
    string Explanation);     // クリア後に教官が語る、事件のあらまし

public static class CaseCatalog
{
    public static readonly CaseInfo[] All =
    {
        new(
            Day: 1,
            Title: "教官への贈り物",
            Situation:
                "初登校日の朝。教官の机の上に、花を添えた一通の手紙が置かれていた。\n" +
                "前の晩、教官は実習室に封印をかけ、机の上に何もないことを確認している。\n" +
                "朝は門番ゴーレムが封印を解いて室内を点検し（このとき机の上は空だった）、" +
                "見習いたちが次々と登校。始業の鐘と同時に、教官が入室した。",
            Objective:
                "実習室の入退室記録から、手紙を置いた者を特定する。\n" +
                "誰にも見られずに教官の机へ近づけたのは、誰か。",
            Hints: new[]
            {
                "まずは 学生名簿・職員名簿・入退室記録 の中身を見てみよう。テーブルの構造は、右上の「テーブル構造」から確認できる。",
                "4月8日の朝の記録だけを、時刻順に並べてみよう。（WHERE と ORDER BY）人物ID が誰なのかは、各名簿で調べられる。",
                "門番ゴーレムが出たあと、最初に入室した見習いは誰だろう。そのとき、室内に他の誰かはいたかな？",
            },
            AcceptedAnswers: new[] { "ネム", "ネムさん" },
            Explanation:
                "見事だ。手紙を置いたのは、ネムだ。\n\n" +
                "門番が点検を終えて出たあと、最初に実習室へ入ったのがネム。" +
                "ほかの見習いが来るまでの数分間、室内にいたのはネムただ一人。" +
                "その間に私の机へ近づけたのは、ネムだけだ。\n\n" +
                "さて、ここで一つ教えておこう。あの手紙は、贈り物ではない。" +
                "花は添えてあったが、中身は脅迫状だ。\n\n" +
                "もう一つ。出席簿を見ると、ネムは今朝の点呼に答えていない。" +
                "手紙を置いて、鐘が鳴る前に姿を消したということだ。\n\n" +
                "初登校日に、顔も見せず、脅迫状だけを残していく見習い。……面白いじゃないか。" +
                "記録は「いつ」と「誰が」までしか語らない。「なぜ」は、これから調べていこう。"),
    };

    public static int Count => All.Length;
    public static CaseInfo Get(int day) => All.First(c => c.Day == day);
    public static bool HasNext(int day) => All.Any(c => c.Day == day + 1);
}