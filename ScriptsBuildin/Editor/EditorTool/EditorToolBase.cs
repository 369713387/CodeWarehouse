using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace YF.EditorTools
{
    public abstract class EditorToolBase : OdinEditorWindow
    {
        public abstract string ToolName { get; }
        public abstract Vector2Int WinSize { get; }

        protected override void OnEnable()
        {
            base.OnEnable();

            this.titleContent = new GUIContent(ToolName);
            this.position.Set(this.position.x, this.position.y, this.WinSize.x, this.WinSize.y);
        }

    }
}

