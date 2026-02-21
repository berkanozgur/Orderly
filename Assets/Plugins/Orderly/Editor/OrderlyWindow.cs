using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if UNITY_VFX_GRAPH
using UnityEngine.VFX;
#endif

namespace Orderly
{
    /// <summary>
    /// Main Orderly Editor Window — Tools > Orderly
    /// Tabs: Folders | Rules | Create
    /// </summary>
    public class OrderlyWindow : EditorWindow
    {
        private OrderlyTemplate _activeTemplate;
        private int _activeTab = 0;
        private static readonly string[] TabLabels = { "📁  Folders", "⚙️  Rules", "✨  Create" };

        private Vector2 _folderScroll;
        private Vector2 _rulesScroll;
        private Vector2 _createScroll;
        private Vector2 _logScroll;

        private List<string> _log = new List<string>();
        private bool _showLog;

        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        private OrderlyAssetType _pickerAssetType;
        private List<string> _pickerFolders = new List<string>();
        private bool _showPicker;
        private Vector2 _pickerScroll;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _logStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _ruleRowStyle;
        private bool _stylesInitialized;

        private static readonly Color BgDark = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color BgCard = new Color(0.23f, 0.23f, 0.23f);
        private static readonly Color BgPicker = new Color(0.14f, 0.14f, 0.14f);
        private static readonly Color AccentBlue = new Color(0.25f, 0.60f, 1.00f);
        private static readonly Color AccentGreen = new Color(0.30f, 0.85f, 0.50f);
        private static readonly Color AccentRed = new Color(0.90f, 0.35f, 0.35f);
        private static readonly Color AccentGray = new Color(0.45f, 0.45f, 0.45f);

        private struct AssetTypeMeta
        {
            public string label;
            public string icon;
            public string extension; 
        }

        private static readonly Dictionary<OrderlyAssetType, AssetTypeMeta> AssetMeta =
            new Dictionary<OrderlyAssetType, AssetTypeMeta>
        {
            { OrderlyAssetType.MonoBehaviour,      new AssetTypeMeta { label = "MonoBehaviour",       icon = "cs Script Icon",          extension = ".cs"           }},
            { OrderlyAssetType.Material,           new AssetTypeMeta { label = "Material",            icon = "Material Icon",           extension = ".mat"          }},
            { OrderlyAssetType.AnimatorController, new AssetTypeMeta { label = "Animator Controller", icon = "AnimatorController Icon", extension = ".controller"   }},
            { OrderlyAssetType.AnimationClip,      new AssetTypeMeta { label = "Animation Clip",      icon = "Animation Icon",          extension = ".anim"         }},
            { OrderlyAssetType.VFXGraph,           new AssetTypeMeta { label = "VFX Graph",           icon = "VisualEffectAsset Icon",  extension = ".vfx"          }},
            { OrderlyAssetType.ShaderGraph,        new AssetTypeMeta { label = "Shader Graph",        icon = "Shader Icon",             extension = ".shadergraph"  }},
        };

        // ── Menu Item ──────────────────────────────────────────────────────────

        [MenuItem("Tools/Orderly", priority = 50)]
        public static void Open()
        {
            var w = GetWindow<OrderlyWindow>("Orderly");
            w.minSize = new Vector2(440f, 580f);
            w.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Orderly", EditorGUIUtility.IconContent("Folder Icon").image);
            TryLoadLastTemplate();
        }

        private void OnGUI()
        {
            InitStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BgDark);

            EditorGUILayout.Space(12f);
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawTemplateSelector();
            EditorGUILayout.Space(8f);

            if (_activeTemplate == null)
            {
                DrawNoTemplateHint();
                return;
            }

            DrawTabs();
            EditorGUILayout.Space(6f);

            switch (_activeTab)
            {
                case 0: DrawFoldersTab(); break;
                case 1: DrawRulesTab(); break;
                case 2: DrawCreateTab(); break;
            }

            DrawLog();
            EditorGUILayout.Space(12f);

            if (_showPicker) DrawSubfolderPicker();
        }


        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            GUILayout.Label("📁  Orderly", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Folder Structure Manager", _subHeaderStyle);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();

            Rect divider = GUILayoutUtility.GetRect(0, 1f, GUILayout.ExpandWidth(true));
            divider.x = 16f; divider.width -= 32f;
            EditorGUI.DrawRect(divider, AccentBlue * 0.6f);
        }

