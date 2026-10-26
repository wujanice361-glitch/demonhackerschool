using System;
using System.Linq;
using System.Text;

namespace SQLGameClass1DDetective;

/// <summary>提出された答えが正解かを判定する。</summary>
public static class AnswerChecker
{
    public static bool IsCorrect(int day, string input)
    {
        var normalized = Normalize(input);
        if (normalized.Length == 0) return false;

        return CaseCatalog.Get(day).AcceptedAnswers
                          .Any(a => Normalize(a) == normalized);
    }

    /// <summary>全角/半角・大文字小文字・空白・句読点の違いを吸収する。</summary>
    private static string Normalize(string s)
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
}