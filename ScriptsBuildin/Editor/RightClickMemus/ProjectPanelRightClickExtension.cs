﻿using System;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

// Priority设置原则
// 分组间隔：相差≥11会产生分隔符，用于功能分组
// 使用频率：常用功能优先级更高（数值更小）
// 功能相关性：相关功能紧密排列
// 预留空间：每组预留足够空间便于后续扩展

namespace YF.EditorTools
{
    public partial class ProjectPanelRightClickExtension
    {

        public const string ADD_SCRIPT_TASK = "ADD_UISCRIPT_TASK";

        [MenuItem("Assets/YF Tools/Copy Asset Path/Relative Path", priority = 1)]
        static void CopyAssetRelativePath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 1);
        }
        [MenuItem("Assets/YF Tools/Copy Asset Path/Full Path", priority = 2)]
        static void CopyAssetFullPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 0);
        }
        [MenuItem("Assets/YF Tools/Copy Asset Path/Assets Name", priority = 3)]
        static void CopyAssetNameWithoutPath()
        {
            CopyAssetsPath2Clipboard(Selection.objects, 2);
        }

        /// <summary>
        /// 复制资源路径到剪贴板
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="copyFullPath"></param>
        private static void CopyAssetsPath2Clipboard(UnityEngine.Object[] assets, int pathMode)
        {
            if (assets == null || assets.Length < 1)
            {
                return;
            }
            StringBuilder strBuilder = new StringBuilder();
            switch (pathMode)
            {
                case 1: //Relative Path
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                case 2:
                    foreach (var item in assets)
                    {
                        var itemPath = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrWhiteSpace(itemPath) || !Path.HasExtension(itemPath))
                        {
                            continue;
                        }
                        itemPath = Path.GetFileName(itemPath);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
                default: //Full Path
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    foreach (var item in assets)
                    {
                        var itemPath = Path.GetFullPath(AssetDatabase.GetAssetPath(item), projectRoot);
                        strBuilder.AppendLine(itemPath);
                    }
                    break;
            }
            var result = strBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            EditorGUIUtility.systemCopyBuffer = result;
        }

        [InitializeOnLoadMethod]
        private static void TaskRefresh()
        {
            if (EditorPrefs.HasKey(ADD_SCRIPT_TASK))
            {
                var infos = EditorPrefs.GetString(ADD_SCRIPT_TASK).Split('|');
                EditorPrefs.DeleteKey(ADD_SCRIPT_TASK);
                if (infos.Length != 2) return;
                var goAssetFile = infos[0];
                var monoScriptFile = infos[1];
                var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(goAssetFile);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(monoScriptFile);
                if (monoScript == null || targetPrefab == null) return;
                var monoType = monoScript.GetClass();
                targetPrefab.GetOrAddComponent(monoType);
            }
        }

        [MenuItem("Assets/YF Tools/Clear Prefabs Missing Scripts", priority = 201)]
        static void ClearMissingScripts()
        {
            var selectObjs = Selection.objects;
            int totalCount = selectObjs.Length;
            for (int i = 0; i < totalCount; i++)
            {
                var item = selectObjs[i];
                EditorUtility.DisplayProgressBar($"Clear missing scripts: [{i}/{totalCount}]", $"清理{item.name}丢失脚本:", i / (float)totalCount);
                var path = AssetDatabase.GetAssetPath(item);
                if (Directory.Exists(path))
                {
                    var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
                    foreach (var guid in prefabs)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        ClearPrefabMissingComponents(assetPath);
                    }
                }
                else if (File.Exists(path) && Path.GetExtension(path).ToLower().CompareTo(".prefab") == 0)
                {
                    ClearPrefabMissingComponents(path);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static void ClearPrefabMissingComponents(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab || type == PrefabAssetType.Variant)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabRoot.GetComponentsInChildren<Transform>(true);
            bool isDirty = false;
            foreach (var node in nodes)
            {
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject) > 0)
                {
                    isDirty = true;
                }
            }
            if (isDirty)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabPath, InteractionMode.AutomatedAction);
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        [MenuItem("Assets/YF Tools/Log Asset Dependencies", priority = 101)]
        static void LogAssetDependencies()
        {
            if (Selection.activeObject == null) return;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path)) return;

            var dependencies = AssetDatabase.GetDependencies(path);
            Debug.Log($"----------------{path} Dependencies---------------");
            foreach (var dependency in dependencies)
            {
                Debug.Log(dependency);
            }
            Debug.Log($"--------------------------------------------------");
        }
    }
}