        private void DrawTemplateSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.LabelField("Template", GUILayout.Width(68f));

            var prev = _activeTemplate;
            _activeTemplate = (OrderlyTemplate)EditorGUILayout.ObjectField(
                _activeTemplate, typeof(OrderlyTemplate), false);
            if (_activeTemplate != prev) { SaveLastTemplatePath(); _log.Clear(); }

            if (ColorButton("New", AccentBlue, 48f)) CreateNewTemplate();
            if (ColorButton("Default", AccentGray, 60f)) LoadDefaultIntoTemplate();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            for (int i = 0; i < TabLabels.Length; i++)
            {
                Color bg = i == _activeTab ? AccentBlue : AccentGray * 0.7f;
                if (ColorButton(TabLabels[i], bg, 120f, 26f))
                    _activeTab = i;
                GUILayout.Space(4f);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Folders ───────────────────────────────────────────────────────

        private void DrawFoldersTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.LabelField("Folder Structure", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (ColorButton("+  Add Root Folder", AccentBlue, 134f))
            {
                Undo.RecordObject(_activeTemplate, "Add Folder");
                _activeTemplate.folders.Add(new OrderlyFolder("NewFolder", Color.white));
                EditorUtility.SetDirty(_activeTemplate);
            }
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);

            _folderScroll = EditorGUILayout.BeginScrollView(_folderScroll,
                GUILayout.MaxHeight(position.height - 300f));

            int removeAt = -1;
            for (int i = 0; i < _activeTemplate.folders.Count; i++)
                if (DrawFolderRow(_activeTemplate.folders[i], 0, i.ToString())) removeAt = i;

            if (removeAt >= 0)
            {
                Undo.RecordObject(_activeTemplate, "Remove Folder");
                _activeTemplate.folders.RemoveAt(removeAt);
                EditorUtility.SetDirty(_activeTemplate);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(8f);
            DrawFolderActionButtons();
        }

        private bool DrawFolderRow(OrderlyFolder folder, int depth, string uid)
        {
            float indent = 16f + depth * 20f;
            bool shouldRemove = false;

            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(indent - 4f, rowRect.y, position.width - indent - 12f, 22f), BgCard);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
            GUILayout.Space(indent);

            if (!_foldouts.ContainsKey(uid)) _foldouts[uid] = true;
            if (folder.children.Count > 0)
                _foldouts[uid] = EditorGUILayout.Foldout(_foldouts[uid], "");
            else
                GUILayout.Space(14f);

            Color newColor = EditorGUILayout.ColorField(GUIContent.none, folder.labelColor,
                false, false, false, GUILayout.Width(28f), GUILayout.Height(18f));
            if (newColor != folder.labelColor)
            {
                Undo.RecordObject(_activeTemplate, "Change Color");
                folder.labelColor = newColor;
                EditorUtility.SetDirty(_activeTemplate);
            }

            GUILayout.Space(4f);
            string newName = EditorGUILayout.TextField(folder.name, GUILayout.ExpandWidth(true));
            if (newName != folder.name)
            {
                Undo.RecordObject(_activeTemplate, "Rename Folder");
                folder.name = newName;
                EditorUtility.SetDirty(_activeTemplate);
            }

            if (ColorButton("+", AccentGreen, 24f))
            {
                Undo.RecordObject(_activeTemplate, "Add Child");
                folder.children.Add(new OrderlyFolder("NewFolder", Color.white));
                _foldouts[uid] = true;
                EditorUtility.SetDirty(_activeTemplate);
            }
            if (ColorButton("✕", AccentRed, 24f)) shouldRemove = true;
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(2f);

            if (folder.children.Count > 0 && _foldouts.TryGetValue(uid, out bool open) && open)
            {
                int removeChild = -1;
                for (int i = 0; i < folder.children.Count; i++)
                    if (DrawFolderRow(folder.children[i], depth + 1, $"{uid}_{i}")) removeChild = i;
                if (removeChild >= 0)
                {
                    Undo.RecordObject(_activeTemplate, "Remove Child");
                    folder.children.RemoveAt(removeChild);
                    EditorUtility.SetDirty(_activeTemplate);
                }
            }

            return shouldRemove;
        }

