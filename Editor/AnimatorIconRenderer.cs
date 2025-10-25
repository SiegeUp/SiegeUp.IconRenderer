using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer.Editor
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/Animator Icon Renderer")]
    public class AnimatorIconRenderer : BaseIconRenderer
    {
        [SerializeField] int[] ignoreLayers;

        [System.NonSerialized] readonly Dictionary<SkinnedMeshRenderer, Mesh> bakedCache = new();

        public override void Render(IconRenderConfig renderConfig, GameObject objectRoot, PreviewRenderUtility preview, Dictionary<Component, Material> materialMap, bool factionMask)
        {
            try
            {
                ApplyAnimationFrame(objectRoot, renderConfig.ClipIndex, renderConfig.FrameIndex);

                var cfgTRS = Matrix4x4.TRS(renderConfig.Position, Quaternion.Euler(renderConfig.Rotation), renderConfig.Scale);

                foreach (var mf in objectRoot.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!mf.sharedMesh) 
                        continue;

                    if (IsIgnored(mf.gameObject.layer))
                        continue;

                    var mr = mf.GetComponent<MeshRenderer>();

                    if (!mr)
                        continue;

                    var childRelToRoot = objectRoot.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    var finalMatrix = cfgTRS * childRelToRoot;

                    DrawSubmeshes(
                        preview,
                        mf.sharedMesh,
                        mr.sharedMaterials,
                        mr,
                        finalMatrix,
                        materialMap,
                        factionMask);
                }

                foreach (var smr in objectRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!smr.sharedMesh) 
                        continue;

                    if (IsIgnored(smr.gameObject.layer))
                        continue;

                    if (!bakedCache.TryGetValue(smr, out var baked))
                    {
                        baked = new Mesh { name = $"{smr.name}_BakedIcon" };
                        baked.MarkDynamic();
                        bakedCache[smr] = baked;
                    }

                    baked.Clear();
                    smr.BakeMesh(baked, true);

                    var childRelToRoot = objectRoot.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                    var finalMatrix = cfgTRS * childRelToRoot;

                    DrawSubmeshes(
                        preview,
                        baked,
                        smr.sharedMaterials,
                        smr,
                        finalMatrix,
                        materialMap,
                        factionMask);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        bool IsIgnored(int layer) =>
            System.Array.IndexOf(ignoreLayers, layer) != -1;

        void DrawSubmeshes(PreviewRenderUtility preview, Mesh mesh, Material[] sourceMats, Component keyForMaterialMap, Matrix4x4 matrix, Dictionary<Component, Material> materialMap, bool factionMask)
        {
            var subCount = Mathf.Max(1, mesh.subMeshCount);
            for (int si = 0; si < subCount; si++)
            {
                int matIndex = Mathf.Clamp(si, 0, sourceMats.Length - 1);

                if (!materialMap.TryGetValue(keyForMaterialMap, out var baseMat))
                    baseMat = sourceMats.Length > 0 ? sourceMats[matIndex] : null;

                if (!baseMat) continue;

                var mat = IconRenderingSettings.Instance.GetMaterialReplacement(baseMat, factionMask);

                preview.DrawMesh(mesh, matrix, mat, si);
            }
        }

        static void ApplyAnimationFrame(GameObject root, int clipIndex, int frameIndex)
        {
            if (!root) 
                return;

            var animator = root.GetComponentInChildren<Animator>(true);

            if (!animator) 
                return;

            var ctrl = animator.runtimeAnimatorController;

            if (!ctrl || ctrl.animationClips == null || ctrl.animationClips.Length == 0)
                return;

            int ci = Mathf.Clamp(clipIndex, 0, ctrl.animationClips.Length - 1);
            var clip = ctrl.animationClips[ci];

            if (!clip)
                return;

            float fps = clip.frameRate > 0f ? clip.frameRate : 60f;
            float t = Mathf.Clamp(frameIndex / fps, 0f, clip.length);

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();
            AnimationMode.SampleAnimationClip(animator.gameObject, clip, t);
        }
    }
}
