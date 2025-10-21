using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/Mesh Icon Renderer")]
    public class MeshIconRenderer : BaseIconRenderer
    {
        [SerializeField]
        int[] ignoreLayers;

        public override void Render(IconRenderConfig renderConfig, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap, bool factionMask)
        {
            foreach (var meshFilter in objectRoot.GetComponentsInChildren<MeshFilter>())
            {
                try
                {
                    if (System.Array.IndexOf(ignoreLayers, meshFilter.gameObject.layer) != -1)
                        continue;
                    var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                    if (meshFilter.sharedMesh && meshRenderer)
                    {
                        for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                        {
                            int materialIndex = Mathf.Min(i, meshRenderer.sharedMaterials.Length - 1);
                            if (!materialMap.TryGetValue(meshRenderer, out var material))
                                material = meshRenderer.sharedMaterials[materialIndex];
                            var tweakedMaterial = factionMask ? renderConfig.GetFactionMaskMaterial(material) : renderConfig.GetMaterialReplacement(material);

                            var matrix = Matrix4x4.TRS(renderConfig.Position, Quaternion.Euler(renderConfig.Rotation), renderConfig.Scale);
                            var positionOffset = Matrix4x4.Translate(-renderConfig.Position);
                            preview.DrawMesh(meshFilter.sharedMesh, matrix * positionOffset * meshFilter.transform.localToWorldMatrix, tweakedMaterial, i);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