        private void DrawFolderActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            if (ColorButton("⚡  Generate Folders", AccentGreen, 160f, 30f))
            {
                _log.Clear();
                var r = OrderlyFolderGenerator.Generate(_activeTemplate);
                _log.AddRange(r.log);
                _showLog = true;
            }
            GUILayout.Space(6f);
            if (ColorButton("🎨  Reapply Colors", AccentBlue, 140f, 30f))
            {
                OrderlyFolderGenerator.ReapplyColors(_activeTemplate);
                _log.Clear(); _log.Add("✅ Colors reapplied."); _showLog = true;
            }
            GUILayout.Space(6f);
            if (ColorButton("Clear Colors", AccentGray, 100f, 30f))
            {
                OrderlyFolderPainter.ClearAll();
                _log.Clear(); _log.Add("🧹 Colors cleared."); _showLog = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRulesTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.LabelField("Asset Creation Rules", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            GUILayout.Label(
                "Map each asset type to a folder from the current template.\n" +
                "Only folders that exist in the structure above are selectable.",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);

            var folderPaths = new List<string> { "— None —" };
            CollectFolderPaths(_activeTemplate.folders, "Assets", folderPaths);

            _rulesScroll = EditorGUILayout.BeginScrollView(_rulesScroll);

            foreach (OrderlyAssetType type in System.Enum.GetValues(typeof(OrderlyAssetType)))
            {
                DrawRuleRow(type, folderPaths);
                GUILayout.Space(2f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRuleRow(OrderlyAssetType type, List<string> folderPaths)
        {
            var meta = AssetMeta[type];
            string currentPath = _activeTemplate.GetRulePath(type) ?? "";

            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(16f, rowRect.y, position.width - 32f, 26f), BgCard);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(26f));
            GUILayout.Space(20f);

            // Icon
            var icon = EditorGUIUtility.IconContent(meta.icon);
            GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));
            GUILayout.Space(4f);

            // Label
            GUILayout.Label(meta.label, GUILayout.Width(150f));

            // Arrow
            GUILayout.Label("→", GUILayout.Width(20f));

            // Folder dropdown
            int currentIndex = string.IsNullOrEmpty(currentPath) ? 0 : folderPaths.IndexOf(currentPath);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, folderPaths.ToArray(), GUILayout.ExpandWidth(true));
            if (newIndex != currentIndex)
            {
                Undo.RecordObject(_activeTemplate, "Set Asset Rule");
                string selected = newIndex == 0 ? "" : folderPaths[newIndex];
                _activeTemplate.SetRulePath(type, selected);
                EditorUtility.SetDirty(_activeTemplate);
            }

