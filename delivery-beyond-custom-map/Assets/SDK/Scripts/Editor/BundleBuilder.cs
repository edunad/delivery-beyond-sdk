#if UNITY_EDITOR

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

#endregion

namespace HyenaQuest
{
    public static class BundleBuilder
    {
        private const string OUTPUT_FOLDER = "Build";

        private static readonly string[] SHADER_EXTENSIONS = {
            ".shader", ".shadergraph", ".shadersubgraph"
        };

        private static readonly (BuildTarget target, string folder)[] PLATFORMS = {
            (BuildTarget.StandaloneWindows64, "windows"),
            (BuildTarget.StandaloneLinux64, "linux")
        };

        [MenuItem("HyenaQuest/Maps/Build All Maps")]
        public static void BuildAllMaps() {
            string[] guids = AssetDatabase.FindAssets("t:WorldSettings");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Map Builder", "No WorldSettings assets found in the project.", "OK");
                return;
            }

            List<AssetBundleBuild> contentBuilds = new List<AssetBundleBuild>();
            List<AssetBundleBuild> shaderBuilds = new List<AssetBundleBuild>();
            Dictionary<string, WorldSettings> settingsMap = new Dictionary<string, WorldSettings>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(path);
                (AssetBundleBuild? content, AssetBundleBuild? shaders) = BundleBuilder.CreateBuildsForPath(path);

                if (content.HasValue && settings) {
                    contentBuilds.Add(content.Value);
                    settingsMap[content.Value.assetBundleName] = settings;
                    if (shaders.HasValue) shaderBuilds.Add(shaders.Value);
                }
            }

            if (contentBuilds.Count == 0)
            {
                EditorUtility.DisplayDialog("Map Builder", "No valid maps to build.", "OK");
                return;
            }

            BundleBuilder.BuildBundles(contentBuilds, shaderBuilds);
            BundleBuilder.OrganizeAndCleanup(contentBuilds, shaderBuilds, settingsMap);

