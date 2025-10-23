using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.IconRenderer
{
    [CreateAssetMenu(menuName = "SiegeUp.IconRenderer/IconRenderConfig")]
    public class IconRenderConfig : ScriptableObject
    {
        [SerializeField] Vector3 position;
        [SerializeField] Vector3 rotation;
        [SerializeField] Vector3 scale = Vector3.one;

        [SerializeField] Color light1Color = Color.white;
        [SerializeField] Color light2Color = Color.white;

        [SerializeField] float light1Intensity = 1.5f;
        [SerializeField] float light2Intensity = 1.5f;

        [SerializeField] Vector3 light1Position;
        [SerializeField] Vector3 light2Position;

        [SerializeField] float padding;
        [SerializeField] Vector2 offset;
        [SerializeField] Vector2Int size = new Vector2Int(256, 256);
        [SerializeField] int clipIndex;
        [SerializeField] int frameIndex;
        [SerializeField] int sSAA;
        [SerializeField] bool includeFactionMask = true;

        public Vector3 Position => position;
        public Vector3 Rotation => rotation;
        public Vector3 Scale => scale;
        public Vector2Int Size => size;
        public Color Light1Color => light1Color;
        public Color Light2Color => light2Color;
        public float Light1Intensity => light1Intensity;
        public float Light2Intensity => light2Intensity;
        public Vector3 Light1Position => light1Position;
        public Vector3 Light2Position => light2Position;
        public float Padding => padding;
        public Vector2 Offset => offset;
        public int ClipIndex => clipIndex;
        public int FrameIndex => frameIndex;
        public int SSAA => sSAA;    
        public bool IncludeFactionMask => includeFactionMask;    
        public bool NeedToUpdate { get; set; }

        void OnValidate()
        {
            NeedToUpdate = true;
        }
    }
}