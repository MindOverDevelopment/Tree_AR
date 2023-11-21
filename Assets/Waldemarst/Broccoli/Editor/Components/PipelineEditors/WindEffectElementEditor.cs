using UnityEngine;
using UnityEditor;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Controller;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Wind effect node editor.
	/// </summary>
	[CustomEditor(typeof(WindEffectElement))]
	public class WindEffectElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The wind effect node.
		/// </summary>
		public WindEffectElement windEffectElement;
		SerializedProperty propMinSprout1Sway;
		SerializedProperty propMaxSprout1Sway;
		SerializedProperty propMinSprout2Sway;
		SerializedProperty propMaxSprout2Sway;
		SerializedProperty propBranchSway;
		SerializedProperty propBranchSwayFlexibility;
		SerializedProperty propTrunkBending;
		SerializedProperty propPreviewWindAlways;
		SerializedProperty propPreviewWindMode;
		SerializedProperty propWindQuality;
		bool shouldUpdateController = false;
		private static GUIContent resetWindBtn = new GUIContent ("Reset Wind", "Set all the Wind parameters for this instance to their default values.");
		private static GUIContent labelSprout1Sway = new GUIContent ("Sprout 1 Sway", "Sprout sway weight to apply to sprouts with wind pattern 1.");
		private static GUIContent labelSprout2Sway = new GUIContent ("Sprout 2 Sway", "Sprout sway weight to apply to sprouts with wind pattern 2.");
		#endregion

		#region Messages
		private static string MSG_SPROUT1_SWAY = "Swinging from side to side on the sprouts with wind pattern 1 following the wind direction.";
		private static string MSG_SPROUT2_SWAY = "Swinging from side to side on the sprouts with wind pattern 2 following the wind direction.";
		private static string MSG_BRANCH_SWAY = "Swinging from side to side on the branches following the wind direction.";
		private static string MSG_BRANCH_SWAY_FLEXIBILITY = "Modifies the branch sway making it more flexible (adding curvy bending).";
		private static string MSG_TRUNK_BENDING = "Bending factor to apply to the tree trunk when a wind directional force is applied to the tree.";
		private static string MSG_PREVIEW_WIND_ALWAYS = "Keeps the wind animation going even if this node is not selected.";
		private static string MSG_ENABLE_ANIMATED = "In order to preview the wind effect please make sure the scene has a WindZone object " +
			"and \"Animated Materials\" on the Scene View panel is enabled. This implementation has support for directional wind zones only.";
		private static string MSG_WIND_QUALITY = "Wind quality to set on the shader.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			windEffectElement = target as WindEffectElement;

			if (TreeFactory.GetActiveInstance() != null && 
				TreeFactory.GetActiveInstance ().previewTree != null)
			{
				if (GlobalSettings.broccoTreeControllerVersion == GlobalSettings.BROCCO_TREE_CONTROLLER_V1) {
					BroccoTreeController treeController = 
						TreeFactory.GetActiveInstance ().previewTree.obj.GetComponent<BroccoTreeController> ();
					if (treeController != null) {
						treeController.shaderType = (BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
						treeController.windQuality = (BroccoTreeController.WindQuality)windEffectElement.windQuality;
						treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
						treeController.sproutSway = windEffectElement.minSprout1Sway;
						treeController.editorWindAlways = windEffectElement.previewWindAlways;
						treeController.trunkBending = windEffectElement.trunkBending;
						treeController.EnableEditorWind (true);
					}
				} else {
					BroccoTreeController2 treeController = 
						TreeFactory.GetActiveInstance ().previewTree.obj.GetComponent<BroccoTreeController2> ();
					if (treeController != null) {
						treeController.windInstance = BroccoTreeController2.WindInstance.Local;
						treeController.localShaderType = (BroccoTreeController2.ShaderType)MaterialManager.leavesShaderType;
						treeController.localWindQuality = (BroccoTreeController2.WindQuality)windEffectElement.windQuality;
						treeController.trunkBending = windEffectElement.trunkBending;
						treeController.editorWindEnabled = true;
					}
					SetPreviewWind ();
				}
			}

			propMinSprout1Sway = GetSerializedProperty ("minSprout1Sway");
			propMaxSprout1Sway = GetSerializedProperty ("maxSprout1Sway");
			propMinSprout2Sway = GetSerializedProperty ("minSprout2Sway");
			propMaxSprout2Sway = GetSerializedProperty ("maxSprout2Sway");
			propBranchSway = GetSerializedProperty ("branchSway");
			propBranchSwayFlexibility = GetSerializedProperty ("branchSwayFlexibility");
			propTrunkBending = GetSerializedProperty ("trunkBending");
			propPreviewWindAlways = GetSerializedProperty ("previewWindAlways");
			propPreviewWindMode = GetSerializedProperty ("previewWindMode");
			propWindQuality = GetSerializedProperty ("windQuality");
		}
		/// <summary>
		/// Raises the disable specific event.
		/// </summary>
		protected override void OnDisableSpecific () {
			if (TreeFactory.GetActiveInstance () != null &&
			    TreeFactory.GetActiveInstance ().previewTree != null)
			{
				if (GlobalSettings.broccoTreeControllerVersion == GlobalSettings.BROCCO_TREE_CONTROLLER_V1) {
					BroccoTreeController treeController = 
						TreeFactory.GetActiveInstance ().previewTree.obj.GetComponent<BroccoTreeController> ();
					if (treeController != null) {
						treeController.shaderType = (BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
						treeController.windQuality = (BroccoTreeController.WindQuality)windEffectElement.windQuality;
						treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
						treeController.sproutSway = windEffectElement.minSprout1Sway;
						treeController.trunkBending = windEffectElement.trunkBending;
						treeController.EnableEditorWind (false);
					}
				} else {
					BroccoTreeController2 treeController = 
						TreeFactory.GetActiveInstance ().previewTree.obj.GetComponent<BroccoTreeController2> ();
					if (treeController != null) {
						treeController.editorWindEnabled = false || windEffectElement.previewWindAlways;
					}
				}
			}
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			shouldUpdateController = false;

			EditorGUILayout.HelpBox (MSG_ENABLE_ANIMATED, MessageType.Warning);
			EditorGUILayout.Space ();

			EditorGUI.BeginChangeCheck ();

			FloatRangePropertyField (propMinSprout1Sway, propMaxSprout1Sway, 0f, 2f, labelSprout1Sway);
			ShowHelpBox (MSG_SPROUT1_SWAY);
			FloatRangePropertyField (propMinSprout2Sway, propMaxSprout2Sway, 0f, 2f, labelSprout2Sway);
			ShowHelpBox (MSG_SPROUT2_SWAY);
			EditorGUILayout.Space ();

			EditorGUILayout.Slider (propBranchSway, 0f, 4f, "Branch Sway");
			ShowHelpBox (MSG_BRANCH_SWAY);

			EditorGUILayout.Slider (propBranchSwayFlexibility, 0f, 1f, "Branch Sway Flexibility");
			ShowHelpBox (MSG_BRANCH_SWAY_FLEXIBILITY);

			EditorGUILayout.Slider (propTrunkBending, 0f, 2f, "Trunk Bending");
			ShowHelpBox (MSG_TRUNK_BENDING);
			EditorGUILayout.Space ();

			EditorGUILayout.PropertyField (propWindQuality);
			ShowHelpBox (MSG_WIND_QUALITY);
			EditorGUILayout.Space ();

			bool resetWind = false;
			if (GUILayout.Button (resetWindBtn)) {
				windEffectElement.minSprout1Sway = 1f;
				windEffectElement.maxSprout1Sway = 1f;
				windEffectElement.minSprout2Sway = 1f;
				windEffectElement.maxSprout2Sway = 1f;
				propBranchSway.floatValue = 1f;
				propBranchSwayFlexibility.floatValue = 0.5f;
				propTrunkBending.floatValue = 1f;
				resetWind = true;
			}

			if (EditorGUI.EndChangeCheck () || resetWind) {
				ApplySerialized ();
				shouldUpdateController = true;
				UpdatePipeline (GlobalSettings.processingDelayMedium, true);
				SetUndoControlCounter ();
			}
			EditorGUILayout.Space ();

			EditorGUILayout.LabelField ("Wind Preview", EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (propPreviewWindMode);
			EditorGUILayout.HelpBox (windParamsInfo, MessageType.None);
			EditorGUILayout.PropertyField (propPreviewWindAlways);
			if (EditorGUI.EndChangeCheck ()) {
				ApplySerialized ();
				SetPreviewWind ();
				shouldUpdateController = true;
			}
			ShowHelpBox (MSG_PREVIEW_WIND_ALWAYS);
			EditorGUILayout.Space ();

			if (shouldUpdateController) {
				TreeFactory treeFactory = TreeFactory.GetActiveInstance ();
				if (GlobalSettings.broccoTreeControllerVersion == GlobalSettings.BROCCO_TREE_CONTROLLER_V1) {
					BroccoTreeController treeController = 
						treeFactory.previewTree.obj.GetComponent<BroccoTreeController> ();
					if (treeController != null) {
						treeController.editorWindAlways = propPreviewWindAlways.boolValue;
						treeController.shaderType = (BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
						treeController.windQuality = (BroccoTreeController.WindQuality)windEffectElement.windQuality;
						treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
						treeController.sproutSway = windEffectElement.minSprout1Sway;
						treeController.trunkBending = windEffectElement.trunkBending;
						treeController.localWindAmplitude = windEffectElement.windAmplitude;
						treeController.EnableEditorWind (true);
					}
				} else {
					BroccoTreeController2 treeController = 
						treeFactory.previewTree.obj.GetComponent<BroccoTreeController2> ();
					if (treeController != null) {
						treeController.windInstance = BroccoTreeController2.WindInstance.Local;
						treeController.localShaderType = (BroccoTreeController2.ShaderType)MaterialManager.leavesShaderType;
						treeController.localWindQuality = (BroccoTreeController2.WindQuality)windEffectElement.windQuality;
						treeController.trunkBending = windEffectElement.trunkBending;
					}
					SetPreviewWind ();
				}
			}

			DrawFieldHelpOptions ();
		}
		private string windParamsInfo;
		/// <summary>
		/// Set the preview wind parameters to the BroccoTreeController.
		/// </summary>
		private void SetPreviewWind () {
			BroccoTreeController2 treeController = 
				TreeFactory.GetActiveInstance ().previewTree.obj.GetComponent<BroccoTreeController2> ();
			if (treeController != null) {
				windParamsInfo = string.Empty;
				if (windEffectElement.previewWindMode == WindEffectElement.PreviewWindMode.WindZone) {
					WindZone[] windZones = FindObjectsOfType<WindZone> ();
					bool windZoneFound = false;
					for (int i = 0; i < windZones.Length; i++) {
						if (windZones [i].gameObject.activeSelf && windZones[i].mode == WindZoneMode.Directional) {
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								windZones[i].windMain, windZones[i].windTurbulence, windZones[i].transform.forward);
							windZoneFound = true;
							break;
						}
					}
					if (!windZoneFound) {
						windParamsInfo = "No active WindZone found in the scene.";
					}
					treeController.localWindSource = BroccoTreeController2.WindSource.WindZone;
				} else {
					treeController.localWindSource = BroccoTreeController2.WindSource.Self;
					treeController.customPreviewMode = true;
					switch (windEffectElement.previewWindMode) {
						case WindEffectElement.PreviewWindMode.CalmBreeze:
							treeController.SetLocalCustomWind (0.5f, 0.5f, Vector3.right);
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								0.5f, 0.5f, Vector3.right);
							break;
						case WindEffectElement.PreviewWindMode.Breeze:
							treeController.SetLocalCustomWind (1.2f, 0.75f, Vector3.right);
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								1.2f, 0.75f, Vector3.right);
							break;
						case WindEffectElement.PreviewWindMode.Windy:
							treeController.SetLocalCustomWind (2f, 1.4f, Vector3.right);
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								2f, 1.4f, Vector3.right);
							break;
						case WindEffectElement.PreviewWindMode.StrongWind:
							treeController.SetLocalCustomWind (3f, 2f, Vector3.right);
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								3f, 2f, Vector3.right);
							break;
						case WindEffectElement.PreviewWindMode.Stormy:
							treeController.SetLocalCustomWind (4f, 3f, Vector3.right);
							windParamsInfo = string.Format ("WindZone:\nMain:{0}\nTurbulence:{1}\nDirection:{2}", 
								4f, 3f, Vector3.right);
							break;
					}
				}
			}
		}
		#endregion
	}
}