            GUILayout.Space(20f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCreateTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.LabelField("Create Assets", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            GUILayout.Label("Assets are created in the mapped folder. You'll pick a subfolder first.",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);

            _createScroll = EditorGUILayout.BeginScrollView(_createScroll);

            foreach (OrderlyAssetType type in System.Enum.GetValues(typeof(OrderlyAssetType)))
            {
                DrawCreateRow(type);
                GUILayout.Space(4f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCreateRow(OrderlyAssetType type)
        {
            var meta = AssetMeta[type];
            string root = _activeTemplate.GetRulePath(type);
            bool hasRule = !string.IsNullOrEmpty(root);

            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(16f, rowRect.y, position.width - 32f, 30f), BgCard);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(30f));
            GUILayout.Space(20f);

            var icon = EditorGUIUtility.IconContent(meta.icon);
            GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));
            GUILayout.Space(6f);
            GUILayout.Label(meta.label, GUILayout.Width(160f));
            GUILayout.Label(hasRule ? $"→  {root}" : "→  No rule set", EditorStyles.miniLabel,
                GUILayout.ExpandWidth(true));

            Color btnColor = hasRule ? AccentGreen : AccentGray;
            GUI.enabled = hasRule;
            if (ColorButton("Create", btnColor, 60f, 22f))
                OpenSubfolderPicker(type, root);
            GUI.enabled = true;

            GUILayout.Space(20f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        private void OpenSubfolderPicker(OrderlyAssetType type, string rootPath)
        {
            _pickerAssetType = type;
            _pickerFolders = GetSubfolders(rootPath);
            _showPicker = true;
            _pickerScroll = Vector2.zero;
        }

        private void DrawSubfolderPicker()
        {
            float logReserved = (_showLog && _log.Count > 0)
                ? Mathf.Min(_log.Count * 18f + 12f, 100f) + 24f
                : 0f;
            float pickerHeight = Mathf.Min(_pickerFolders.Count * 24f + 80f, 280f);
            float yStart = position.height - pickerHeight - logReserved - 16f;

            Rect bg = new Rect(16f, yStart, position.width - 32f, pickerHeight);
            EditorGUI.DrawRect(bg, BgPicker);
            EditorGUI.DrawRect(new Rect(bg.x, bg.y, bg.width, 1f), AccentBlue);

            GUILayout.BeginArea(bg);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            GUILayout.Label($"Pick subfolder for {AssetMeta[_pickerAssetType].label}:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (ColorButton("✕", AccentRed, 24f, 20f)) _showPicker = false;
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            _pickerScroll = EditorGUILayout.BeginScrollView(_pickerScroll,
                GUILayout.Height(pickerHeight - 68f));

            foreach (string folder in _pickerFolders)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10f);
                string display = folder.Replace("Assets/", "");
                if (GUILayout.Button(display, EditorStyles.miniButton))
                {
                    _showPicker = false;
                    CreateAssetInFolder(_pickerAssetType, folder);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(6f);
            GUILayout.EndArea();
        }


        private void CreateAssetInFolder(OrderlyAssetType type, string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                _log.Add($"❌ Folder does not exist: {folderPath}. Run Generate Folders first.");
                _showLog = true;
                return;
            }

            switch (type)
            {
                case OrderlyAssetType.MonoBehaviour:
                    CreateScript(folderPath);
                    break;

                case OrderlyAssetType.Material:
                    CreateMaterial(folderPath);
                    break;

                case OrderlyAssetType.AnimatorController:
                    CreateAnimatorController(folderPath);
                    break;

                case OrderlyAssetType.AnimationClip:
                    CreateSimpleAsset<AnimationClip>(folderPath, "NewAnimation", "anim");
                    break;

                case OrderlyAssetType.VFXGraph:
                    TryCreateVFXGraph(folderPath);
                    break;

                case OrderlyAssetType.ShaderGraph:
                    TryCreateShaderGraph(folderPath);
                    break;
            }
        }
        private void CreateMaterial(string folderPath)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewMaterial.mat");
            var material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("HDRP/Lit"));
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(material);
            _log.Add($"✅ Created: {path}"); _showLog = true;
        }
        private void CreateScript(string folderPath)
        {
            string name = EditorUtility.SaveFilePanel("Create Script", folderPath, "NewScript", "cs");
            if (string.IsNullOrEmpty(name)) return;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (name.StartsWith(projectRoot))
                name = name.Substring(projectRoot.Length + 1).Replace("\\", "/");

            string className = Path.GetFileNameWithoutExtension(name);
            string content =
                $"using UnityEngine;\n\npublic class {className} : MonoBehaviour\n{{\n    // Start is called before the first frame update\n    void Start()\n    {{\n        \n    }}\n\n    // Update is called once per frame\n    void Update()\n    {{\n        \n    }}\n}}\n";

            File.WriteAllText(Path.Combine(projectRoot, name), content);
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(name);
            if (asset) EditorGUIUtility.PingObject(asset);
            _log.Add($"✅ Script created: {name}"); _showLog = true;
        }

        private void CreateSimpleAsset<T>(string folderPath, string defaultName, string ext) where T : Object, new()
        {
            string path = $"{folderPath}/{defaultName}.{ext}";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var asset = new T();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            _log.Add($"✅ Created: {path}"); _showLog = true;
        }

