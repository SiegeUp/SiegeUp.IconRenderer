using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SiegeUp.IconRenderer
{
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/IconsMap")]
    public class IconsMap : ScriptableObject
    {
        [SerializeField]
        string iconsPath;

        [SerializeField]
        IconRenderConfig defaultRenderConfig;

        [SerializeField]
        List<PrefabIconInfo> iconsMap = new List<PrefabIconInfo>();

        [SerializeField]
        List<PrefabIconInfo> runtimeIcons = new List<PrefabIconInfo>();

        public string IconsPath => iconsPath;
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

        [System.Serializable]
        public class PrefabIconInfo
        {
            public GameObject prefabRef;

            public Sprite sprite;
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
