using UnityEngine;
using UnityEditor;

namespace MegaFiers
{
	[CustomEditor(typeof(MegaSprite))]
	public class MegaSpriteEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			MegaSprite mod = (MegaSprite)target;

			DrawDefaultInspector();

			if ( GUILayout.Button("Update Sprite") )
				mod.UpdateSprite();

			if ( GUI.changed )
				EditorUtility.SetDirty(mod);
		}

		public void OnSceneGUI()
		{
			MegaSprite mod = (MegaSprite)target;

			if ( mod )
			{
				Handles.matrix = mod.transform.localToWorldMatrix;
				mod.pivot = MegaEditorGUILayout.PositionHandle(target, mod.pivot, Quaternion.identity);
				Handles.Label(mod.pivot, "Pivot");
			}
		}
	}
}