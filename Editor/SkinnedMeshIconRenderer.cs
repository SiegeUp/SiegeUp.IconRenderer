using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/Skinned Mesh Icon Renderer")]
    public class SkinnedMeshIconRenderer : BaseIconRenderer
    {
        [SerializeField]
        int[] ignoreLayers;
        Dictionary<SkinnedMeshRenderer, Mesh> tmpMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

        public override void Render(IconRenderConfig renderConfig, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap, bool factionMask)
        {
            try
            {
                var skinnedMeshRenderers = objectRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    if (System.Array.IndexOf(ignoreLayers, skinnedMeshRenderer.gameObject.layer) != -1)
                        continue;
                    var matrix = Matrix4x4.TRS(renderConfig.Position, Quaternion.Euler(renderConfig.Rotation), renderConfig.Scale);
                    var positionOffset = Matrix4x4.Translate(-objectRoot.transform.position);

                    if (!materialMap.TryGetValue(skinnedMeshRenderer, out var material))
                        material = skinnedMeshRenderer.sharedMaterials[0];
                    var tweakedMaterial = factionMask ? IconRenderingSettings.Instance.GetFactionMaskMaterial(material) : IconRenderingSettings.Instance.GetMaterialReplacement(material);

                    var mesh = new Mesh();
                    skinnedMeshRenderer.BakeMesh(mesh, true);
                    preview.DrawMesh(mesh, matrix * positionOffset * skinnedMeshRenderer.transform.localToWorldMatrix, tweakedMaterial, 0);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
