using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Orderly
{
    /// <summary>
    /// Handles the actual folder creation and color registration.
    /// Safe by design: never overwrites or deletes existing folders.
    /// </summary>
    public static class OrderlyFolderGenerator
    {
        public struct GenerationResult
        {
            public int created;
            public int skipped;
            public List<string> log;
        }

        /// <summary>
        /// Generates the full folder tree defined in the template under Assets/.
        /// Returns a result summary for display in the UI.
        /// </summary>
        public static GenerationResult Generate(OrderlyTemplate template)
        {
            var result = new GenerationResult
            {
                log = new List<string>()
            };

            if (template == null)
            {
                result.log.Add("❌ No template provided.");
                return result;
            }

            foreach (var folder in template.folders)
            {
                ProcessFolder(folder, "Assets", ref result);
            }

            AssetDatabase.Refresh();

            result.log.Add($"✅ Done — {result.created} folder(s) created, {result.skipped} already existed.");
            return result;
        }

        private static void ProcessFolder(OrderlyFolder folder, string parentPath, ref GenerationResult result)
        {
            if (string.IsNullOrWhiteSpace(folder.name)) return;

            string folderPath = $"{parentPath}/{folder.name}";

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                result.log.Add($"⏭ Skipped (exists): {folderPath}");
                result.skipped++;
            }
            else
            {
                AssetDatabase.CreateFolder(parentPath, folder.name);
                result.log.Add($"📁 Created: {folderPath}");
                result.created++;
            }

            OrderlyFolderPainter.RegisterColor(folderPath, folder.labelColor);

            foreach (var child in folder.children)
            {
                ProcessFolder(child, folderPath, ref result);
            }
        }

        /// <summary>
        /// Re-applies all folder colors from a template without creating anything.
        /// Useful after reopening a project.
        /// </summary>
        public static void ReapplyColors(OrderlyTemplate template)
        {
            if (template == null) return;
            foreach (var folder in template.folders)
            {
                ReapplyColorsRecursive(folder, "Assets");
            }
            EditorApplication.RepaintProjectWindow();
        }

        private static void ReapplyColorsRecursive(OrderlyFolder folder, string parentPath)
        {
            string folderPath = $"{parentPath}/{folder.name}";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                OrderlyFolderPainter.RegisterColor(folderPath, folder.labelColor);
            }
            foreach (var child in folder.children)
            {
                ReapplyColorsRecursive(child, folderPath);
            }
        }
    }
}
