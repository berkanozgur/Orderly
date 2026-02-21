using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orderly
{
    /// <summary>
    /// Creatable asset types that Orderly can route to specific folders.
    /// </summary>
    public enum OrderlyAssetType
    {
        MonoBehaviour = 0,
        Material = 1,
        AnimatorController = 2,
        AnimationClip = 3,
        VFXGraph = 4,
        ShaderGraph = 5,
    }

    /// <summary>
    /// Maps one creatable asset type to a folder path (Assets-relative).
    /// e.g. MonoBehaviour → "Assets/Scripts"
    /// </summary>
    [Serializable]
    public class OrderlyAssetRule
    {
        public OrderlyAssetType assetType;
        /// <summary>Assets-relative path, e.g. "Assets/Scripts". Empty = rule inactive.</summary>
        public string targetFolderPath = "";
    }

    /// <summary>
    /// A single folder definition within an Orderly template.
    /// </summary>
    [Serializable]
    public class OrderlyFolder
    {
        public string name = "NewFolder";
        public Color labelColor = Color.white;
        public List<OrderlyFolder> children = new List<OrderlyFolder>();

        public OrderlyFolder() { }

        public OrderlyFolder(string name, Color color)
        {
            this.name = name;
            this.labelColor = color;
        }
    }

    /// <summary>
    /// A saveable Orderly template that defines a folder structure and asset creation rules.
    /// Create via: Assets > Create > Orderly > Folder Template
    /// </summary>
    [CreateAssetMenu(fileName = "OrderlyTemplate", menuName = "Orderly/Folder Template", order = 1)]
    public class OrderlyTemplate : ScriptableObject
    {
        [Tooltip("Friendly name shown in the Orderly window.")]
        public string templateName = "My Template";

        [Tooltip("Root-level folders to create under Assets/.")]
        public List<OrderlyFolder> folders = new List<OrderlyFolder>();

        [Tooltip("Maps creatable asset types to target folders.")]
        public List<OrderlyAssetRule> assetRules = new List<OrderlyAssetRule>();

        /// <summary>Returns the target folder path for a given asset type, or null if unset.</summary>
        public string GetRulePath(OrderlyAssetType type)
        {
            foreach (var rule in assetRules)
                if (rule.assetType == type && !string.IsNullOrEmpty(rule.targetFolderPath))
                    return rule.targetFolderPath;
            return null;
        }

        /// <summary>Sets or creates the rule for an asset type.</summary>
        public void SetRulePath(OrderlyAssetType type, string path)
        {
            foreach (var rule in assetRules)
            {
                if (rule.assetType == type) { rule.targetFolderPath = path; return; }
            }
            assetRules.Add(new OrderlyAssetRule { assetType = type, targetFolderPath = path });
        }

        public static List<OrderlyFolder> GetDefaultFolders()
        {
            return new List<OrderlyFolder>
            {
                new OrderlyFolder("Scripts",    new Color(0.40f, 0.76f, 1.00f)),
                new OrderlyFolder("Prefabs",    new Color(0.60f, 1.00f, 0.60f)),
                new OrderlyFolder("Scenes",     new Color(1.00f, 0.85f, 0.35f)),
                new OrderlyFolder("Textures",   new Color(1.00f, 0.60f, 0.40f)),
                new OrderlyFolder("Materials",  new Color(0.80f, 0.50f, 1.00f)),
                new OrderlyFolder("Audio",      new Color(1.00f, 0.55f, 0.75f)),
                new OrderlyFolder("Animations", new Color(0.40f, 1.00f, 0.90f)),
                new OrderlyFolder("Models",     new Color(0.90f, 0.70f, 0.50f)),
                new OrderlyFolder("Plugins",    new Color(0.65f, 0.65f, 0.65f)),
            };
        }

        public static List<OrderlyAssetRule> GetDefaultRules()
        {
            return new List<OrderlyAssetRule>
            {
                new OrderlyAssetRule { assetType = OrderlyAssetType.MonoBehaviour,      targetFolderPath = "Assets/Scripts"    },
                new OrderlyAssetRule { assetType = OrderlyAssetType.Material,           targetFolderPath = "Assets/Materials"  },
                new OrderlyAssetRule { assetType = OrderlyAssetType.AnimatorController, targetFolderPath = "Assets/Animations" },
                new OrderlyAssetRule { assetType = OrderlyAssetType.AnimationClip,      targetFolderPath = "Assets/Animations" },
                new OrderlyAssetRule { assetType = OrderlyAssetType.VFXGraph,           targetFolderPath = ""                  },
                new OrderlyAssetRule { assetType = OrderlyAssetType.ShaderGraph,        targetFolderPath = ""                  },
            };
        }
    }
}