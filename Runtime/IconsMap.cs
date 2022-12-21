using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SiegeUp.IconRenderer
{
    [ExecuteInEditMode]
    [CreateAssetMenu]
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
        public IconRenderConfig DefaultRenderConfig => defaultRenderConfig;
        public static IconsMap Instance { get; private set; }
        public IEnumerable<PrefabIconInfo> RuntimeIcons => runtimeIcons;

        IconsMap()
        {
            Instance = this;
        }

        private void Awake()
        {
            Instance = this;
        }

        [System.Serializable]
        public class PrefabIconInfo
        {
            public GameObject prefabRef;
            public Texture2D texture2d;
            public IconRenderConfig renderConfig;

            Sprite spriteCache;
            public Sprite Sprite
            {
                get
                {
                    if (!spriteCache)
                        spriteCache = texture2d ? Sprite.Create(texture2d, new Rect(Vector2.zero, new Vector2(texture2d.width, texture2d.height)), Vector2.zero) : null;
                    return spriteCache;
                }
            }
        }

        public PrefabIconInfo GetPrefabIconInfo(GameObject prefabRef)
        {
            var prefabIconInfo = iconsMap.Find(item => item.prefabRef && item.prefabRef == prefabRef);
            if (prefabIconInfo == null)
                prefabIconInfo = runtimeIcons.Find(item => item.prefabRef && item.prefabRef == prefabRef);
            if (prefabIconInfo == null)
            {
                prefabIconInfo = new PrefabIconInfo { prefabRef = prefabRef, texture2d = null, renderConfig = defaultRenderConfig };
                runtimeIcons.Add(prefabIconInfo);
            }
            return prefabIconInfo;
        }

        public void SetRuntimeIcons(IEnumerable<PrefabIconInfo> newRuntimeIcons)
        {
            runtimeIcons = newRuntimeIcons.ToList();
        }
    }
}