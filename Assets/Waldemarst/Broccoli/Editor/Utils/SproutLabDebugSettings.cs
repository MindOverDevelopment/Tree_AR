using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Broccoli.BroccoEditor
{
    #if BROCCOLI_DEVEL
    [CreateAssetMenu(fileName = "SproutLab Debug Settings", menuName = "Broccoli Devel/SproutLab Debug Settings")]
    #endif
    public class SproutLabDebugSettings : ScriptableObject {
        #region Vars
        public bool snapshotInfoFoldout = true;
        public bool snapshotMeshFoldout = true;
        public bool snapshotProcessFoldout = true;
        public bool snapshotTextureFoldout = true;
        public bool snapshotFragsFoldout = true;
        public bool snapshotPolysFoldout = true;
        public int snapshotLODIndex = 0;
        public int snapshotResolution = 0;
        public bool wireframeEnabled = false;
        public Color wireframeBaseColor = Color.white;
        public bool snapshotShowCurve = false;
        public bool showCurveNodes = true;
        public bool showCurveNodeForward = true;
        public bool showCurveNodeUp = true;
        public bool showCurvePoints = true;
        public bool showCurvePointForward = true;
        public bool showCurvePointNormal = true;
        public bool showCurvePointUp = true;
        public bool showCurvePointTangent = true;
        public bool showMeshBounds = false;
        public Vector3 targetPosition = Vector3.zero;
        public float targetScale = 1f;
        public int targetBendMode = 0;
        public float targetBendForward = 0f;
        public float targetBendSide = 0f;
        public Vector3 targetOrentation = Vector3.zero;
        public Vector3 targetOrientationForward = Vector3.forward;
        public Vector3 targetOrientationNormal = Vector3.right;
        public Vector3 targetOrientationEuler = Vector3.zero;
        public Mesh targetMesh = null;
        public Vector3 targetMeshScale = Vector3.one;
        public Vector3 targetMeshRotation = Vector3.zero;
        public Vector3 targetPivot = Vector3.zero;
        public float targetWidth = 1f;
        public float targetHeight = 1f;
        public float targetWidthPivot = 0.5f;
        public float targetHeightPivot = 0f;
        public int targetWidthSegments = 3;
        public int targetHeightSegments = 3;
        public int targetPlanes = 1;
        public float targetDepth = 0f;
        public Material targetMaterial = null;

        public Vector3 gravityDirection = Vector3.down;
        public bool showGravityDirection = false;
        public Vector3 gravityDirectionPos = Vector3.one;
        public float gravityDirectionScale = 1f; 
        public Color gravityDirectionColor = Color.yellow;

        public bool meshShowNormals = false;
        public bool meshShowTangents = false;
        #endregion

        #region Singleton
        private static SproutLabDebugSettings _instance;
        public static SproutLabDebugSettings instance {
            get {
                if (_instance == null) { 
                    SproutLabDebugSettings[] assets = Resources.LoadAll<SproutLabDebugSettings>("");
                    if (assets == null || assets.Length < 1) {
                        _instance = ScriptableObject.CreateInstance <SproutLabDebugSettings> ();
                    } else {
                        _instance = assets [0];
                    }
                }
                return _instance;
            }
        }
        #endregion
    }
}