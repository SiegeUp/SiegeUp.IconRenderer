using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace SiegeUp.IconRenderer
{
    public class IconRenderingTool : EditorWindow
    {
        PreviewRenderUtility preview;

        Texture2D finalIcon;
        Vector2 scrollPosition;

        Material transparencyPostProcess;
        RenderTexture transparentIconRT;

        const int iconSize = 1024;
        Rect iconRenderRect = new Rect(0, 0, iconSize, iconSize);

        [MenuItem("Window/Icon Renderer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IconRenderingTool));
        }

        void SaveIcon(GameObject obj, Texture2D icon)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long _);
            string path = Path.Join(IconRenderingSettings.Instance.IconsMap.IconsPath, $"/icon_{ guid }.png");

            byte[] bytes = icon.EncodeToPNG();

            UnityEngine.Windows.File.WriteAllBytes(Path.Join(Application.dataPath, path), bytes);

            AssetDatabase.ImportAsset(Path.Join("Assets/", path), ImportAssetOptions.ForceSynchronousImport);
            var iconAsset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + path);

            IconRenderingSettings.Instance.IconsMap.GetPrefabIconInfo(obj).texture2d = iconAsset;

            EditorUtility.SetDirty(IconRenderingSettings.Instance.IconsMap);
        }

        private void OnGUI()
        {
            var selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets | SelectionMode.TopLevel);
            GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var selectedObject in selectedObjects)
            {
                GUILayout.Label("+ " + selectedObject.name);
                var iconInfo = IconsMap.Instance.GetPrefabIconInfo(selectedObject);
                GUILayoutOption[] labelOptions = { GUILayout.Width(100), GUILayout.Height(100) };

                if (iconInfo != null && iconInfo.texture2d && iconInfo.renderConfig)
                {
                    GUILayout.Label("Size: " + iconInfo.texture2d.width + "x" + iconInfo.texture2d.height);
                    GUILayout.Label("Render config: " + iconInfo.renderConfig.name);
                    GUILayout.Label("Rotation: " + iconInfo.renderConfig.Rotation);

                    var renderConfigEditor = Editor.CreateEditor(iconInfo.renderConfig);
                    renderConfigEditor.OnInspectorGUI();
                }
                else
                {
                    GUILayout.Label("Not rendered yet");
                    GUILayout.Label("Press render to see generated parameters");
                }

                GUILayout.BeginHorizontal();
                if (iconInfo != null && iconInfo.texture2d)
                    GUILayout.Label(iconInfo.texture2d, labelOptions);

                var newIcon = RenderIcon(selectedObject);
                GUILayout.Label(newIcon, labelOptions);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Save"))
                    SaveIcon(selectedObject, newIcon);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        RectOffset FindBounds(Texture2D tex)
        {
            const int step = 4;

            int top = tex.height;
            int left = tex.width;
            int bottom = 0;
            int right = 0;

            for (int x = 0; x < iconSize / step; x++)
            {
                for (int y = 0; y < iconSize / step; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y) * step;
                    Color pixel = tex.GetPixel(pos.x, pos.y);
                    if (pixel.r + pixel.g + pixel.b > 0.01f)
                    {
                        top = Mathf.Min(top, pos.y);
                        left = Mathf.Min(left, pos.x);
                        bottom = Mathf.Max(bottom, pos.y);
                        right = Mathf.Max(right, pos.x);
                    }
                }
            }

            return new RectOffset(
                Mathf.Max(0, left - step),
                Mathf.Min(iconSize, right + step),
                Mathf.Max(0, top - step), 
                Mathf.Min(iconSize, bottom + step));
        }

        public Texture2D RenderIcon(GameObject gameObject)
        {
            var iconInfo = IconsMap.Instance.GetPrefabIconInfo(gameObject);

            var blackBgIcon = RenderIcon(gameObject, Color.black);
            var whiteBgIcon = RenderIcon(gameObject, Color.white);

            RectOffset rectOffset = FindBounds(blackBgIcon);

            Vector2 size = new Vector2(rectOffset.right - rectOffset.left, rectOffset.bottom - rectOffset.top);
            float maxSize = Mathf.Max(size.x, size.y);
            float scale = maxSize / iconSize;
            float padding = scale * iconInfo.renderConfig.Padding * 2;

            Vector2 aspectRatioPaddings = new Vector2(maxSize - size.x, maxSize - size.y) / 2;
            Vector2 scaledOffset = new Vector2(rectOffset.left - aspectRatioPaddings.x, rectOffset.top - aspectRatioPaddings.y) / iconSize;

            Vector2 additionalOffset = iconInfo.renderConfig.Offset * scale + Vector2.one * iconInfo.renderConfig.Padding * scale;

            if (!transparencyPostProcess)
                transparencyPostProcess = new Material(Shader.Find("IconRenderer/TransparencyPostprocess"));

            if (!transparentIconRT)
                transparentIconRT = new RenderTexture(iconInfo.renderConfig.Size.x, iconInfo.renderConfig.Size.y, 32);
                
            transparencyPostProcess.SetTexture("_MainTex", blackBgIcon);
            var scaleAndOffset = new Vector4(scale + padding, scale + padding, scaledOffset.x - additionalOffset.x, scaledOffset.y - additionalOffset.y);
            transparencyPostProcess.SetVector("ScaleOffset", scaleAndOffset);
            transparencyPostProcess.SetTexture("_WhiteBgTex", whiteBgIcon);
            Graphics.Blit(blackBgIcon, transparentIconRT, transparencyPostProcess, 0);

            if (!finalIcon)
                finalIcon = new Texture2D(iconInfo.renderConfig.Size.x, iconInfo.renderConfig.Size.y);

            RenderTexture.active = transparentIconRT;
            finalIcon.ReadPixels(new Rect(0, 0, finalIcon.width, finalIcon.height), 0, 0);
            finalIcon.Apply();
            RenderTexture.active = null;
            return finalIcon;
        }
        
        public Texture2D RenderIcon(GameObject gameObject, Color backgroundColor)
        {
            var iconInfo = IconsMap.Instance.GetPrefabIconInfo(gameObject);

            preview.camera.backgroundColor = backgroundColor;
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.fieldOfView = IconRenderingSettings.Instance.FOV;

            var materialMap = IconRenderingSettings.Instance.MaterialMapper.GetMaterialMap(gameObject);

            preview.BeginStaticPreview(iconRenderRect);

            preview.lights[0].color = preview.lights[1].color = iconInfo.renderConfig.LightColor;

            preview.lights[0].intensity = 0.7f * iconInfo.renderConfig.LightIntencity;
            preview.lights[1].intensity = 0.9f * iconInfo.renderConfig.LightIntencity;

            foreach (var iconRenderer in IconRenderingSettings.Instance.IconRenderers)
                iconRenderer.Render(iconInfo, gameObject.gameObject, preview, materialMap);

            preview.camera.Render();
            return preview.EndStaticPreview();
        }

        void OnEnable()
        {
            preview = new PreviewRenderUtility();
            preview.lights[0].transform.localEulerAngles = new Vector3(10, 95, 30);
            preview.lights[0].intensity = 0.7f * 1.0f;
            preview.lights[0].type = LightType.Directional;
            preview.lights[1].transform.localEulerAngles = new Vector3(30, -13, 30);
            preview.lights[1].intensity = 0.9f * 1.0f;
            preview.lights[1].type = LightType.Directional;

            Camera camera = preview.camera;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;
            camera.transform.position = new Vector3(0, 1, -10);
            camera.transform.LookAt(Vector3.zero);
        }

        void OnDisable()
        {
            preview.Cleanup();

            if (transparentIconRT)
                DestroyImmediate(transparentIconRT);
            if (transparencyPostProcess)
                DestroyImmediate(transparencyPostProcess);
            if (finalIcon)
                DestroyImmediate(finalIcon);
        }
    }
}
