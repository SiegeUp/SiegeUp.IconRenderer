#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    [CustomEditor(typeof(IconsMap))]
    public class IconsMapEditor : UnityEditor.Editor
    {
        SerializedProperty iconsPathProp;
        SerializedProperty configsPathProp;
        SerializedProperty defaultCfgProp;
        SerializedProperty iconsMapProp;

        string search = "";
        Vector2 scroll;
        int page = 0;
        const int PageSize = 50;

        enum Filter { All, MissingPrefab, MissingSprite, HasMask, NoMask }
        Filter filter = Filter.All;
        bool sortByName = true;

        GUIStyle _header;
        GUIStyle Header => _header ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

        void OnEnable()
        {
            iconsPathProp = serializedObject.FindProperty("iconsPath");
            configsPathProp = serializedObject.FindProperty("configsPath");
            defaultCfgProp = serializedObject.FindProperty("defaultRenderConfig");
            iconsMapProp = serializedObject.FindProperty("iconsMap");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Icons Map", Header);
            EditorGUILayout.PropertyField(iconsPathProp);
            EditorGUILayout.PropertyField(configsPathProp);
            EditorGUILayout.PropertyField(defaultCfgProp);

            DrawToolbar();

            var indices = BuildFilteredIndices();

            int pages = Mathf.Max(1, Mathf.CeilToInt(indices.Count / (float)PageSize));
            page = Mathf.Clamp(page, 0, pages - 1);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Показано {(indices.Count == 0 ? 0 : Mathf.Min(PageSize, indices.Count - page * PageSize))} из {indices.Count}", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("#", GUILayout.Width(30));
                GUILayout.Label("Prefab", GUILayout.Width(200));
                GUILayout.Label("Sprite", GUILayout.Width(180));
                GUILayout.Label("Mask", GUILayout.Width(120));
                GUILayout.Label("Render Config", GUILayout.ExpandWidth(true));
                GUILayout.Label("", GUILayout.Width(135));
            }

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(520));
            {
                int start = page * PageSize;
                int end = Mathf.Min(indices.Count, start + PageSize);

                for (int k = start; k < end; k++)
                {
                    int i = indices[k];
                    var elem = iconsMapProp.GetArrayElementAtIndex(i);
                    var prefab = elem.FindPropertyRelative("prefabRef");
                    var sprite = elem.FindPropertyRelative("sprite");
                    var mask = elem.FindPropertyRelative("factionMaskSprite");
                    var cfg = elem.FindPropertyRelative("renderConfig");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label((i + 1).ToString(), GUILayout.Width(30));
                        EditorGUILayout.PropertyField(prefab, GUIContent.none, GUILayout.Width(200));
                        EditorGUILayout.PropertyField(sprite, GUIContent.none, GUILayout.Width(180));
                        EditorGUILayout.PropertyField(mask, GUIContent.none, GUILayout.Width(120));
                        EditorGUILayout.PropertyField(cfg, GUIContent.none, GUILayout.ExpandWidth(true));
                    }

                    EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
                }
            }
            EditorGUILayout.EndScrollView();

            DrawPager(pages);

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear All Null"))
                    (target as IconsMap)?.ClearAllNull();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(150));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(18)))
                    search = "";

                filter = (Filter)EditorGUILayout.EnumPopup(filter, EditorStyles.toolbarPopup, GUILayout.Width(120));
                sortByName = GUILayout.Toggle(sortByName, "Sort A→Z", EditorStyles.toolbarButton, GUILayout.Width(80));

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("↻ Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    Repaint();
            }
        }

        void DrawPager(int pages)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = page > 0;
                if (GUILayout.Button("<<", GUILayout.Width(40))) page = 0;
                if (GUILayout.Button("<", GUILayout.Width(30))) page--;
                GUI.enabled = true;

                GUILayout.Label($"Страница {page + 1} / {pages}", GUILayout.Width(120));

                GUI.enabled = page < pages - 1;
                if (GUILayout.Button(">", GUILayout.Width(30))) page++;
                if (GUILayout.Button(">>", GUILayout.Width(40))) page = pages - 1;
                GUI.enabled = true;
            }
        }

        List<int> BuildFilteredIndices()
        {
            var list = new List<int>();
            for (int i = 0; i < iconsMapProp.arraySize; i++)
            {
                var elem = iconsMapProp.GetArrayElementAtIndex(i);
                var prefab = elem.FindPropertyRelative("prefabRef").objectReferenceValue as GameObject;
                var sprite = elem.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                var mask = elem.FindPropertyRelative("factionMaskSprite").objectReferenceValue as Sprite;

                string name = prefab ? prefab.name : (sprite ? sprite.name : "<missing>");
                if (!string.IsNullOrEmpty(search) && !name.ToLowerInvariant().Contains(search.ToLowerInvariant()))
                    continue;

                bool pass = filter switch {
                    Filter.All => true,
                    Filter.MissingPrefab => prefab == null,
                    Filter.MissingSprite => sprite == null,
                    Filter.HasMask => mask != null,
                    Filter.NoMask => mask == null,
                    _ => true
                };
                if (pass) list.Add(i);
            }

            if (sortByName)
            {
                list.Sort((a, b) => {
                    string na = GetName(a);
                    string nb = GetName(b);
                    return string.Compare(na, nb, System.StringComparison.OrdinalIgnoreCase);
                });
            }
            return list;

            string GetName(int idx)
            {
                var e = iconsMapProp.GetArrayElementAtIndex(idx);
                var pr = e.FindPropertyRelative("prefabRef").objectReferenceValue as GameObject;
                var sp = e.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                return pr ? pr.name : (sp ? sp.name : "");
            }
        }

        readonly struct SerializedElement
        {
            public readonly SerializedProperty Prop;
            public readonly string PrefabName;
            public SerializedElement(SerializedProperty prop)
            {
                Prop = prop.Copy();
                var prefab = prop.FindPropertyRelative("prefabRef").objectReferenceValue as GameObject;
                var sprite = prop.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                PrefabName = prefab ? prefab.name : (sprite ? sprite.name : "");
            }
        }
    }
}
#endif
