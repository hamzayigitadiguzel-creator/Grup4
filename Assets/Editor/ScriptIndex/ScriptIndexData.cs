using System;
using System.Collections.Generic;

namespace Storia.Editor.ScriptIndex
{
    /// <summary>
    /// JSON çıktısı için veri modelleri.
    /// </summary>
    [Serializable]
    public sealed class ScriptIndexOutput
    {
        public string generatedAtUtc;
        public string root;
        public List<ScriptFileInfo> files;
        public TreeNode tree;
    }

    [Serializable]
    public sealed class ScriptFileInfo
    {
        public string path;
        public string name;
        public string folder;

        public ScriptFileInfo(string path, string name, string folder)
        {
            this.path = path;
            this.name = name;
            this.folder = folder;
        }
    }

    [Serializable]
    public sealed class TreeNode
    {
        public string name;
        public string type; // "folder" veya "file"
        public string path; // Unity relative path
        public List<TreeNode> children;

        public TreeNode(string name, string type, string path)
        {
            this.name = name;
            this.type = type;
            this.path = path;
            this.children = new List<TreeNode>();
        }
    }
}
