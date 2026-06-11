using System;
using UnityEngine;

// チュートリアル用 README アセットのデータ本体。
public class Readme : ScriptableObject
{
    // 表示用アイコンを保持する。
    public Texture2D icon;
    // タイトル文を保持する。
    public string title;
    // セクション一覧を保持する。
    public Section[] sections;
    // レイアウトを読み込んだかを保持する。
    public bool loadedLayout;

    // README 内の各説明ブロックを表す。
    [Serializable]
    public class Section
    {
        // 見出し・本文・リンク情報をまとめて保持する。
        public string heading, text, linkText, url;
    }
}
