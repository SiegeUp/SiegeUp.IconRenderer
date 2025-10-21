using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    public abstract class BaseIconRenderer : ScriptableObject
    {
        public abstract void Render(IconRenderConfig renderConfig, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap);
    }
}
