using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Enxada.EditorTools
{
    /// <summary>Helpers dos scripts de setup.</summary>
    public static class SetupUtil
    {
        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Carrega o ScriptableObject do caminho ou cria um novo (rodando init). Nunca sobrescreve.</summary>
        public static T EnsureAsset<T>(string path, Action<T> init = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
