using UnityEngine;
using UnityEditor;

namespace Broccoli.Controller
{
	[CustomEditor(typeof(BroccoTreeController2))]
	public class BroccoTreeControllerEditor : UnityEditor.Editor {
        #region Vars
		protected SerializedProperty serializedProperty;
		BroccoTreeController2 treeController;
        bool changed = false;
        #endregion

        #region GUI Vars
		SerializedProperty propWindInstance;
        
        //SerializedProperty propTrunkBending;
        private static GUIContent labelWindInstance = new GUIContent ("Wind Instance", 
            "How this tree instance updates its wind values. Use 'Local' for unique per instance values or 'Global' for values shared across all tree instances (best performance).");
        private static GUIContent labelWindSource = new GUIContent ("Wind Source", 
            "Source for the wind values to use to calculate the wind effect in the scene. Use 'WindZone' to get them from an active directional WindZone GameObject in the scene or 'Self' for custom values.");
        #endregion

		// Use this for initialization
		void OnEnable () {
			treeController = target as BroccoTreeController2;
            propWindInstance = serializedObject.FindProperty ("windInstance");
            //propTrunkBending = serializedObject.FindProperty ("trunkBending");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update ();
            changed = false;

            EditorGUI.BeginChangeCheck ();

			BroccoTreeController2.WindInstance windInstance =
                (BroccoTreeController2.WindInstance)EditorGUILayout.EnumPopup ("Wind Instance", treeController.windInstance);
            if (windInstance != treeController.windInstance) {
                treeController.windInstance = windInstance;
                propWindInstance.enumValueIndex = (int)windInstance;
                changed = true;
            }

            // LOCAL
            if (treeController.windInstance == BroccoTreeController2.WindInstance.Local) {
                // Local Wind Source.
                BroccoTreeController2.WindSource localWindSource =
                    (BroccoTreeController2.WindSource)EditorGUILayout.EnumPopup ("Wind Source", treeController.localWindSource);
                if (localWindSource != treeController.localWindSource) {
                    treeController.localWindSource = localWindSource;
                    changed = true;
                }

                // Wind Source WINDZONE
                if (treeController.localWindSource == BroccoTreeController2.WindSource.WindZone) {
                    // Display direction, main and turbulence.
                    EditorGUILayout.HelpBox (treeController.GetLocalWindValues (), MessageType.None);
                }
                // Wind Source SELF
                else {
                    // Wind Main.
                    float windMain = EditorGUILayout.FloatField ("Wind Main", treeController.localWindMain);
                    if (windMain != treeController.localWindMain) {
                        treeController.localWindParams.customWindMain = windMain;
                        treeController.UpdateWind (
                            treeController.localWindParams.customWindMain,
                            treeController.localWindParams.customWindTurbulence,
                            treeController.localWindParams.customWindDirection);
                        changed = true;
                    }
                    // Wind Turbulence.
                    float windTurbulence = EditorGUILayout.FloatField ("Wind Turbulence", treeController.localWindTurbulence);
                    if (windTurbulence != treeController.localWindTurbulence) {
                        treeController.localWindParams.customWindTurbulence = windTurbulence;
                        treeController.UpdateWind (
                            treeController.localWindParams.customWindMain,
                            treeController.localWindParams.customWindTurbulence,
                            treeController.localWindParams.customWindDirection);
                        changed = true;
                    }
                    // Wind Direction.
                    Vector3 windDirection = EditorGUILayout.Vector3Field ("Wind Direction", treeController.localWindDirection);
                    if (windDirection != treeController.localWindDirection) {
                        treeController.localWindParams.customWindDirection = windDirection;
                        treeController.UpdateWind (
                            treeController.localWindParams.customWindMain,
                            treeController.localWindParams.customWindTurbulence,
                            treeController.localWindParams.customWindDirection);
                        changed = true;
                    }
                }
            }
            // GLOBAL
            else {
                // Global Wind Source.
                BroccoTreeController2.WindSource globalWindSource =
                    (BroccoTreeController2.WindSource)EditorGUILayout.EnumPopup ("Wind Instance", treeController.globalWindSource);
                if (globalWindSource != treeController.globalWindSource) {
                    treeController.globalWindSource = globalWindSource;
                    changed = true;
                }

                /*
                // Trunk Bending.
                float trunkBending = EditorGUILayout.FloatField ("Trunk Bending", treeController.trunkBending);
                if (trunkBending != treeController.trunkBending) {
                    treeController.trunkBending = trunkBending;
                    propTrunkBending.floatValue = trunkBending;
                    treeController.globalWindSource = treeController.globalWindSource;
                    changed = true;
                }
                */

                // Wind Source WINDZONE
                if (treeController.globalWindSource == BroccoTreeController2.WindSource.WindZone) {
                    // Display direction, main and turbulence.
                    EditorGUILayout.HelpBox (treeController.GetGlobalWindValues (), MessageType.None);
                }
                // Wind Source SELF
                else {
                    // Wind Main.
                    float windMain = EditorGUILayout.FloatField ("Wind Main", treeController.globalWindMain);
                    if (windMain != treeController.globalWindMain) {
                        BroccoTreeController2.globalWindParams.customWindMain = windMain;
                        treeController.UpdateWind (
                            BroccoTreeController2.globalWindParams.customWindMain,
                            BroccoTreeController2.globalWindParams.customWindTurbulence,
                            BroccoTreeController2.globalWindParams.customWindDirection);
                        changed = true;
                    }
                    // Wind Turbulence.
                    float windTurbulence = EditorGUILayout.FloatField ("Wind Turbulence", treeController.globalWindTurbulence);
                    if (windTurbulence != treeController.globalWindTurbulence) {
                        BroccoTreeController2.globalWindParams.customWindTurbulence = windTurbulence;
                        treeController.UpdateWind (
                            BroccoTreeController2.globalWindParams.customWindMain,
                            BroccoTreeController2.globalWindParams.customWindTurbulence,
                            BroccoTreeController2.globalWindParams.customWindDirection);
                        changed = true;
                    }
                    // Wind Direction.
                    Vector3 windDirection = EditorGUILayout.Vector3Field ("Wind Direction", treeController.globalWindDirection);
                    if (windDirection != treeController.globalWindDirection) {
                        BroccoTreeController2.globalWindParams.customWindDirection = windDirection;
                        treeController.UpdateWind (
                            BroccoTreeController2.globalWindParams.customWindMain,
                            BroccoTreeController2.globalWindParams.customWindTurbulence,
                            BroccoTreeController2.globalWindParams.customWindDirection);
                        changed = true;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck () || changed) {
			    serializedObject.ApplyModifiedProperties ();
            }
		}
	}
}