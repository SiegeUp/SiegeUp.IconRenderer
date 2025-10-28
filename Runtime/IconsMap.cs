using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace SiegeUp.IconRenderer
{
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/IconsMap")]
    public class IconsMap : ScriptableObject
    {
        [SerializeField] string iconsPath;
        [SerializeField] string configsPath;
        [SerializeField] IconRenderConfig defaultRenderConfig;
        [SerializeField] List<PrefabIconInfo> iconsMap = new List<PrefabIconInfo>();
        [SerializeField] List<PrefabIconInfo> runtimeIcons = new List<PrefabIconInfo>();

        public string IconsPath => iconsPath;
        public string ConfigsPath => configsPath;
        public static IconsMap Instance { get; private set; }
        public List<PrefabIconInfo> IconsMapObjects => iconsMap;

        IconsMap()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
        }

        [ContextMenu("ClearAllNull")]
        public void ClearAllNull()
        {
            for (int i = iconsMap.Count - 1; i >= 0; i--)
            {
                var icon = iconsMap[i];
                if (icon.prefabRef == null || icon.sprite == null)
                {
                    iconsMap.RemoveAt(i);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [System.Serializable]
        public class PrefabIconInfo
        {
            public GameObject prefabRef;

            public Sprite sprite;
            public Sprite factionMaskSprite;
            public IconRenderConfig renderConfig;
        }

        public PrefabIconInfo GetPrefabIconInfo(GameObject prefabRef)
        {
            var prefabIconInfo = iconsMap.Find(item => item.prefabRef && item.prefabRef == prefabRef);
            if (prefabIconInfo == null)
                prefabIconInfo = runtimeIcons.Find(item => item.prefabRef && item.prefabRef == prefabRef);
            if (prefabIconInfo == null)
            {
                prefabIconInfo = new PrefabIconInfo { prefabRef = prefabRef, sprite = null, renderConfig = defaultRenderConfig };
                if (!Application.isPlaying)
                {
                    iconsMap.Add(prefabIconInfo);
                }
            }
            return prefabIconInfo;
        }

        public void SetRuntimeIcons(IEnumerable<PrefabIconInfo> newRuntimeIcons)
        {
            runtimeIcons = newRuntimeIcons.ToList();
        }
    }
}
