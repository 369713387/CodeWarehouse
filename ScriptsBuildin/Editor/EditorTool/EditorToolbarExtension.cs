using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.CodeEditor;

namespace YF.EditorTools
{
    public class EditorToolbarExtension
    {
        private static GUIContent switchSceneBtContent;

        private static List<string> sceneAssetList;

        [InitializeOnLoadMethod]
        static void Init()
        {
            sceneAssetList = new List<string>();

            var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");
            EditorSceneManager.sceneOpened += OnSceneOpened;

            UnityEditorToolbar.LeftToolbarGUI.Add(OnLeftToolbarGUI);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
        }

        private static void OnLeftToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            {
                DrawSwithSceneDropdownMenus();
            }
        }

        static void DrawSwithSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            popMenu.allowDuplicateNames = true;

            // 支持多个场景根目录，使用分号或逗号分隔
            string[] sceneRootPaths = Array.Empty<string>();
            if (!string.IsNullOrEmpty(ConstEditor.ScenePath))
            {
                sceneRootPaths = ConstEditor.ScenePath
                    .Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => Utility.Path.GetRegularPath(p.Trim()).TrimEnd('/'))
                    .ToArray();
            }
            if (sceneRootPaths.Length == 0)
            {
                sceneRootPaths = new string[] { Utility.Path.GetRegularPath(ConstEditor.ScenePath).TrimEnd('/') };
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", sceneRootPaths);
            sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                sceneAssetList.Add(scenePath);

                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                string normalizedDir = Utility.Path.GetRegularPath(fileDir).TrimEnd('/');

                // 匹配所在的根目录（选择最长匹配）
                string matchedRoot = sceneRootPaths
                    .Where(root => normalizedDir == root || normalizedDir.StartsWith(root + "/"))
                    .OrderByDescending(root => root.Length)
                    .FirstOrDefault();

                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;

                if (!string.IsNullOrEmpty(matchedRoot))
                {
                    bool isInRootDir = normalizedDir == matchedRoot;
                    if (!isInRootDir)
                    {
                        var sceneDir = System.IO.Path.GetRelativePath(matchedRoot, fileDir);
                        sceneDir = Utility.Path.GetRegularPath(sceneDir).TrimEnd('/');
                        displayName = $"{sceneDir}/{sceneName}";
                    }
                }
                else
                {
                    // 回退：显示相对于 Assets 的路径
                    var sceneDir = System.IO.Path.GetRelativePath("Assets", fileDir);
                    sceneDir = Utility.Path.GetRegularPath(sceneDir).TrimEnd('/');
                    if (!string.IsNullOrEmpty(sceneDir))
                    {
                        displayName = $"{sceneDir}/{sceneName}";
                    }
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
            }
            popMenu.ShowAsContext();
        }

        private static void SwitchScene(int menuIdx)
        {
            if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[menuIdx];
                var curScene = EditorSceneManager.GetActiveScene();
                if (curScene != null && curScene.isDirty)
                {
                    int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                    switch (opIndex)
                    {
                        case 0:
                            if (!EditorSceneManager.SaveOpenScenes())
                            {
                                return;
                            }
                            break;
                        case 1:
                            return;
                    }
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }
    }
}


