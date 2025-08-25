using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace YF.EditorTools
{
    [EditorToolMenu("压缩文件", typeof(CompressToolEditor), 1)]
    public class CompressImageFilePanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:sprite t:texture2d t:folder";
        
        public override string DragAreaTips => "拖拽到此处添加文件夹或jpg/png";
        
        public override string ReadmeText => "压缩图片原文件,支持在线/离线压缩,只支持jpg/png";

        private readonly string[] mSupportAssetFormats = { ".png", ".jpg" }; //支持压缩的格式;
        
        private readonly Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        ReorderableList tinypngKeyScrollList;
        
        Vector2 tinypngScrollListPos;
        public override void OnEnter()
        {
            tinypngKeyScrollList = new ReorderableList(EditorToolSettings.Instance.CompressImgToolKeys, typeof(string), true, true, true, true);
            // tinypngKeyScrollList.drawHeaderCallback = DrawTinypngKeyScrollListHeader;
            // tinypngKeyScrollList.drawElementCallback = DrawTinypngKeyItem;
            Debug.Log("进入CompressImageFilePanel");
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override bool IsSupportAsset(string assetPath)
        {
            var format = Path.GetExtension(assetPath).ToLower();
            return mSupportAssetFormats.Contains(format);
        }
        
        public override void DrawSettingsPanel()
        {
            EditorGUI.BeginDisabledGroup(EditorToolSettings.Instance.CompressImgToolOffline);
            {
                tinypngScrollListPos = EditorGUILayout.BeginScrollView(tinypngScrollListPos, GUILayout.Height(110));
                {
                    tinypngKeyScrollList.DoLayoutList();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.BeginHorizontal("box");
            {
                EditorToolSettings.Instance.CompressImgToolOffline = EditorGUILayout.ToggleLeft("离线压缩", EditorToolSettings.Instance.CompressImgToolOffline, GUILayout.Width(100));
                EditorToolSettings.Instance.CompressImgToolCoverRaw = EditorGUILayout.ToggleLeft("覆盖原图片", EditorToolSettings.Instance.CompressImgToolCoverRaw, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CompressImgToolOffline);
                {
                    // EditorGUILayout.MinMaxSlider(Utility.Text.Format("压缩质量({0}%-{1}%)", (int)EditorToolSettings.Instance.CompressImgToolQualityMinLv, (int)EditorToolSettings.Instance.CompressImgToolQualityLv), ref EditorToolSettings.Instance.CompressImgToolQualityMinLv, ref EditorToolSettings.Instance.CompressImgToolQualityLv, 0, 100);
                    //
                    // EditorToolSettings.Instance.CompressImgToolFastLv = EditorGUILayout.IntSlider(Utility.Text.Format("快压等级({0})", EditorToolSettings.Instance.CompressImgToolFastLv), EditorToolSettings.Instance.CompressImgToolFastLv, 1, 10);
                    // EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUI.BeginDisabledGroup(EditorToolSettings.Instance.CompressImgToolCoverRaw);
                {
                    EditorGUILayout.LabelField("输出路径:", GUILayout.Width(80));
                    EditorGUILayout.SelectableLabel(EditorToolSettings.Instance.CompressImgToolOutputDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("选择", GUILayout.Width(80)))
                    {
                        // var backupPath = EditorUtilityExtension.OpenRelativeFolderPanel("选择图片输出路径", EditorToolSettings.Instance.CompressImgToolOutputDir);
                        // EditorToolSettings.Instance.CompressImgToolOutputDir = backupPath;
                        // EditorToolSettings.Save();
                        // GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("打开", GUILayout.Width(80)))
                    {
                        EditorUtility.RevealInFinder(Path.Combine(Directory.GetParent(Application.dataPath).FullName, EditorToolSettings.Instance.CompressImgToolOutputDir));
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("备份路径:", GUILayout.Width(80));
                EditorGUILayout.SelectableLabel(EditorToolSettings.Instance.CompressImgToolBackupDir, EditorStyles.selectionRect, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("选择", GUILayout.Width(80)))
                {
                    // var backupPath = EditorUtilityExtension.OpenRelativeFolderPanel("选择备份路径", EditorToolSettings.Instance.CompressImgToolBackupDir);
                    //
                    // EditorToolSettings.Instance.CompressImgToolBackupDir = backupPath;
                    // EditorToolSettings.Save();
                    // GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("打开", GUILayout.Width(80)))
                {
                    EditorUtility.RevealInFinder(Path.Combine(Directory.GetParent(Application.dataPath).FullName, EditorToolSettings.Instance.CompressImgToolBackupDir));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        public override void DrawBottomButtonsPanel()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("开始压缩", GUILayout.Height(30)))
                {
                    //StartCompress();
                }
                if (GUILayout.Button("备份图片", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    //BackupImages();
                }
                if (GUILayout.Button("还原备份", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    //RecoveryImages();
                }
                if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                {
                    SaveSettings();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}