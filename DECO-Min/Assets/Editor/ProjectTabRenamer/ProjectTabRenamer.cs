using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectTabRenamer
{
    private static MethodInfo getActiveFolderPathMethod;
    private static GUIContent defaultTitle = null;

    static ProjectTabRenamer()
    {
        // Unity 6のProjectBrowserクラスから、現在開いているフォルダパスを取得する内部メソッドを取得
        var projectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        if (projectBrowserType != null)
        {
            getActiveFolderPathMethod = projectBrowserType.GetMethod("GetActiveFolderPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        // エディタの更新イベントに登録
        EditorApplication.update += UpdateTabNames;
    }

    private static void UpdateTabNames()
    {
        if (getActiveFolderPathMethod == null) return;

        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (var window in windows)
        {
            if (window == null || window.GetType().Name != "ProjectBrowser") continue;

            // ロック状態（isLocked）を取得
            var propertyInfo = window.GetType().GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null) continue;

            bool isLocked = (bool)propertyInfo.GetValue(window);

            if (isLocked)
            {
                // Unity 6仕様：ロックされたタブが現在「実際に開いているフォルダパス」を直接取得
                string path = (string)getActiveFolderPathMethod.Invoke(window, null);

                if (!string.IsNullOrEmpty(path))
                {
                    // パスから末尾のフォルダ名だけを切り出す
                    string folderName = System.IO.Path.GetFileName(path);

                    // ルート（Assets直下）の場合は「Assets」と表示
                    if (string.IsNullOrEmpty(folderName) && path == "Assets")
                    {
                        folderName = "Assets";
                    }

                    if (!string.IsNullOrEmpty(folderName))
                    {
                        if (window.titleContent.ToString() != folderName)
                        {
                            if(defaultTitle == null)
                            {
                                defaultTitle = new GUIContent(window.titleContent);
                            }
                            window.titleContent = new GUIContent($"{folderName}");
                            continue;
                        }
                    }
                }
            }
            else
            {
                // ロックが解除されたら通常の「Project」という名前に戻す
                if (defaultTitle != null)
                {
                    window.titleContent = new GUIContent(defaultTitle);
                }
            }
        }
    }
}
