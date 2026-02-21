using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Orderly
{
    /// <summary>
    /// Hooks into the Project window's item GUI to draw a colored label
    /// behind any folder that has a registered color.
    /// Colors are stored in EditorPrefs so they persist across sessions.
    /// </summary>
    [InitializeOnLoad]
    public static class OrderlyFolderPainter
    {
        private static readonly Dictionary<string, Color> _colorMap = new Dictionary<string, Color>();

        private const string PrefsKey = "Orderly_FolderColors";

        static OrderlyFolderPainter()
        {
            LoadColors();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        public static void RegisterColor(string assetPath, Color color)
        {
            _colorMap[assetPath] = color;
            SaveColors();
            EditorApplication.RepaintProjectWindow();
        }

        public static void UnregisterColor(string assetPath)
        {
            if (_colorMap.Remove(assetPath))
            {
                SaveColors();
                EditorApplication.RepaintProjectWindow();
            }
        }

        public static void ClearAll()
        {
            _colorMap.Clear();
            EditorPrefs.DeleteKey(PrefsKey);
            EditorApplication.RepaintProjectWindow();
        }

        public static Color? GetColor(string assetPath)
        {
            return _colorMap.TryGetValue(assetPath, out var c) ? c : (Color?)null;
        }

        public static Dictionary<string, Color> GetAllColors() => new Dictionary<string, Color>(_colorMap);

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;
            if (!AssetDatabase.IsValidFolder(path)) return;
            if (!_colorMap.TryGetValue(path, out Color color)) return;

            bool isListView = selectionRect.height <= 20f;

            Color bg = color;
            bg.a = 0.25f;

            Color accent = color;
            accent.a = 0.85f;

            if (isListView)
            {
                Rect bar = new Rect(selectionRect.x, selectionRect.y, 3f, selectionRect.height);
                EditorGUI.DrawRect(bar, accent);

                Rect bgRect = new Rect(selectionRect.x + 3f, selectionRect.y,
                                       selectionRect.width - 3f, selectionRect.height);
                EditorGUI.DrawRect(bgRect, bg);
            }
            else
            {
                Rect band = new Rect(selectionRect.x, selectionRect.yMax - 4f,
                                     selectionRect.width, 4f);
                EditorGUI.DrawRect(band, accent);
            }
        }

        private static void SaveColors()
        {
            // Simple CSV format: path|r|g|b|a,path|r|g|b|a,...
            var parts = new List<string>();
            foreach (var kv in _colorMap)
            {
                Color c = kv.Value;
                parts.Add($"{kv.Key}|{c.r}|{c.g}|{c.b}|{c.a}");
            }
            EditorPrefs.SetString(PrefsKey, string.Join(",", parts));
        }

        private static void LoadColors()
        {
            _colorMap.Clear();
            string raw = EditorPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw)) return;

            foreach (string entry in raw.Split(','))
            {
                string[] parts = entry.Split('|');
                if (parts.Length != 5) continue;
                if (!float.TryParse(parts[1], out float r)) continue;
                if (!float.TryParse(parts[2], out float g)) continue;
                if (!float.TryParse(parts[3], out float b)) continue;
                if (!float.TryParse(parts[4], out float a)) continue;
                _colorMap[parts[0]] = new Color(r, g, b, a);
            }
        }
    }
}
