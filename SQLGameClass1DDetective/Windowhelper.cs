using System;
using System.Windows;

namespace SQLGameClass1DDetective;

public static class WindowHelper
{
    /// <summary>
    /// ウィンドウを、その端末の画面（作業領域＝タスクバーを除いた部分）に収める。
    /// 小さい画面でも、はみ出さないようにする。各ウィンドウのコンストラクターで呼ぶ。
    /// </summary>
    public static void FitToScreen(Window w, double ratio = 0.95)
    {
        var area = SystemParameters.WorkArea; // 画面の作業領域（デバイス非依存の単位）

        w.MaxWidth = area.Width;
        w.MaxHeight = area.Height;
        w.MinWidth = Math.Min(w.MinWidth, area.Width * ratio);
        w.MinHeight = Math.Min(w.MinHeight, area.Height * ratio);
        w.Width = Math.Min(w.Width, area.Width * ratio);
        w.Height = Math.Min(w.Height, area.Height * ratio);
    }
}