        private void CreateAnimatorController(string folderPath)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewAnimatorController.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            EditorGUIUtility.PingObject(controller);
            _log.Add($"✅ Created: {path}"); _showLog = true;
        }

        private void TryCreateVFXGraph(string folderPath)
        {
#if UNITY_VFX_GRAPH
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewVFXGraph.vfx");
            var graph = ScriptableObject.CreateInstance<VisualEffectAsset>();
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(graph);
            _log.Add($"✅ Created: {path}");
#else
            _log.Add("❌ VFX Graph package not installed. Install via Package Manager.");
#endif
            _showLog = true;
        }

        private void TryCreateShaderGraph(string folderPath)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewShaderGraph.shadergraph");
            Object folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folderAsset) Selection.activeObject = folderAsset;

            EditorApplication.ExecuteMenuItem("Assets/Create/Shader Graph/URP/Lit Shader Graph");
            _log.Add("ℹ️ Shader Graph creation triggered. Rename the new asset as needed.");
            _showLog = true;
        }

        /// <summary>
        /// Returns a list of all subfolders (including the root itself) under rootPath,
        /// based on what actually exists in the project.
        /// </summary>
        private static List<string> GetSubfolders(string rootPath)
        {
            var result = new List<string>();
            if (AssetDatabase.IsValidFolder(rootPath)) result.Add(rootPath);
            CollectExistingSubfolders(rootPath, result);
            return result;
        }

        private static void CollectExistingSubfolders(string path, List<string> results)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:folder", new[] { path }))
            {
                string sub = AssetDatabase.GUIDToAssetPath(guid);
                if (sub != path && !results.Contains(sub))
                    results.Add(sub);
            }
        }

        /// <summary>Recursively builds a flat list of all template folder paths.</summary>
        private static void CollectFolderPaths(List<OrderlyFolder> folders, string parent, List<string> result)
        {
            foreach (var f in folders)
            {
                string path = $"{parent}/{f.name}";
                result.Add(path);
                if (f.children.Count > 0)
                    CollectFolderPaths(f.children, path, result);
            }
        }

        private void DrawLog()
        {
            if (_log.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            _showLog = EditorGUILayout.Foldout(_showLog, $"Log ({_log.Count})", true);
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();

            if (!_showLog) return;

            float logHeight = Mathf.Min(_log.Count * 18f + 12f, 100f);
            Rect logBgRect = GUILayoutUtility.GetRect(0f, logHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(logBgRect, new Color(0.12f, 0.12f, 0.12f));
            _logScroll = GUI.BeginScrollView(logBgRect, _logScroll,
                new Rect(0, 0, logBgRect.width - 16f, Mathf.Max(_log.Count * 18f + 12f, logHeight)));
            GUILayout.Space(4f);
            foreach (var line in _log)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label(line, _logStyle ?? EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(4f);
            GUI.EndScrollView();
        }


        private void DrawNoTemplateHint()
        {
            GUILayout.Space(40f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("No template selected.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("Create a new one or pick an existing Orderly Template asset.",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (ColorButton("Create New Template", AccentBlue, 180f, 30f)) CreateNewTemplate();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewTemplate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Orderly Template", "OrderlyTemplate", "asset", "Choose save location.");
            if (string.IsNullOrEmpty(path)) return;

            var t = CreateInstance<OrderlyTemplate>();
            t.templateName = Path.GetFileNameWithoutExtension(path);
            t.folders = OrderlyTemplate.GetDefaultFolders();
            t.assetRules = OrderlyTemplate.GetDefaultRules();

            AssetDatabase.CreateAsset(t, path);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            _activeTemplate = t;
            SaveLastTemplatePath();
            EditorGUIUtility.PingObject(t);
        }

        private void LoadDefaultIntoTemplate()
        {
            if (_activeTemplate == null) return;
            if (!EditorUtility.DisplayDialog("Load Defaults",
                "Replace the current folder list and rules with defaults?", "Yes", "Cancel")) return;
            Undo.RecordObject(_activeTemplate, "Load Defaults");
            _activeTemplate.folders = OrderlyTemplate.GetDefaultFolders();
            _activeTemplate.assetRules = OrderlyTemplate.GetDefaultRules();
            EditorUtility.SetDirty(_activeTemplate);
        }

        private void SaveLastTemplatePath()
        {
            if (_activeTemplate == null) return;
            EditorPrefs.SetString("Orderly_LastTemplate", AssetDatabase.GetAssetPath(_activeTemplate));
        }

        private void TryLoadLastTemplate()
        {
            string path = EditorPrefs.GetString("Orderly_LastTemplate", "");
            if (!string.IsNullOrEmpty(path))
                _activeTemplate = AssetDatabase.LoadAssetAtPath<OrderlyTemplate>(path);
        }

        private bool ColorButton(string label, Color color, float width, float height = 22f)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool result = GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = prev;
            return result;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            _subHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleRight
            };
            _logStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.75f, 0.85f, 0.75f) }
            };
        }
    }
}