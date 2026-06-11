using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

// Readme アセットを見やすく表示し、チュートリアルの初期レイアウトも管理する。
[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
public class ReadmeEditor : Editor
{
    // このセッションで README を自動選択したかを記録するキー。
    static string s_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";

    // README ソースディレクトリのルート。
    static string s_ReadmeSourceDirectory = "Assets/TutorialInfo";

    // セクション間の余白。
    const float k_Space = 16f;

    // エディタ起動後に README を自動選択する。
    static ReadmeEditor()
    {
        EditorApplication.delayCall += SelectReadmeAutomatically;
    }

    // チュートリアル素材を削除する。
    static void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",

            $"All contents under {s_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(s_ReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {s_ReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    // 起動時に README を自動選択し、必要ならレイアウトを読む。
    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(s_ShowedReadmeSessionStateName, false))
        {
            var readme = SelectReadme();
            SessionState.SetBool(s_ShowedReadmeSessionStateName, true);

            if (readme && !readme.loadedLayout)
            {
                LoadLayout();
                readme.loadedLayout = true;
            }
        }
    }

    // 標準レイアウトを読み込む。
    static void LoadLayout()
    {
        var assembly = typeof(EditorApplication).Assembly;
        var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
        var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
        method.Invoke(null, new object[] { Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false });
    }

    // README アセットを探して選択する。
    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };

            return (Readme)readmeObject;
        }
        else
        {
            Debug.Log("Couldn't find a readme");
            return null;
        }
    }

    // インスペクタ上部のヘッダーを描画する。
    protected override void OnHeaderGUI()
    {
        var readme = (Readme)target;
        Init();

        var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

        GUILayout.BeginHorizontal("In BigTitle");
        {
            if (readme.icon != null)
            {
                GUILayout.Space(k_Space);
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
            }
            GUILayout.Space(k_Space);
            GUILayout.BeginVertical();
            {

                GUILayout.FlexibleSpace();
                GUILayout.Label(readme.title, TitleStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
    }

    // README の本文セクションを描画する。
    public override void OnInspectorGUI()
    {
        var readme = (Readme)target;
        Init();

        foreach (var section in readme.sections)
        {
            if (!string.IsNullOrEmpty(section.heading))
            {
                GUILayout.Label(section.heading, HeadingStyle);
            }

            if (!string.IsNullOrEmpty(section.text))
            {
                GUILayout.Label(section.text, BodyStyle);
            }

            if (!string.IsNullOrEmpty(section.linkText))
            {
                if (LinkLabel(new GUIContent(section.linkText)))
                {
                    Application.OpenURL(section.url);
                }
            }

            GUILayout.Space(k_Space);
        }

        if (GUILayout.Button("Remove Readme Assets", ButtonStyle))
        {
            RemoveTutorial();
        }
    }

    // GUI スタイルの初期化フラグ。
    bool m_Initialized;

    // リンク描画用スタイルを返す。
    GUIStyle LinkStyle
    {
        get { return m_LinkStyle; }
    }

    // リンク描画用スタイルを保持する。
    [SerializeField]
    GUIStyle m_LinkStyle;

    // タイトル描画用スタイルを返す。
    GUIStyle TitleStyle
    {
        get { return m_TitleStyle; }
    }

    // タイトル描画用スタイルを保持する。
    [SerializeField]
    GUIStyle m_TitleStyle;

    // 見出し描画用スタイルを返す。
    GUIStyle HeadingStyle
    {
        get { return m_HeadingStyle; }
    }

    // 見出し描画用スタイルを保持する。
    [SerializeField]
    GUIStyle m_HeadingStyle;

    // 本文描画用スタイルを返す。
    GUIStyle BodyStyle
    {
        get { return m_BodyStyle; }
    }

    // 本文描画用スタイルを保持する。
    [SerializeField]
    GUIStyle m_BodyStyle;

    // ボタン描画用スタイルを返す。
    GUIStyle ButtonStyle
    {
        get { return m_ButtonStyle; }
    }

    // ボタン描画用スタイルを保持する。
    [SerializeField]
    GUIStyle m_ButtonStyle;

    // スタイルを初期化する。
    void Init()
    {
        if (m_Initialized)
            return;
        m_BodyStyle = new GUIStyle(EditorStyles.label);
        m_BodyStyle.wordWrap = true;
        m_BodyStyle.fontSize = 14;
        m_BodyStyle.richText = true;

        m_TitleStyle = new GUIStyle(m_BodyStyle);
        m_TitleStyle.fontSize = 26;

        m_HeadingStyle = new GUIStyle(m_BodyStyle);
        m_HeadingStyle.fontStyle = FontStyle.Bold;
        m_HeadingStyle.fontSize = 18;

        m_LinkStyle = new GUIStyle(m_BodyStyle);
        m_LinkStyle.wordWrap = false;

        // Match selection color which works nicely for both light and dark skins
        m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
        m_LinkStyle.stretchWidth = false;

        m_ButtonStyle = new GUIStyle(EditorStyles.miniButton);
        m_ButtonStyle.fontStyle = FontStyle.Bold;

        m_Initialized = true;
    }

    // クリック可能なリンクラベルを描画する。
    bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
    {
        var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

        Handles.BeginGUI();
        Handles.color = LinkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, LinkStyle);
    }
}
