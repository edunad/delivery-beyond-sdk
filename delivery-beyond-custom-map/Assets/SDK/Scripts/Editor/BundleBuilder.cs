#if UNITY_EDITOR

#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public static class BundleBuilder
    {
        private const string OUTPUT_FOLDER = "Build";

        [MenuItem("HyenaQuest/Maps/Build All Maps")]
        public static void BuildAllMaps()
        {
            var guids = AssetDatabase.FindAssets("t:WorldSettings");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Map Builder", "No WorldSettings assets found in the project.", "OK");
                return;
            }

            BundleBuilder.CleanOutputFolder();

            var builds = new List<AssetBundleBuild>();
            var settingsMap = new Dictionary<string, WorldSettings>();

            foreach (var guid in guids)
            {
                var settingsPath = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(settingsPath);
                var build = BundleBuilder.CreateBuildForPath(settingsPath);

                if (build.HasValue && settings)
                {
                    builds.Add(build.Value);
                    settingsMap[build.Value.assetBundleName] = settings;
                }
            }

            if (builds.Count == 0)
            {
                EditorUtility.DisplayDialog("Map Builder", "No valid maps to build.", "OK");
                return;
            }

            Directory.CreateDirectory(BundleBuilder.OUTPUT_FOLDER);
            BuildPipeline.BuildAssetBundles(
                BundleBuilder.OUTPUT_FOLDER,
                builds.ToArray(),
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                EditorUserBuildSettings.activeBuildTarget
            );

            BundleBuilder.OrganizeAndCleanup(builds, settingsMap);
            Debug.Log($"Built {builds.Count} map bundle(s) to {Path.GetFullPath(BundleBuilder.OUTPUT_FOLDER)}/");
        }

        [MenuItem("HyenaQuest/Maps/Build Selected Map")]
        public static void BuildSelectedMap()
        {
            var selected = Selection.activeObject as WorldSettings;
            if (!selected)
            {
                EditorUtility.DisplayDialog("Map Builder", "Select a WorldSettings asset first.", "OK");
                return;
            }

            BundleBuilder.CleanOutputFolder();

            var settingsPath = AssetDatabase.GetAssetPath(selected);
            var build = BundleBuilder.CreateBuildForPath(settingsPath);
            if (!build.HasValue) return;

            Directory.CreateDirectory(BundleBuilder.OUTPUT_FOLDER);
            BuildPipeline.BuildAssetBundles(
                BundleBuilder.OUTPUT_FOLDER,
                new[] { build.Value },
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                EditorUserBuildSettings.activeBuildTarget
            );

            var settingsMap = new Dictionary<string, WorldSettings>
            {
                [build.Value.assetBundleName] = selected
            };

            BundleBuilder.OrganizeAndCleanup(new List<AssetBundleBuild> { build.Value }, settingsMap);

            var bundleName = build.Value.assetBundleName;
            var mapName = Path.GetFileNameWithoutExtension(bundleName);
            Debug.Log($"[MapBuilder] Built: {mapName}/{bundleName}");
            EditorUtility.RevealInFinder(Path.Combine(BundleBuilder.OUTPUT_FOLDER, mapName));
        }

        [MenuItem("HyenaQuest/Maps/Build Selected Map", true)]
        private static bool BuildSelectedMapValidation()
        {
            return Selection.activeObject is WorldSettings;
        }

        #region PRIVATE

        private static void CleanOutputFolder()
        {
            if (Directory.Exists(BundleBuilder.OUTPUT_FOLDER))
                Directory.Delete(BundleBuilder.OUTPUT_FOLDER, true);
        }

        private static AssetBundleBuild? CreateBuildForSettings(string guid)
        {
            return BundleBuilder.CreateBuildForPath(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static AssetBundleBuild? CreateBuildForPath(string settingsPath)
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(settingsPath);
            if (!settings)
            {
                Debug.LogWarning($"Could not load WorldSettings at: {settingsPath}");
                return null;
            }

            var mapFolder = Path.GetDirectoryName(settingsPath)!.Replace("\\", "/");
            var bundleName = Path.GetFileName(mapFolder).ToLowerInvariant();

            var assets = BundleBuilder.CollectFolderAssets(mapFolder);
            if (assets.Length == 0)
            {
                Debug.LogWarning($"No assets found in: {mapFolder}");
                return null;
            }

            Debug.Log($"Queued: {bundleName} ({assets.Length} assets from {mapFolder}/)");
            return new AssetBundleBuild
            {
                assetBundleName = $"{bundleName}.bundle",
                assetNames = assets
            };
        }

        private static string[] CollectFolderAssets(string folderPath)
        {
            var assets = new HashSet<string>();

            var guids = AssetDatabase.FindAssets("", new[] { folderPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (path.EndsWith(".cs")) continue;

                assets.Add(path);
            }

            var folderAssets = assets.ToArray();
            foreach (var asset in folderAssets)
            foreach (var dep in AssetDatabase.GetDependencies(asset, true))
            {
                if (dep.EndsWith(".cs")) continue;
                if (dep.StartsWith("Packages/")) continue;

                assets.Add(dep);
            }

            return assets.ToArray();
        }

        private static void OrganizeAndCleanup(List<AssetBundleBuild> builds,
            Dictionary<string, WorldSettings> settingsMap)
        {
            foreach (var build in builds)
            {
                var bundleFileName = build.assetBundleName;
                var mapName = Path.GetFileNameWithoutExtension(bundleFileName);

                var mapFolder = Path.Combine(BundleBuilder.OUTPUT_FOLDER, mapName);
                var bundlesFolder = Path.Combine(mapFolder, "bundles");
                Directory.CreateDirectory(bundlesFolder);

                // Move bundle into bundles/
                var src = Path.Combine(BundleBuilder.OUTPUT_FOLDER, bundleFileName);
                var dst = Path.Combine(bundlesFolder, bundleFileName);

                if (File.Exists(src))
                {
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }

                // Clean up manifest
                var manifestSrc = src + ".manifest";
                if (File.Exists(manifestSrc)) File.Delete(manifestSrc);

                // Generate mod.json
                BundleBuilder.GenerateModJson(mapFolder, mapName, settingsMap.GetValueOrDefault(bundleFileName));
            }

            // Clean up root bundle artifacts
            var rootBundleName = Path.GetFileName(BundleBuilder.OUTPUT_FOLDER);
            var rootBundle = Path.Combine(BundleBuilder.OUTPUT_FOLDER, rootBundleName);

            if (File.Exists(rootBundle)) File.Delete(rootBundle);
            if (File.Exists(rootBundle + ".manifest")) File.Delete(rootBundle + ".manifest");
        }

        private static void GenerateModJson(string mapFolder, string mapName, WorldSettings settings)
        {
            var title = settings ? settings.name : mapName;
            var description = $"Custom map {title}";

            title = title.Replace("\\", @"\\").Replace("\"", "\\\"");
            description = description.Replace("\\", @"\\").Replace("\"", "\\\"");

            var json = "{\n" +
                       $"    \"title\": \"{title}\",\n" +
                       $"    \"description\": \"{description}\",\n" +
                       "    \"type\": \"SHARED\",\n" +
                       "    \"author\": \"\",\n" +
                       "    \"tags\": [\"MAP\"]\n" +
                       "}";
            
            var path = Path.Combine(mapFolder, "mod.json");
            File.WriteAllText(path, json);

            Debug.Log($"[MapBuilder] Generated: {path}");
        }

        #endregion
    }
}

#endif