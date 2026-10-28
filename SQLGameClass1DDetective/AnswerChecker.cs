using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLGameClass1DDetective;

/// <summary>答えの種類。種類ごとに、表記ゆれの吸収の仕方が違う。</summary>
public enum AnswerKind { Name, Number, Time }

/// <summary>提出された答えが正解かを判定する。</summary>
public static class AnswerChecker
{
    private static readonly string[] Honorifics = { "さん", "くん", "ちゃん", "君", "氏" };

    private static readonly Dictionary<char, int> KanjiDigits = new()
    {
        ['〇'] = 0,
        ['零'] = 0,
        ['一'] = 1,
        ['二'] = 2,
        ['三'] = 3,
        ['四'] = 4,
        ['五'] = 5,
        ['六'] = 6,
        ['七'] = 7,
        ['八'] = 8,
        ['九'] = 9,
    };

    public static bool IsCorrect(IEnumerable<string> accepted, string input, AnswerKind kind)
    {
        var normalized = Normalize(input, kind);
        if (normalized.Length == 0) return false;

        return accepted.Any(a => Normalize(a, kind) == normalized);
    }

    /// <summary>答え欄の下に出す「答え方」の説明。</summary>
    public static string FormatHint(AnswerKind kind) => kind switch
    {
        AnswerKind.Time => "答え方: 時刻（例: 07:41）",
        AnswerKind.Number => "答え方: 数（例: 3）",
        _ => "答え方: 名前",
    };

    private static string Normalize(string s, AnswerKind kind) => kind switch
    {
        AnswerKind.Time => NormalizeTime(s),
        AnswerKind.Number => NormalizeNumber(s),
        _ => NormalizeName(s),
    };

    /// <summary>全角/半角・大文字小文字・空白・句読点の違いを吸収する。</summary>
    private static string Base(string s)
    {
        var t = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var sb = new StringBuilder();
        foreach (var ch in t)
        {
            if (char.IsWhiteSpace(ch) || ch == '。' || ch == '、' || ch == '.' || ch == ',') continue;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    // 名前: 「ゼリィさん」「ゼリィ君」も「ゼリィ」として扱う
    private static string NormalizeName(string s)
    {
        var t = Base(s);
        foreach (var suffix in Honorifics)
        {
            if (t.Length > suffix.Length && t.EndsWith(suffix))
                return t[..^suffix.Length];
        }
        return t;
    }

    // 数: 「3」「３」「3人」「三人」を、すべて「3」として扱う
    private static string NormalizeNumber(string s)
    {
        var t = ReplaceKanjiNumber(Base(s));
        var m = Regex.Match(t, @"^\d+");
        return m.Success ? m.Value : t;
    }

    private static string ReplaceKanjiNumber(string t)
    {
        // 十の位を含む形（十、十二、二十、二十三 など）
        var m = Regex.Match(t, @"^([一二三四五六七八九])?十([一二三四五六七八九])?");
        if (m.Success)
        {
            int tens = m.Groups[1].Success ? KanjiDigits[m.Groups[1].Value[0]] : 1;
            int ones = m.Groups[2].Success ? KanjiDigits[m.Groups[2].Value[0]] : 0;
            return (tens * 10 + ones).ToString() + t[m.Length..];
        }

        // 一桁（または、並べた数字）
        var sb = new StringBuilder();
        int i = 0;
        while (i < t.Length && KanjiDigits.ContainsKey(t[i]))
        {
            sb.Append(KanjiDigits[t[i]]);
            i++;
        }
        return sb.Length > 0 ? sb.ToString() + t[i..] : t;
    }

    // 時刻: 「07:41」「7:41」「7時41分」「07時41分」を、すべて「07:41」として扱う
    private static string NormalizeTime(string s)
    {
        var t = Base(s);
        var m = Regex.Match(t, @"^(\d{1,2})(?::|時)(\d{1,2})(?:分)?(?::\d{2})?$");
        if (!m.Success) return t;

        int h = int.Parse(m.Groups[1].Value);
        int min = int.Parse(m.Groups[2].Value);
        return $"{h:00}:{min:00}";
    }
}