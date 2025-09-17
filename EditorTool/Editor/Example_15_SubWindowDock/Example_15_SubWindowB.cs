using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ToolKits
{
    public class Example_15_SubWindowB :OdinEditorWindow
    {
        public static Example_15_SubWindowB PopUp()
        {
            var window = GetWindow<Example_15_SubWindowB>("子窗口B");
            window.Show();
            return window;
        }
    }   
}
