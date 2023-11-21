using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Factory;
using Broccoli.Pipe;
using Broccoli.Controller;

namespace Broccoli.Examples 
{
	using Pipeline = Broccoli.Pipe.Pipeline;
	using Position = Broccoli.Pipe.Position;
	public class WindSceneController : MonoBehaviour {
		#region Vars
        public GameObject windArrow = null;
        public GameObject targetWindArrow = null;
        public Coroutine coroutine = null;
        BroccoTreeController2 treeController = null;
		#endregion

		#region Events
		/// <summary>
		/// Start event.
		/// </summary>
		void Start () {
            // Get only one of the BroccoTreeController2 in the scene to update wind.
            // The rest of the tree controllers should have instance to global and source to self in order to receive wind updates.
            // 1. GLOBAL: wind calculations shared by all instances.
            // 2. SELF: wind values are set by script and not taken from a WindZone.
            BroccoTreeController2[] treeControllers = FindObjectsOfType<BroccoTreeController2> ();
            if (treeControllers != null && treeControllers.Length > 0) {
                treeController = treeControllers [0];
            }

            // If a tree controller was found, set the initial wind values.
            // This script will update the wind with random values every time a click is received.
            if (treeController != null) {
                treeController.windInstance = BroccoTreeController2.WindInstance.Global;
                treeController.globalWindSource = BroccoTreeController2.WindSource.Self;
                treeController.UpdateWind (0f, 0f, Vector3.right);
            }

            // Transition to a breeze wind in 5 seconds.
            coroutine = StartCoroutine (WindTo (1f, 1f, Vector3.left, 5f));
		}
		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update () {
			if (Input.GetMouseButtonDown (0)) {
                if (coroutine != null) {
                    StopCoroutine (coroutine);
                }
                float randomWindValue = Random.Range (0f, 1f); 
                float targetWindMain = Mathf.Lerp (0.15f, 3f, randomWindValue);
                float targetWindTurbulence = Mathf.Lerp (0.2f, 2.2f, randomWindValue);
                Vector3 targetWindDirection = new Vector3 (Random.Range (-1f, 1f), 0f, Random.Range (-1f, 1f));
                float transitionSeconds = Random.Range (1f, 2.5f);
                coroutine = StartCoroutine (WindTo (targetWindMain, targetWindTurbulence, targetWindDirection, transitionSeconds));
                Debug.Log (string.Format ("Target main: {0}, turbulence: {1}, direction: {2}, transition seconds: {3}", 
                    targetWindMain, targetWindTurbulence, targetWindDirection, transitionSeconds)); 
            }
		}
		#endregion

		#region Animations
        /// <summary>
		/// Update Wind values as a transition.
		/// </summary>
		/// <param name="windMain">Game object.</param>
		/// <param name="windTurbulence">Target scale.</param>
        /// <param name="windDirection">Target scale.</param>      
		/// <param name="seconds">Seconds.</param>
		/// <param name="destroyAtEnd">If set to <c>true</c> destroy at end.</param>
		IEnumerator WindTo (float windMain, float windTurbulence, Vector3 windDirection, float seconds){
			float progress = 0;
			float startWindMain = treeController.globalWindMain;
            float startWindTurbulence = treeController.globalWindTurbulence;
            Vector3 startWindDirection = treeController.globalWindDirection;
			float finalWindMain = windMain;
            float finalWindTurbulence = windTurbulence;
            Vector3 finalWindDirection = windDirection;
            Vector3 _windDir;
            Quaternion fromToQ = Quaternion.FromToRotation (startWindDirection, finalWindDirection);
            UpdateWindArrow (targetWindArrow, finalWindDirection);
			while (progress <= 1) {
                _windDir = Quaternion.Slerp (Quaternion.identity, fromToQ, progress) * startWindDirection;
                treeController.UpdateWind (
                    Mathf.Lerp (startWindMain, finalWindMain, progress), 
                    Mathf.Lerp (startWindTurbulence, finalWindTurbulence, progress), 
                    _windDir);
                UpdateWindArrow (windArrow, _windDir);
				progress += Time.deltaTime * (1f / seconds);
				yield return null;
			}
			treeController.UpdateWind (finalWindMain, finalWindTurbulence, finalWindDirection);
		}
        void UpdateWindArrow (GameObject arrow, Vector3 windDirection) {
            if (arrow != null) {
                arrow.transform.forward = windDirection;
            }
        }
		#endregion
	}
}