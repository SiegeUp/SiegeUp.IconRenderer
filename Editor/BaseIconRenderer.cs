using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer
{
    public abstract class BaseIconRenderer : ScriptableObject
    {
        public abstract void Render(IconsMap.PrefabIconInfo iconInfo, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap);
    }
}
