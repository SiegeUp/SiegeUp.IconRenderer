using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.IconRenderer
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/IconRenderConfig")]
    public class IconRenderConfig : ScriptableObject
    {
        [SerializeField]
        Vector3 position;

        [SerializeField]
        Vector3 rotation;

        [SerializeField]
        Vector3 scale = Vector3.one;

        [SerializeField]
        Color lightColor = Color.white;

        [SerializeField]
        float lightIntencity = 1.5f;

        [SerializeField]
        float padding;

        [SerializeField]
        Vector2 offset;

        [SerializeField]
        Vector2Int size = new Vector2Int(256, 256);

        public Vector3 Position => position;
        public Vector3 Rotation => rotation;
        public Vector3 Scale => scale;
        public Vector2Int Size => size;
        public Color LightColor => lightColor;
        public float LightIntencity => lightIntencity;
        public float Padding => padding;
        public Vector2 Offset => offset;

        [System.Serializable]
        public struct MaterialReplacement
        {
            public Material sourceMaterial;
            public Material replacementMaterial;
        }

        public MaterialReplacement[] materialReplacements;

        public Material GetMaterialReplacement(Material sourceMaterial)
        {
            var replacementIndex = System.Array.FindIndex(materialReplacements, i => i.sourceMaterial == sourceMaterial);
            return replacementIndex != -1 ? materialReplacements[replacementIndex].replacementMaterial : sourceMaterial;
        }
    }
}