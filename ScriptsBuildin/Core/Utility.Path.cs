namespace YF.EditorTools
{
    public static partial class Utility
    {
        public static class Path
        {
            /// <summary>
            /// 获取规范的路径。
            /// </summary>
            /// <param name="path">要规范的路径。</param>
            /// <returns>规范的路径。</returns>
            public static string GetRegularPath(string path)
            {
                if (path == null)
                {
                    return null;
                }
                return path.Replace('\\', '/');
            }
        }
    }
}   
