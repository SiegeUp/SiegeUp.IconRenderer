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
            var cfgTRS = Matrix4x4.TRS(renderConfig.Position, Quaternion.Euler(renderConfig.Rotation), renderConfig.Scale);

            foreach (var meshFilter in objectRoot.GetComponentsInChildren<MeshFilter>())
            {
                try
                {
                    if (System.Array.IndexOf(ignoreLayers, meshFilter.gameObject.layer) != -1)
                        continue;

                    var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                    var mesh = meshFilter.sharedMesh;
                    if (!mesh || !meshRenderer) continue;

                    var childRelativeToRoot =
                        objectRoot.transform.worldToLocalMatrix *
                        meshFilter.transform.localToWorldMatrix;

                    var finalMatrix = cfgTRS * childRelativeToRoot;

                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        int materialIndex = Mathf.Min(i, meshRenderer.sharedMaterials.Length - 1);

                        if (!materialMap.TryGetValue(meshRenderer, out var baseMat))
                            baseMat = meshRenderer.sharedMaterials[materialIndex];

                        var tweakedMaterial = factionMask ? IconRenderingSettings.Instance.GetFactionMaskMaterial(baseMat) : IconRenderingSettings.Instance.GetMaterialReplacement(baseMat);

                        preview.DrawMesh(mesh, finalMatrix, tweakedMaterial, i);
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
