using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    public class IconRenderingTool : EditorWindow
    {
        PreviewRenderUtility preview;

        Vector2 scrollPosition;

        Material transparencyPostProcess;
        RenderTexture transparentIconRT;

        const int iconSizeBase = 1024;

        readonly Dictionary<string, Texture2D> cachedIconsMap = new();
        readonly Dictionary<string, Texture2D> cachedFactionMasksMap = new();
        readonly Dictionary<string, IconRenderConfig> currentRenderConfigMap = new();

        GameObject[] oldSelectedObjects;

        [MenuItem("Window/Icon Renderer")]
        public static void ShowWindow() => GetWindow(typeof(IconRenderingTool));

        void OnEnable()
        {
            preview = new PreviewRenderUtility();
            SetupLight(1, Color.white, new Vector3(10, 95, 30), 1, Color.white, new Vector3(30, -13, 30));

            var cam = preview.camera;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000;
            cam.transform.position = new Vector3(0, 1, -10);
            cam.transform.LookAt(Vector3.zero);
            cam.allowHDR = false;
            cam.allowMSAA = true;
        }

        void OnDisable()
        {
            preview.Cleanup();
            if (transparentIconRT) DestroyImmediate(transparentIconRT);
            if (transparencyPostProcess) DestroyImmediate(transparencyPostProcess);
        }

        void OnGUI()
        {
            var selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets | SelectionMode.TopLevel);
            if (oldSelectedObjects == null || !selectedObjects.SequenceEqual(oldSelectedObjects))
                oldSelectedObjects = selectedObjects;

            GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var go in selectedObjects)
            {
                if (!go) 
                    continue;

                var iconInfo = IconsMap.Instance.GetPrefabIconInfo(go);
                var baseConfig = iconInfo != null ? iconInfo.renderConfig : null;

                InitCurrentConfigIfNeeded(go.name, baseConfig);

                var currentConfig = currentRenderConfigMap[go.name];

                GUILayout.Label("+ " + go.name, EditorStyles.boldLabel);
                DrawInfoBlock(iconInfo, baseConfig, ref currentConfig);
                currentRenderConfigMap[go.name] = currentConfig;

                GUILayout.BeginHorizontal();
                TryDrawSavedIcon(iconInfo);

                var needRegen = NeedRegen(go.name, iconInfo, currentConfig);
                if (needRegen)
                {
                    cachedIconsMap[go.name] = RenderIcon(go, currentConfig, ssaa: false, factionMask: false);
                    cachedFactionMasksMap[go.name] = RenderIcon(go, currentConfig, ssaa: false, factionMask: true);

                    if (currentConfig) 
                        currentConfig.NeedToUpdate = false;

                    if (iconInfo?.renderConfig) 
                        iconInfo.renderConfig.NeedToUpdate = false;
                }

                DrawPreviewPair(go.name);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Save"))
                {
                    SaveWithRerender(go, currentConfig);
                }

                GUILayout.Space(8);
            }

            if (selectedObjects.Length > 0 && GUILayout.Button("Save all"))
            {
                foreach (var go in selectedObjects)
                {
                    currentRenderConfigMap.TryGetValue(go.name, out var currentConfig);
                    SaveWithRerender(go, currentConfig);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawInfoBlock(IconsMap.PrefabIconInfo iconInfo, IconRenderConfig baseConfig, ref IconRenderConfig currentConfig)
        {
            GUILayoutOption[] labelOptions = { GUILayout.Width(100), GUILayout.Height(100) };

            if (iconInfo != null && iconInfo.sprite && baseConfig)
            {
                GUILayout.Label($"Size: {iconInfo.sprite.texture.width}x{iconInfo.sprite.texture.height}");
                GUILayout.Label("Render config: " + baseConfig.name);
                GUILayout.Label("Position: " + baseConfig.Position);
                GUILayout.Label("Rotation: " + baseConfig.Rotation);
                GUILayout.Label("Scale: " + baseConfig.Scale);

                var newCurrent = (IconRenderConfig)EditorGUILayout.ObjectField("Current config", currentConfig, typeof(IconRenderConfig), false);
                if (newCurrent != currentConfig)
                {
                    currentConfig = newCurrent ? newCurrent : baseConfig;
                    DropCacheFor(iconInfo.prefabRef.name);
                }

                var renderConfigEditor = UnityEditor.Editor.CreateEditor(currentConfig);
                renderConfigEditor?.OnInspectorGUI();
            }
            else
            {
                GUILayout.Label("Not rendered yet");
                GUILayout.Label("Press render to see generated parameters");

                var newCurrent = (IconRenderConfig)EditorGUILayout.ObjectField("Current config", currentConfig, typeof(IconRenderConfig), false);
                if (newCurrent != currentConfig)
                {
                    currentConfig = newCurrent ? newCurrent : baseConfig;
                    if (iconInfo != null) 
                        DropCacheFor(iconInfo.prefabRef.name);
                }
            }
        }

        void TryDrawSavedIcon(IconsMap.PrefabIconInfo iconInfo)
        {
            if (iconInfo != null && iconInfo.sprite != null)
            {
                GUILayoutOption[] labelOptions = { GUILayout.Width(100), GUILayout.Height(100) };
                GUILayout.Label(iconInfo.sprite.texture, labelOptions);
            }
        }

        void DrawPreviewPair(string key)
        {
            GUILayoutOption[] labelOptions = { GUILayout.Width(100), GUILayout.Height(100) };

            if (cachedIconsMap.TryGetValue(key, out var iconTex) && iconTex)
                GUILayout.Label(iconTex, labelOptions);

            if (cachedFactionMasksMap.TryGetValue(key, out var maskTex) && maskTex)
            {
                GUILayout.Space(8);
                GUILayout.Label(maskTex, labelOptions);
            }
        }

        void DropCacheFor(string key)
        {
            cachedIconsMap.Remove(key);
            cachedFactionMasksMap.Remove(key);
        }

        void SaveWithRerender(GameObject go, IconRenderConfig currentConfig)
        {
            var iconTexSave = RenderIcon(go, currentConfig, ssaa: true, factionMask: false);
            var maskTexSave = RenderIcon(go, currentConfig, ssaa: true, factionMask: true);

            cachedIconsMap[go.name] = iconTexSave;
            cachedFactionMasksMap[go.name] = maskTexSave;

            SaveIcon(go, iconTexSave, maskTexSave, currentConfig);
        }

        void SaveIcon(GameObject obj, Texture2D icon, Texture2D factionMask, IconRenderConfig renderConfig = null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long _);

            var iconSprite = SaveTextureAsSprite(icon, IconRenderingSettings.Instance.IconsMap.IconsPath, $"icon_{guid}.png");
            var iconInfo = IconRenderingSettings.Instance.IconsMap.GetPrefabIconInfo(obj);
            iconInfo.sprite = iconSprite;

            if (factionMask != null)
            {
                var maskSprite = SaveTextureAsSprite(factionMask, IconRenderingSettings.Instance.IconsMap.IconsPath, $"icon_{guid}_mask.png");
                iconInfo.factionMaskSprite = maskSprite;
            }

            if (renderConfig)
                iconInfo.renderConfig = renderConfig;

            EditorUtility.SetDirty(IconRenderingSettings.Instance.IconsMap);
            AssetDatabase.SaveAssetIfDirty(IconRenderingSettings.Instance.IconsMap);
        }

        static Sprite SaveTextureAsSprite(Texture2D tex, string relDir, string fileName)
        {
            string rel = Path.Join(relDir, "/" + fileName);
            string abs = Path.Join(Application.dataPath, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            File.WriteAllBytes(abs, tex.EncodeToPNG());
            string assetPath = Path.Join("Assets/", rel);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(assetPath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = 128;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // === Render pipeline ===

        bool NeedRegen(string key, IconsMap.PrefabIconInfo iconInfo, IconRenderConfig cfg)
        {
            bool notCached = !cachedIconsMap.TryGetValue(key, out _);
            return notCached || (cfg != null && cfg.NeedToUpdate) || (iconInfo?.renderConfig != null && iconInfo.renderConfig.NeedToUpdate);
        }

        Texture2D RenderIcon(GameObject gameObject, IconRenderConfig config, bool ssaa = false, bool factionMask = false)
        {
            config ??= IconsMap.Instance.GetPrefabIconInfo(gameObject).renderConfig;

            var black = RenderRaw(gameObject, Color.black, config, ssaa, factionMask);
            var white = RenderRaw(gameObject, Color.white, config, ssaa, factionMask);

            black.filterMode = FilterMode.Bilinear;
            white.filterMode = FilterMode.Bilinear;

            int W = black.width;
            RectOffset rectOffset = FindBounds(black);

            Vector2 size = new(rectOffset.right - rectOffset.left, rectOffset.bottom - rectOffset.top);
            float maxSize = Mathf.Max(size.x, size.y);
            float scale = maxSize / W;
            float padding = scale * config.Padding * 2;

            Vector2 aspectPad = new Vector2(maxSize - size.x, maxSize - size.y) * 0.5f;
            Vector2 scaledOff = new Vector2(rectOffset.left - aspectPad.x, rectOffset.top - aspectPad.y) / W;
            Vector2 additional = config.Offset * scale + Vector2.one * config.Padding * scale;

            EnsurePostProcessAndRT(config.Size.x, config.Size.y);

            transparencyPostProcess.SetTexture("_MainTex", black);
            transparencyPostProcess.SetTexture("_WhiteBgTex", white);
            transparencyPostProcess.SetVector("ScaleOffset",
                new Vector4(scale + padding, scale + padding, scaledOff.x - additional.x, scaledOff.y - additional.y));
            transparencyPostProcess.SetVector("_TexelSize", new Vector2(1f / W, 1f / W));

            Graphics.Blit(black, transparentIconRT, transparencyPostProcess, 0);

            var finalIcon = new Texture2D(config.Size.x, config.Size.y, TextureFormat.RGBA32, false, false) {
                filterMode = FilterMode.Bilinear
            };

            RenderTexture.active = transparentIconRT;
            finalIcon.ReadPixels(new Rect(0, 0, finalIcon.width, finalIcon.height), 0, 0);
            finalIcon.Apply();
            RenderTexture.active = null;

            return finalIcon;
        }

        Texture2D RenderRaw(GameObject gameObject, Color bg, IconRenderConfig config, bool ssaa, bool factionMask)
        {
            config ??= IconsMap.Instance.GetPrefabIconInfo(gameObject).renderConfig;

            var cam = preview.camera;
            cam.backgroundColor = bg;
            cam.clearFlags = CameraClearFlags.Color;
            cam.fieldOfView = IconRenderingSettings.Instance.FOV;

            var materialMap = IconRenderingSettings.Instance.MaterialMapper.GetMaterialMap(gameObject);

            int workingSize = iconSizeBase * Mathf.Max(1, ssaa ? config.SSAA : 1);
            var rect = new Rect(0, 0, workingSize, workingSize);

            preview.BeginStaticPreview(rect);

            SetupLight(config.Light1Intensity, config.Light1Color, config.Light1Position,
                       config.Light2Intensity, config.Light2Color, config.Light2Position);

            foreach (var iconRenderer in IconRenderingSettings.Instance.IconRenderers)
                iconRenderer.Render(config, gameObject.gameObject, preview, materialMap, factionMask);

            cam.Render();
            var tex = preview.EndStaticPreview();

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        void EnsurePostProcessAndRT(int width, int height)
        {
            if (!transparencyPostProcess)
                transparencyPostProcess = new Material(Shader.Find("IconRenderer/TransparencyPostprocess"));

            if (!transparentIconRT || transparentIconRT.width != width || transparentIconRT.height != height)
            {
                if (transparentIconRT) DestroyImmediate(transparentIconRT);
                transparentIconRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        RectOffset FindBounds(Texture2D tex)
        {
            const int step = 5;
            int W = tex.width, H = tex.height;

            int top = H, left = W, bottom = 0, right = 0;

            for (int x = 0; x < W; x += step)
            {
                for (int y = 0; y < H; y += step)
                {
                    var pixel = tex.GetPixel(x, y);
                    if (pixel.r + pixel.g + pixel.b > 0.01f)
                    {
                        if (y < top) top = y;
                        if (x < left) left = x;
                        if (y > bottom) bottom = y;
                        if (x > right) right = x;
                    }
                }
            }

            return new RectOffset(
                Mathf.Max(0, left - step * 2),
                Mathf.Min(W, right + step * 2),
                Mathf.Max(0, top - step * 2),
                Mathf.Min(H, bottom + step * 2));
        }

        void SetupLight(float l1Intensity, Color l1Color, Vector3 l1Euler,
                        float l2Intensity, Color l2Color, Vector3 l2Euler)
        {
            preview.lights[0].transform.localEulerAngles = l1Euler;
            preview.lights[0].intensity = l1Intensity;
            preview.lights[0].color = l1Color;
            preview.lights[0].type = LightType.Directional;
            preview.lights[0].bounceIntensity = 10;

            preview.lights[1].transform.localEulerAngles = l2Euler;
            preview.lights[1].intensity = l2Intensity;
            preview.lights[1].color = l2Color;
            preview.lights[1].type = LightType.Directional;
            preview.lights[1].bounceIntensity = 10;

        }

        void InitCurrentConfigIfNeeded(string key, IconRenderConfig baseConfig)
        {
            if (baseConfig == null) return;
            if (!currentRenderConfigMap.ContainsKey(key) || !currentRenderConfigMap[key])
                currentRenderConfigMap[key] = baseConfig;
        }
    }
}
