using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ToolKits
{
    public class Example_11_ShowButtonEditorWindow : OdinEditorWindow
    {
        private static Example_11_ShowButtonEditorWindow _window;
        private static readonly Vector2 MIN_SIZE = new Vector2(400, 300);
        
        [SerializeField] private float iconSize = 32f;

        [MenuItem("Tools/Examples/ShowButtonWindow", priority = 11)]
        private static void PopUp()
        {
            _window = GetWindow<Example_11_ShowButtonEditorWindow>();
            _window.minSize = MIN_SIZE;
            _window.Show();
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();
            
            GUILayout.Label("不同图标大小调整方法演示:", EditorStyles.boldLabel);
            
            iconSize = EditorGUILayout.Slider("图标大小", iconSize, 16f, 64f);
            
            GUILayout.Space(10);
            
            // 演示不同方法
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("方法1 - EditorGUIUtility.SetIconSize:");
                ShowButton(GUILayoutUtility.GetRect(iconSize, iconSize));
            }
            
            GUILayout.Space(5);
            
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("方法2 - 自定义GUIContent:");
                ShowButton_Method2(GUILayoutUtility.GetRect(iconSize, iconSize));
            }
            
            GUILayout.Space(5);
            
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("方法3 - 控制Rect大小:");
                ShowButton_Method3(GUILayoutUtility.GetRect(iconSize, iconSize), iconSize);
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label("提示: 拖动滑条调整图标大小", EditorStyles.helpBox);
        }

        private void ShowButton(Rect rect)
        {
            // 方法1: 使用 EditorGUIUtility.IconContent 重载方法指定大小
            var iconContent = EditorGUIUtility.IconContent("d_Settings Icon");
            
            // 方法2: 通过全局设置调整图标大小
            var originalIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(10, 10));
            
            if (GUI.Button(rect, iconContent))
            {
                Application.OpenURL("https://www.baidu.com");
            }
            
            // 恢复原始图标大小
            EditorGUIUtility.SetIconSize(originalIconSize);
        }

        private void ShowButton_Method2(Rect rect)
        {
            // 方法3: 创建自定义 GUIContent 并调整图片
            var icon = EditorGUIUtility.IconContent("d_Settings Icon").image;
            var customContent = new GUIContent();
            
            // 创建指定大小的图标
            if (icon != null)
            {
                // 可以通过 TextureScale 或其他方式调整图片大小
                customContent.image = icon;
                customContent.tooltip = "设置";
            }
            
            if (GUI.Button(rect, customContent))
            {
                Application.OpenURL("https://www.baidu.com");
            }
        }

        private void ShowButton_Method3(Rect rect, float iconSize = 24f)
        {
            // 方法4: 通过样式控制图标大小
            var style = new GUIStyle(GUI.skin.button);
            style.imagePosition = ImagePosition.ImageOnly;
            
            // 调整按钮的固定大小来控制图标显示大小
            var iconRect = new Rect(rect.x, rect.y, iconSize, iconSize);
            
            if (GUI.Button(iconRect, EditorGUIUtility.IconContent("d_Settings Icon"), style))
            {
                Application.OpenURL("https://www.baidu.com");
            }
        }
    }
}