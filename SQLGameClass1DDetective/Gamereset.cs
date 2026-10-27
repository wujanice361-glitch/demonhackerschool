using System.Threading.Tasks;

namespace SQLGameClass1DDetective;

public static class GameReset
{
    /// <summary>
    /// リセットを確定する。クリアの記録とメモを消して、新しいプロフィールに入れ替える。
    /// 導入を最後まで終えたあとに呼ぶこと（途中でやめたときは呼ばない）。
    /// </summary>
    public static async Task CommitAsync(GameProfile fresh)
    {
        await Task.Run(() => ProgressStore.ResetAll());

        MemoStore.Instance.Text = "";
        MemoStore.Instance.Save();
        GameProfile.Replace(fresh);
    }
}