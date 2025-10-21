using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace SiegeUp.IconRenderer.Editor
{
    public class IconRenderingTool : EditorWindow
    {
        PreviewRenderUtility preview;

        Vector2 scrollPosition;

        Material transparencyPostProcess;
        RenderTexture transparentIconRT;

        const int iconSizeBase = 1024;

        Dictionary<string, Texture2D> cachedIconsMap = new();

        Dictionary<string, IconRenderConfig> currentRenderConfigMap = new();
        GameObject[] oldSelectedObjects;

        [MenuItem("Window/Icon Renderer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IconRenderingTool));
        }

        void SaveIcon(GameObject obj, Texture2D icon, IconRenderConfig renderConfig = null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long _);
            string path = Path.Join(IconRenderingSettings.Instance.IconsMap.IconsPath, $"/icon_{guid}.png");

            byte[] bytes = icon.EncodeToPNG();
            UnityEngine.Windows.File.WriteAllBytes(Path.Join(Application.dataPath, path), bytes);

            string assetPath = Path.Join("Assets/", path);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.alphaIsTransparency = true;
                textureImporter.filterMode = FilterMode.Bilinear;
                textureImporter.SaveAndReimport();
            }

            Sprite spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            var iconInfo = IconRenderingSettings.Instance.IconsMap.GetPrefabIconInfo(obj);
            iconInfo.sprite = spriteAsset;

            if (renderConfig)
                iconInfo.renderConfig = renderConfig;

            EditorUtility.SetDirty(IconRenderingSettings.Instance.IconsMap);
            AssetDatabase.SaveAssetIfDirty(IconRenderingSettings.Instance.IconsMap);
        }

        void OnGUI()
        {
            var selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets | SelectionMode.TopLevel);
            if (oldSelectedObjects == null || !selectedObjects.SequenceEqual(oldSelectedObjects))
            {
                oldSelectedObjects = selectedObjects;
            }

            GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var selectedObject in selectedObjects)
            {
                if (!selectedObject) continue;

                GUILayout.Label("+ " + selectedObject.name, EditorStyles.boldLabel);
                var iconInfo = IconsMap.Instance.GetPrefabIconInfo(selectedObject);
                GUILayoutOption[] labelOptions = { GUILayout.Width(100), GUILayout.Height(100) };

                IconRenderConfig baseConfig = iconInfo != null ? iconInfo.renderConfig : null;

                if (baseConfig && !currentRenderConfigMap.ContainsKey(selectedObject.name))
                    currentRenderConfigMap[selectedObject.name] = baseConfig;

                currentRenderConfigMap.TryGetValue(selectedObject.name, out var currentConfig);
                if (!currentConfig && baseConfig)
                {
                    currentConfig = baseConfig;
                    currentRenderConfigMap[selectedObject.name] = currentConfig;
                }

                if (iconInfo != null && iconInfo.sprite && baseConfig)
                {
                    GUILayout.Label("Size: " + iconInfo.sprite.texture.width + "x" + iconInfo.sprite.texture.height);
                    GUILayout.Label("Render config: " + baseConfig.name);
                    GUILayout.Label("Position: " + baseConfig.Position);
                    GUILayout.Label("Rotation: " + baseConfig.Rotation);
                    GUILayout.Label("Scale: " + baseConfig.Scale);

                    var newCurrent = (IconRenderConfig)EditorGUILayout.ObjectField("Current config", currentConfig, typeof(IconRenderConfig), false);
                    if (newCurrent != currentConfig)
                    {
                        currentRenderConfigMap[selectedObject.name] = newCurrent ? newCurrent : baseConfig; 
                        cachedIconsMap.Remove(selectedObject.name);
                        currentConfig = currentRenderConfigMap[selectedObject.name];
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
                        currentRenderConfigMap[selectedObject.name] = newCurrent ? newCurrent : baseConfig;
                        cachedIconsMap.Remove(selectedObject.name);
                        currentConfig = currentRenderConfigMap[selectedObject.name];
                    }
                }

                GUILayout.BeginHorizontal();

                if (iconInfo != null && iconInfo.sprite)
                    GUILayout.Label(iconInfo.sprite.texture, labelOptions);

                bool needRegen =
                    !cachedIconsMap.TryGetValue(selectedObject.name, out Texture2D cachedTexture) ||
                    (currentConfig != null && currentConfig.NeedToUpdate) ||
                    (iconInfo != null && iconInfo.renderConfig != null && iconInfo.renderConfig.NeedToUpdate);

                if (needRegen)
                {
                    cachedIconsMap[selectedObject.name] = RenderIcon(selectedObject, currentConfig);

                    if (currentConfig != null)
                        currentConfig.NeedToUpdate = false;

                    if (iconInfo?.renderConfig != null)
                        iconInfo.renderConfig.NeedToUpdate = false;
                }

                GUILayout.Label(cachedIconsMap[selectedObject.name], labelOptions);

                GUILayout.EndHorizontal();

                if (GUILayout.Button("Save"))
                {
                    cachedIconsMap[selectedObject.name] = RenderIcon(selectedObject, currentConfig, true);
                    SaveIcon(selectedObject, cachedIconsMap[selectedObject.name], currentConfig);
                }

                GUILayout.Space(8);
            }

            if (selectedObjects.Length > 0 && GUILayout.Button("Save all"))
            {
                foreach (var selectedObject in selectedObjects)
                {
                    currentRenderConfigMap.TryGetValue(selectedObject.name, out var currentConfig);
                    cachedIconsMap[selectedObject.name] = RenderIcon(selectedObject, currentConfig, true);
                    SaveIcon(selectedObject, cachedIconsMap[selectedObject.name], currentConfig);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        RectOffset FindBounds(Texture2D tex)
        {
            const int step = 4;
            int W = tex.width;
            int H = tex.height;

            int top = H;
            int left = W;
            int bottom = 0;
            int right = 0;

            for (int x = 0; x < W; x += step)
            {
                for (int y = 0; y < H; y += step)
                {
                    Color pixel = tex.GetPixel(x, y);
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
                Mathf.Max(0, left - step),
                Mathf.Min(W, right + step),
                Mathf.Max(0, top - step),
                Mathf.Min(H, bottom + step));
        }

        public Texture2D RenderIcon(GameObject gameObject, IconRenderConfig currentConfig, bool ssaa = false)
        {
            if (!currentConfig)
                currentConfig = IconsMap.Instance.GetPrefabIconInfo(gameObject).renderConfig;

            var blackBgIcon = RenderIcon(gameObject, Color.black, currentConfig, ssaa);
            var whiteBgIcon = RenderIcon(gameObject, Color.white, currentConfig, ssaa);

            blackBgIcon.filterMode = FilterMode.Bilinear;
            whiteBgIcon.filterMode = FilterMode.Bilinear;

            int W = blackBgIcon.width;
            int H = blackBgIcon.height;

            RectOffset rectOffset = FindBounds(blackBgIcon);

            Vector2 size = new Vector2(rectOffset.right - rectOffset.left, rectOffset.bottom - rectOffset.top);
            float maxSize = Mathf.Max(size.x, size.y);

            float scale = maxSize / W;
            float padding = scale * currentConfig.Padding * 2;

            Vector2 aspectRatioPaddings = new Vector2(maxSize - size.x, maxSize - size.y) / 2;
            Vector2 scaledOffset = new Vector2(
                rectOffset.left - aspectRatioPaddings.x,
                rectOffset.top - aspectRatioPaddings.y
            ) / W;

            Vector2 additionalOffset = currentConfig.Offset * scale + Vector2.one * currentConfig.Padding * scale;

            if (!transparencyPostProcess)
                transparencyPostProcess = new Material(Shader.Find("IconRenderer/TransparencyPostprocess"));

            if (!transparentIconRT ||
                transparentIconRT.width != currentConfig.Size.x ||
                transparentIconRT.height != currentConfig.Size.y)
            {
                if (transparentIconRT) DestroyImmediate(transparentIconRT);
                transparentIconRT = new RenderTexture(
                    currentConfig.Size.x,
                    currentConfig.Size.y,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear
                );
                transparentIconRT.filterMode = FilterMode.Bilinear;
                transparentIconRT.wrapMode = TextureWrapMode.Clamp;
            }

            transparencyPostProcess.SetTexture("_MainTex", blackBgIcon);
            transparencyPostProcess.SetTexture("_WhiteBgTex", whiteBgIcon);

            var scaleAndOffset = new Vector4(
                scale + padding,
                scale + padding,
                scaledOffset.x - additionalOffset.x,
                scaledOffset.y - additionalOffset.y
            );
            transparencyPostProcess.SetVector("ScaleOffset", scaleAndOffset);
            transparencyPostProcess.SetVector("_TexelSize", new Vector2(1f / W, 1f / H));

            Graphics.Blit(blackBgIcon, transparentIconRT, transparencyPostProcess, 0);

            var finalIcon = new Texture2D(currentConfig.Size.x, currentConfig.Size.y, TextureFormat.RGBA32, false, false);
            finalIcon.filterMode = FilterMode.Bilinear;

            RenderTexture.active = transparentIconRT;
            finalIcon.ReadPixels(new Rect(0, 0, finalIcon.width, finalIcon.height), 0, 0);
            finalIcon.Apply();
            RenderTexture.active = null;
            return finalIcon;
        }

        public Texture2D RenderIcon(GameObject gameObject, Color backgroundColor, IconRenderConfig currentConfig, bool ssaa = false)
        {
            if (!currentConfig)
                currentConfig = IconsMap.Instance.GetPrefabIconInfo(gameObject).renderConfig;

            var cam = preview.camera;
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.Color;
            cam.fieldOfView = IconRenderingSettings.Instance.FOV;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            var materialMap = IconRenderingSettings.Instance.MaterialMapper.GetMaterialMap(gameObject);

            int workingSize = iconSizeBase * Mathf.Max(1, ssaa ? currentConfig.SSAA : 1);
            Rect iconRenderRect = new Rect(0, 0, workingSize, workingSize);

            preview.BeginStaticPreview(iconRenderRect);

            SetupLight(
                currentConfig.Light1Intensity, currentConfig.Light1Color, currentConfig.Light1Position,
                currentConfig.Light2Intensity, currentConfig.Light2Color, currentConfig.Light2Position
            );

            foreach (var iconRenderer in IconRenderingSettings.Instance.IconRenderers)
                iconRenderer.Render(currentConfig, gameObject.gameObject, preview, materialMap);

            cam.Render();
            var tex = preview.EndStaticPreview();

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            return tex;
        }

        void SetupLight(float light1Intensity, Color light1Color, Vector3 light1Position,
                        float light2Intensity, Color light2Color, Vector3 light2Position)
        {
            preview.lights[0].transform.localEulerAngles = light1Position;
            preview.lights[0].intensity = light1Intensity;
            preview.lights[0].color = light1Color;
            preview.lights[0].type = LightType.Directional;

            preview.lights[1].transform.localEulerAngles = light2Position;
            preview.lights[1].intensity = light2Intensity;
            preview.lights[1].color = light2Color;
            preview.lights[1].type = LightType.Directional;
        }

        void OnEnable()
        {
            preview = new PreviewRenderUtility();

            SetupLight(1, Color.white, new Vector3(10, 95, 30), 1, Color.white, new Vector3(30, -13, 30));

            Camera camera = preview.camera;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;
            camera.transform.position = new Vector3(0, 1, -10);
            camera.transform.LookAt(Vector3.zero);

            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        void OnDisable()
        {
            preview.Cleanup();

            if (transparentIconRT)
                DestroyImmediate(transparentIconRT);
            if (transparencyPostProcess)
                DestroyImmediate(transparencyPostProcess);
        }
    }
}
