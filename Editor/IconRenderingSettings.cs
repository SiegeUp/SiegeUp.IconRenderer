using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace SiegeUp.IconRenderer.Editor
{
	[CreateAssetMenu(menuName = "SiegeUp.IconRenderer/IconRenderingSettings")]
	public class IconRenderingSettings : ScriptableObject
	{
		[SerializeField]
		BaseIconRenderer[] iconRenderers;

		[SerializeField]
		BaseMaterialMapper materialMapper;

		[SerializeField]
		IconsMap iconsMap;

		[SerializeField]
		float fov = 30;

		public static IconRenderingSettings Instance { get; private set; }

		public IEnumerable<BaseIconRenderer> IconRenderers => iconRenderers.Where(i => i);
		public BaseMaterialMapper MaterialMapper => materialMapper;
		public IconsMap IconsMap => iconsMap;
		public float FOV => fov;

        void OnEnable()
		{
			Instance = this;
		}

		[InitializeOnLoadMethod]
        static void FindInstance()
		{
			string[] assets = AssetDatabase.FindAssets($"t:{nameof(IconRenderingSettings)}");
			if (assets.Length > 0)
			{
				string path = AssetDatabase.GUIDToAssetPath(assets[0]);
				Instance = AssetDatabase.LoadAssetAtPath<IconRenderingSettings>(path);
			}
			else
			{
				Debug.LogError($"In order to use Icons Renderer, create asset {nameof(IconRenderingSettings)}");
			}
		}
	}
}
