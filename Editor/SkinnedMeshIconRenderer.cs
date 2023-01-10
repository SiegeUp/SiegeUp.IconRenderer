using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SiegeUp.IconRenderer.Editor
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/Skinned Mesh Icon Renderer")]
    public class SkinnedMeshIconRenderer : BaseIconRenderer
    {
        [SerializeField]
        int[] ignoreLayers;
        Dictionary<SkinnedMeshRenderer, Mesh> tmpMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

        public override void Render(IconsMap.PrefabIconInfo iconInfo, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap)
        {
            try
            {
                var skinnedMeshRenderers = objectRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    if (System.Array.IndexOf(ignoreLayers, skinnedMeshRenderer.gameObject.layer) != -1)
                        continue;
                    var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(iconInfo.renderConfig.Rotation), Vector3.one * 0.3f);
                    var positionOffset = Matrix4x4.Translate(-objectRoot.transform.position);

                    if (!materialMap.TryGetValue(skinnedMeshRenderer, out var material))
                        material = skinnedMeshRenderer.sharedMaterials[0];
                    var tweakedMaterial = iconInfo.renderConfig.GetMaterialReplacement(material);

                    if (!tmpMeshes.TryGetValue(skinnedMeshRenderer, out var tmpMesh))
                        tmpMeshes[skinnedMeshRenderer] = tmpMesh = new Mesh();
                    skinnedMeshRenderer.BakeMesh(tmpMesh, true);
                    preview.DrawMesh(tmpMesh, matrix * positionOffset * skinnedMeshRenderer.transform.localToWorldMatrix, tweakedMaterial, 0);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