            Debug.Log($"Built {contentBuilds.Count} map bundle(s) for {BundleBuilder.PLATFORMS.Length} platform(s) to {Path.GetFullPath(BundleBuilder.OUTPUT_FOLDER)}/");
        }

        [MenuItem("HyenaQuest/Maps/Build Selected Map")]
        public static void BuildSelectedMap() {
            WorldSettings selected = Selection.activeObject as WorldSettings;
            if (!selected)
            {
                EditorUtility.DisplayDialog("Map Builder", "Select a WorldSettings asset first.", "OK");
                return;
            }

            string settingsPath = AssetDatabase.GetAssetPath(selected);
            (AssetBundleBuild? content, AssetBundleBuild? shaders) = BundleBuilder.CreateBuildsForPath(settingsPath);
            if (!content.HasValue) return;

            List<AssetBundleBuild> contentBuilds = new List<AssetBundleBuild> { content.Value };
            List<AssetBundleBuild> shaderBuilds = shaders.HasValue
                ? new List<AssetBundleBuild> { shaders.Value }
                : new List<AssetBundleBuild>();

            BundleBuilder.BuildBundles(contentBuilds, shaderBuilds);

            Dictionary<string, WorldSettings> settingsMap = new Dictionary<string, WorldSettings> {
                [content.Value.assetBundleName] = selected
            };

            BundleBuilder.OrganizeAndCleanup(contentBuilds, shaderBuilds, settingsMap);

            string mapName = Path.GetFileNameWithoutExtension(content.Value.assetBundleName);

            Debug.Log($"[MapBuilder] Built: {mapName} for {BundleBuilder.PLATFORMS.Length} platform(s)");
            EditorUtility.RevealInFinder(Path.Combine(BundleBuilder.OUTPUT_FOLDER, mapName));
        }

        [MenuItem("HyenaQuest/Maps/Build Selected Map", true)]
        private static bool BuildSelectedMapValidation() {
            return Selection.activeObject is WorldSettings;
        }

        #region PRIVATE
        private static string GenerateShaderVariantCollection(string mapName, IEnumerable<string> shaderPaths)
        {
            string svcPath = $"Assets/__Generated/{mapName}_AutoSVC.shadervariants";
            
            Directory.CreateDirectory("Assets/__Generated");
            
            ShaderVariantCollection svc = new ShaderVariantCollection();
            foreach (string path in shaderPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) continue;

                Shader shader = mat.shader;
                if (!shader) continue;

                string[] keywords = mat.shaderKeywords;
                try
                {
                    ShaderVariantCollection.ShaderVariant variant = new ShaderVariantCollection.ShaderVariant(
                        shader,
                        PassType.Normal,
                        keywords
                    );

                    svc.Add(variant);
                }
                catch
                {
                    // ignored
                }
            }
            
            AssetDatabase.CreateAsset(svc, svcPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return svcPath;
        }
        
        private static (AssetBundleBuild? content, AssetBundleBuild? shaders) CreateBuildsForPath(string settingsPath) {
            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(settingsPath);
            if (!settings) return (null, null);

            string mapFolder = Path.GetDirectoryName(settingsPath)!.Replace("\\", "/");
            string bundleName = Path.GetFileName(mapFolder).ToLowerInvariant();

            (string[] contentAssets, string[] shaderAssets) = CollectFolderAssets(mapFolder);
            
            string svcPath = BundleBuilder.GenerateShaderVariantCollection(bundleName, contentAssets.Concat(shaderAssets));
            if (!string.IsNullOrEmpty(svcPath))
            {
                List<string> shaderList = shaderAssets.ToList();
                shaderList.Add(svcPath);
                shaderAssets = shaderList.ToArray();
            }
            
            AssetBundleBuild contentBuild = new AssetBundleBuild {
                assetBundleName = $"{bundleName}.bundle",
                assetNames = contentAssets
            };

            AssetBundleBuild? shaderBuild = shaderAssets.Length > 0
                ? new AssetBundleBuild {
                    assetBundleName = $"{bundleName}_shaders.bundle",
                    assetNames = shaderAssets
                }
                : null;

            return (contentBuild, shaderBuild);
        }

        private static (string[] content, string[] shaders) CollectFolderAssets(string folderPath) {
            HashSet<string> allAssets = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || path.EndsWith(".cs")) continue;
                allAssets.Add(path);
            }

            foreach (string asset in allAssets.ToArray()) {
                foreach (string dep in AssetDatabase.GetDependencies(asset, true)) {
                    if (dep.EndsWith(".cs")) continue;
                    if (dep.StartsWith("Packages/") && !BundleBuilder.IsShaderAsset(dep)) continue;
                    
                    allAssets.Add(dep);
                }
            }

            HashSet<string> shaderSet = new HashSet<string>();
            HashSet<string> contentSet = new HashSet<string>();

            foreach (string asset in allAssets) {
                if (BundleBuilder.IsShaderAsset(asset)) shaderSet.Add(asset);
                else contentSet.Add(asset);
            }

            return (contentSet.ToArray(), shaderSet.ToArray());
        }

        private static bool IsShaderAsset(string path) {
            return BundleBuilder.SHADER_EXTENSIONS.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                   || path.EndsWith(".shadervariants", StringComparison.OrdinalIgnoreCase);
        }

        private static void BuildBundles(List<AssetBundleBuild> contentBuilds, List<AssetBundleBuild> shaderBuilds) {
            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup originalGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            List<AssetBundleBuild> allBuilds = new List<AssetBundleBuild>();
            allBuilds.AddRange(contentBuilds);
            allBuilds.AddRange(shaderBuilds);

            HashSet<string> shaderAssetPaths = new HashSet<string>();
            foreach (AssetBundleBuild sb in shaderBuilds)
                foreach (string asset in sb.assetNames)
                    shaderAssetPaths.Add(asset);

            foreach ((BuildTarget target, string folder) in BundleBuilder.PLATFORMS) {
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                PlayerSettings.SetGraphicsAPIs(target, target == BuildTarget.StandaloneLinux64
                    ? new[] { GraphicsDeviceType.Vulkan }
                    : new[] { GraphicsDeviceType.Direct3D11 });

                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

                AssetDatabase.SaveAssets();
                
                foreach (string shaderPath in shaderAssetPaths)
                    AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                string stagingDir = Path.Combine(OUTPUT_FOLDER, $"_staging_{folder}");
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
                Directory.CreateDirectory(stagingDir);

                BuildPipeline.BuildAssetBundles(
                    stagingDir,
                    allBuilds.ToArray(),
                    BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode,
                    target
                );
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(originalGroup, originalTarget);
        }

        private static void OrganizeAndCleanup(List<AssetBundleBuild> contentBuilds, List<AssetBundleBuild> shaderBuilds, Dictionary<string, WorldSettings> settingsMap) {
            HashSet<string> shaderBundleNames = new HashSet<string>();
            foreach (AssetBundleBuild sb in shaderBuilds) shaderBundleNames.Add(sb.assetBundleName);

            string firstPlatformFolder = BundleBuilder.PLATFORMS[0].folder;

            foreach (AssetBundleBuild build in contentBuilds)
            {
                string bundleFileName = build.assetBundleName;
                string mapName = Path.GetFileNameWithoutExtension(bundleFileName);
                string mapFolder = Path.Combine(BundleBuilder.OUTPUT_FOLDER, mapName);
                string bundlesFolder = Path.Combine(mapFolder, "bundles");

                Directory.CreateDirectory(bundlesFolder);

                string firstStaging = Path.Combine(BundleBuilder.OUTPUT_FOLDER, $"_staging_{firstPlatformFolder}");
                string contentSrc = Path.Combine(firstStaging, bundleFileName);
                string contentDst = Path.Combine(bundlesFolder, bundleFileName);

                if (File.Exists(contentSrc))
                {
                    if (File.Exists(contentDst)) File.Delete(contentDst);
                    File.Copy(contentSrc, contentDst);
                }

                string shaderBundleName = $"{mapName}_shaders.bundle";
                if (shaderBundleNames.Contains(shaderBundleName))
                    foreach ((BuildTarget _, string platformFolder) in BundleBuilder.PLATFORMS)
                    {
                        string platformDir = Path.Combine(bundlesFolder, platformFolder);
                        Directory.CreateDirectory(platformDir);

                        string shaderStaging = Path.Combine(BundleBuilder.OUTPUT_FOLDER, $"_staging_{platformFolder}");
                        string shaderSrc = Path.Combine(shaderStaging, shaderBundleName);
                        string shaderDst = Path.Combine(platformDir, shaderBundleName);

                        if (File.Exists(shaderSrc))
                        {
                            if (File.Exists(shaderDst)) File.Delete(shaderDst);
                            File.Move(shaderSrc, shaderDst);
                        }

                        string shaderManifest = shaderSrc + ".manifest";
                        if (File.Exists(shaderManifest)) File.Delete(shaderManifest);
                    }

                BundleBuilder.GenerateModJson(mapFolder, mapName, settingsMap.GetValueOrDefault(bundleFileName));

                string previewPath = Path.Combine(mapFolder, "preview.png");
                if (!File.Exists(previewPath)) BundleBuilder.GeneratePreviewImage(previewPath, mapName);
            }

            // Clean up -----------------------------
            foreach ((BuildTarget _, string platformFolder) in BundleBuilder.PLATFORMS)
            {
                string stagingDir = Path.Combine(BundleBuilder.OUTPUT_FOLDER, $"_staging_{platformFolder}");
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            }
            // -----------------
        }

        private static void GenerateModJson(string mapFolder, string mapName, WorldSettings settings) {
            string path = Path.Combine(mapFolder, "mod.json");
            if (File.Exists(path)) return;
            
            string title = settings ? settings.name : mapName;
            string description = $"Custom map: {title}";

            title = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            description = description.Replace("\\", "\\\\").Replace("\"", "\\\"");

            string json = "{\n" +
                          $"    \"title\": \"{title}\",\n" +
                          $"    \"description\": \"{description}\",\n" +
                          "    \"type\": \"SHARED\",\n" +
                          "    \"author\": \"\",\n" +
                          "    \"preview\": \"./preview.png\",\n" +
                          "    \"tags\": [\"MAP\"]\n" +
                          "}";

            File.WriteAllText(path, json);
            Debug.Log($"[MapBuilder] Generated: {path}");
        }

        private static void GeneratePreviewImage(string outputPath, string seed) {
            const int S = 512;
            uint h = 2166136261u;
            foreach (char c in seed)
            {
                h ^= c;
                h *= 16777619;
            }

            uint Rand() {
                h ^= h << 13;
                h ^= h >> 17;
                h ^= h << 5;
                return h;
            }

            (Color, Color, Color, Color)[] palettes = {
                (new Color(0.24f, 0.48f, 0.46f), new Color(0.18f, 0.38f, 0.36f), new Color(0.30f, 0.55f, 0.50f), new Color(0.14f, 0.30f, 0.30f)),
                (new Color(0.35f, 0.30f, 0.45f), new Color(0.25f, 0.22f, 0.38f), new Color(0.42f, 0.35f, 0.52f), new Color(0.18f, 0.16f, 0.28f)),
                (new Color(0.28f, 0.38f, 0.28f), new Color(0.20f, 0.30f, 0.20f), new Color(0.35f, 0.45f, 0.32f), new Color(0.15f, 0.22f, 0.15f)),
                (new Color(0.40f, 0.34f, 0.28f), new Color(0.30f, 0.26f, 0.20f), new Color(0.48f, 0.40f, 0.32f), new Color(0.22f, 0.18f, 0.14f)),
                (new Color(0.25f, 0.35f, 0.50f), new Color(0.18f, 0.26f, 0.40f), new Color(0.32f, 0.42f, 0.56f), new Color(0.12f, 0.18f, 0.30f)),
                (new Color(0.50f, 0.25f, 0.28f), new Color(0.38f, 0.18f, 0.20f), new Color(0.58f, 0.32f, 0.34f), new Color(0.28f, 0.12f, 0.14f)),
                (new Color(0.45f, 0.38f, 0.50f), new Color(0.30f, 0.25f, 0.42f), new Color(0.52f, 0.44f, 0.58f), new Color(0.20f, 0.15f, 0.30f)),
                (new Color(0.22f, 0.42f, 0.50f), new Color(0.15f, 0.30f, 0.45f), new Color(0.28f, 0.50f, 0.55f), new Color(0.10f, 0.22f, 0.35f)),
                (new Color(0.48f, 0.40f, 0.22f), new Color(0.38f, 0.32f, 0.16f), new Color(0.55f, 0.48f, 0.28f), new Color(0.26f, 0.22f, 0.10f)),
                (new Color(0.45f, 0.28f, 0.40f), new Color(0.35f, 0.18f, 0.32f), new Color(0.52f, 0.35f, 0.48f), new Color(0.24f, 0.12f, 0.22f)),
                (new Color(0.30f, 0.42f, 0.42f), new Color(0.22f, 0.34f, 0.38f), new Color(0.38f, 0.50f, 0.48f), new Color(0.14f, 0.24f, 0.28f)),
                (new Color(0.50f, 0.35f, 0.22f), new Color(0.40f, 0.28f, 0.16f), new Color(0.58f, 0.42f, 0.28f), new Color(0.30f, 0.20f, 0.10f)),
                (new Color(0.20f, 0.35f, 0.30f), new Color(0.14f, 0.28f, 0.24f), new Color(0.26f, 0.42f, 0.36f), new Color(0.08f, 0.20f, 0.16f)),
                (new Color(0.38f, 0.32f, 0.42f), new Color(0.28f, 0.24f, 0.35f), new Color(0.46f, 0.38f, 0.50f), new Color(0.18f, 0.16f, 0.25f)),
                (new Color(0.42f, 0.42f, 0.35f), new Color(0.32f, 0.32f, 0.26f), new Color(0.50f, 0.50f, 0.42f), new Color(0.22f, 0.22f, 0.18f))
            };

            (Color c0, Color c1, Color c2, Color c3) = palettes[Rand() % (uint)palettes.Length];
            int gradType = (int)(Rand() % 5);

            float[] bayer = {
                0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5
            };
            for (int i = 0; i < 16; i++) bayer[i] /= 16f;

            Color[] px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float nx = x / (float)S;
                    float ny = y / (float)S;

                    float t = gradType switch {
                        0     => ny,
                        1     => nx,
                        2     => (nx + ny) * 0.5f,
                        3     => Mathf.Clamp01(Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f)) * 1.4f),
                        var _ => Mathf.Max(Mathf.Abs(nx - 0.5f), Mathf.Abs(ny - 0.5f)) * 2f
                    };

                    Color col = Color.Lerp(c0, c1, t);
                    float d = bayer[y % 4 * 4 + x % 4];
                    col = Color.Lerp(col, c3, d > 0.5f + t * 0.3f ? 0.3f : 0f);

                    if (x % 32 == 0 || y % 32 == 0) col = Color.Lerp(col, c2, 0.15f);

                    col.r = Mathf.Round(col.r * 32f) / 32f;
                    col.g = Mathf.Round(col.g * 32f) / 32f;
                    col.b = Mathf.Round(col.b * 32f) / 32f;

                    px[y * S + x] = col;
                }

            string text = seed.Replace("_", " ").Replace("-", " ").ToUpperInvariant();

            int sc = text.Length > 12 ? 3 : 4;
            int tw = text.Length * 6 * sc - sc;
            int tx = (S - tw) / 2;
            int ty = (S - 7 * sc) / 2;

            Color tCol = Color.Lerp(c2, Color.white, 0.6f);
            Color sCol = new Color(0, 0, 0, 0.4f);

            for (int pass = 0; pass < 2; pass++)
            {
                int o = pass == 0 ? sc : 0;
                Color col = pass == 0 ? sCol : tCol;
                int cx = tx + o;

                foreach (char ch in text)
                {
                    byte[] g = BundleBuilder.GetGlyph(ch);
                    if (g != null)
                        for (int gy = 0; gy < 7; gy++)
                            for (int gx = 0; gx < 5; gx++)
                            {
                                if ((g[gy] & (1 << (4 - gx))) == 0) continue;
                                for (int sy = 0; sy < sc; sy++)
                                    for (int sx = 0; sx < sc; sx++)
                                    {
                                        int ppx = cx + gx * sc + sx, ppy = ty + o + gy * sc + sy;
                                        if (ppx < 0 || ppx >= S || ppy < 0 || ppy >= S) continue;
                                        int idx = (S - 1 - ppy) * S + ppx;
                                        px[idx] = pass == 0 ? Color.Lerp(px[idx], col, col.a) : col;
                                    }
                            }

                    cx += 6 * sc;
                }
            }

            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static byte[] GetGlyph(char c) {
            return c switch {
                'A'   => new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
                'B'   => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
                'C'   => new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
                'D'   => new byte[] { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E },
                'E'   => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
                'F'   => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
                'G'   => new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0E },
                'H'   => new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
                'I'   => new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
                'J'   => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
                'K'   => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
                'L'   => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
                'M'   => new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
                'N'   => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
                'O'   => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
                'P'   => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
                'Q'   => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
                'R'   => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
                'S'   => new byte[] { 0x0E, 0x11, 0x10, 0x0E, 0x01, 0x11, 0x0E },
                'T'   => new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
                'U'   => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
                'V'   => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
                'W'   => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
                'X'   => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
                'Y'   => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
                'Z'   => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
                '0'   => new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
                '1'   => new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
                '2'   => new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F },
                '3'   => new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x01, 0x11, 0x0E },
                '4'   => new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
                '5'   => new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
                '6'   => new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
                '7'   => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
                '8'   => new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
                '9'   => new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
                ' '   => null,
                '_'   => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
                '-'   => new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
                '.'   => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x0C },
                '!'   => new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 },
                '?'   => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
                '&'   => new byte[] { 0x0C, 0x12, 0x0C, 0x12, 0x15, 0x12, 0x0D },
                '\''  => new byte[] { 0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00 },
                var _ => new byte[] { 0x1F, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1F }
            };
        }

        #endregion
    }
}

#endif