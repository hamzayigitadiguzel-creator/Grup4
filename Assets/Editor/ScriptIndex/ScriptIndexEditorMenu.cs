using UnityEditor;

namespace Storia.Editor.ScriptIndex
{
    /// <summary>
    /// Unity Editor menü entegrasyonu: Tools/Project/Generate Scripts Index
    /// </summary>
    public static class ScriptIndexEditorMenu
    {
        [MenuItem("Tools/Storia/Generate Scripts Index")]
        private static void GenerateScriptsIndex()
        {
            ScriptIndexGenerator.Generate();
        }
    }
}
