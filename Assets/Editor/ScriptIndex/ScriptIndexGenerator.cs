using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Storia.Editor.ScriptIndex
{
    /// <summary>
    /// Assets/Scripts altındaki tüm .cs dosyalarını tarayarak JSON indeks oluşturur.
    /// </summary>
    public sealed class ScriptIndexGenerator
    {
        private const string ScriptsRootFolder = "Assets/Scripts";
        private const string OutputFilePath = "Assets/Docs/scripts-index.json";

        /// <summary>
        /// Tarama işlemini başlat ve JSON dosyasını oluştur.
        /// </summary>
        public static void Generate()
        {
            try
            {
                // Assets/Scripts klasörünün varlığını kontrol et
                if (!Directory.Exists(ScriptsRootFolder))
                {
                    UnityEngine.Debug.LogError($"[ScriptIndexGenerator] Klasör bulunamadı: {ScriptsRootFolder}");
                    return;
                }

                // Tüm .cs dosyalarını tara
                List<ScriptFileInfo> files = ScanScriptFiles();

                // Ağaç yapısını oluştur
                TreeNode tree = BuildTree(files);

                // Çıktı nesnesini oluştur
                ScriptIndexOutput output = new ScriptIndexOutput
                {
                    generatedAtUtc = DateTime.UtcNow.ToString("o"), // ISO 8601
                    root = ScriptsRootFolder,
                    files = files,
                    tree = tree
                };

                // JSON'a serileştir ve dosyaya yaz
                WriteJsonToFile(output);

                // Unity AssetDatabase'i yenile
                AssetDatabase.Refresh();

                // Özet log
                int folderCount = CountFolders(tree);
                UnityEngine.Debug.Log($"[ScriptIndexGenerator] İndeks oluşturuldu:\n" +
                          $"  • Dosya: {OutputFilePath}\n" +
                          $"  • Toplam klasör: {folderCount}\n" +
                          $"  • Toplam dosya: {files.Count}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ScriptIndexGenerator] Hata oluştu: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static List<ScriptFileInfo> ScanScriptFiles()
        {
            List<ScriptFileInfo> files = new List<ScriptFileInfo>();

            // Assets/Scripts altındaki tüm .cs dosyalarını bul
            string[] allFiles = Directory.GetFiles(ScriptsRootFolder, "*.cs", SearchOption.AllDirectories);

            foreach (string filePath in allFiles)
            {
                // .meta dosyalarını filtrele
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Unity path formatına dönüştür (backslash -> forward slash)
                string unityPath = filePath.Replace('\\', '/');

                // Defansif filtreleme (bin, obj, Library vb.)
                if (IsExcludedPath(unityPath))
                    continue;

                string fileName = Path.GetFileName(unityPath);
                string folderPath = Path.GetDirectoryName(unityPath)?.Replace('\\', '/') ?? "";

                files.Add(new ScriptFileInfo(unityPath, fileName, folderPath));
            }

            // Deterministik sıralama: path'e göre case-insensitive alfabetik
            files.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));

            return files;
        }

        private static bool IsExcludedPath(string path)
        {
            // Defansif olarak dışlanacak path parçaları
            string[] excludedParts = { "/bin/", "/obj/", "/.git/", "/Library/", "/Temp/" };

            foreach (string excluded in excludedParts)
            {
                if (path.Contains(excluded))
                    return true;
            }

            return false;
        }

        private static TreeNode BuildTree(List<ScriptFileInfo> files)
        {
            // Root node
            TreeNode root = new TreeNode("Scripts", "folder", ScriptsRootFolder);

            // Her dosyayı ağaca ekle
            foreach (ScriptFileInfo file in files)
            {
                AddFileToTree(root, file);
            }

            // Ağacı deterministik sırala
            SortTree(root);

            return root;
        }

        private static void AddFileToTree(TreeNode root, ScriptFileInfo file)
        {
            // Dosyanın path'ini parçalara ayır
            // Örnek: Assets/Scripts/Core/Controllers/File.cs
            //  -> ["Core", "Controllers", "File.cs"]
            string relativePath = file.path.Substring(ScriptsRootFolder.Length).TrimStart('/');
            string[] parts = relativePath.Split('/');

            TreeNode current = root;

            // Klasörleri oluştur/bul
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string folderName = parts[i];
                TreeNode child = current.children.Find(n => n.name == folderName && n.type == "folder");

                if (child == null)
                {
                    // Yeni klasör düğümü oluştur
                    string folderPath = ScriptsRootFolder + "/" + string.Join("/", parts.Take(i + 1));
                    child = new TreeNode(folderName, "folder", folderPath);
                    current.children.Add(child);
                }

                current = child;
            }

            // Dosyayı ekle
            string fileName = parts[parts.Length - 1];
            TreeNode fileNode = new TreeNode(fileName, "file", file.path);
            current.children.Add(fileNode);
        }

        private static void SortTree(TreeNode node)
        {
            if (node.children == null || node.children.Count == 0)
                return;

            // Önce klasörler, sonra dosyalar; içlerinde alfabetik
            node.children.Sort((a, b) =>
            {
                // Tip karşılaştırması (folder önce)
                if (a.type != b.type)
                {
                    return a.type == "folder" ? -1 : 1;
                }

                // Aynı tipse alfabetik
                return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            // Alt düğümleri de sırala (recursive)
            foreach (TreeNode child in node.children)
            {
                if (child.type == "folder")
                {
                    SortTree(child);
                }
            }
        }

        private static void WriteJsonToFile(ScriptIndexOutput output)
        {
            // Çıktı klasörünü oluştur (yoksa)
            string outputDir = Path.GetDirectoryName(OutputFilePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // JSON'a serileştir (pretty print)
            string json = JsonUtility.ToJson(output, prettyPrint: true);

            // Dosyaya yaz (UTF-8)
            File.WriteAllText(OutputFilePath, json, System.Text.Encoding.UTF8);
        }

        private static int CountFolders(TreeNode node)
        {
            if (node == null || node.type != "folder")
                return 0;

            int count = 1; // Kendisi

            foreach (TreeNode child in node.children)
            {
                if (child.type == "folder")
                {
                    count += CountFolders(child);
                }
            }

            return count;
        }
    }
}
