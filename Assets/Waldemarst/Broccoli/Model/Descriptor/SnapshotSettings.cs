using System.Collections.Generic;

using UnityEngine.Serialization;

using UnityEngine;

namespace Broccoli.Pipe {
	/// <summary>
	/// ScriptableObject wrap for the BranchDescriptorCollection class.
	/// </summary>
    #if BROCCOLI_DEVEL
    [CreateAssetMenu(fileName = "SnapshotSettings", menuName = "Broccoli Devel/Snapshot Settings")]
    #endif   
	public class SnapshotSettings : ScriptableObject {
        #region Main Vars
        /// <summary>
        /// Name to call the snapshot type.
        /// </summary>
        public string snapshotType = "Frond";
        public int processorId = 0; 
        public bool hasFlip = false;
        #endregion

        #region Level Vars
        public bool enableSelectActiveLevels = true;
        public int maxSelectableActiveLevel = 3;
        public int defaultActiveLevels = 2; // From zero index.
        public bool hasRange = true;
        public int curveLevelLimit = -1;
        #endregion

        #region Girth Vars
        [Header("Girth")]
        public float girthAtBaseMinLimit = 0.005f;
        public float girthAtBaseMaxLimit = 0.1f;
        public float girthAtTopMinLimit = 0.001f;
        public float girthAtTopMaxLimit =  0.02f;
        #endregion

        #region Bend Vars
        [Header("Bending")]
        public float fBendingScale = 1f;
        public float sBendingScale = 1f;
        public bool hasSBending = true;
        #endregion

        #region Noise Vars
        [Header("Noise")]
        public float noiseResolution = 4f;
        public float noiseStrength = 0.05f;
        #endregion

        #region Stem Level
        [Header("Stem level")]
        public bool stemFrequencyEnabled = true;
        public int stemMinFrequency = 1;
        public int stemMaxFrequency = 12;
        public float stemMinLength = 0.1f;
        public float stemMaxLength = 2f;
        public bool stemRadiusEnabled = false;
        public float stemMinRadius = 0f;
        public float stemMaxRadius = 2f;
        public bool hasStemOffset = false;
        public int stemMinSproutFrequency = 0;
        public int stemMaxSproutFrequency = 25;
        #endregion

        #region Level 1
        [Header("Level 1")]
        [FormerlySerializedAs("levelMinFrequency")]
        public int level1MinFrequency = 0;
        [FormerlySerializedAs("levelMaxFrequency")]
        public int level1MaxFrequency = 12;
        [FormerlySerializedAs("levelMinLengthAtBase")]
        public float level1MinLengthAtBase = 0.1f;
        [FormerlySerializedAs("levelMaxLengthAtBase")]
        public float level1MaxLengthAtBase = 2f;
        [FormerlySerializedAs("levelMinLengthAtTop")]
        public float level1MinLengthAtTop = 0.1f;
        [FormerlySerializedAs("levelMaxLengthAtTop")]
        public float level1MaxLengthAtTop = 2f;
        [FormerlySerializedAs("levelPlaneAlignmentEnabled")]
        public bool level1PlaneAlignmentEnabled = false;
        public int level1MinSproutFrequency = 0;
        public int level1MaxSproutFrequency = 25;
        #endregion

        #region Level 2
        [Header("Level 2")]
        public int level2MinFrequency = 0;
        public int level2MaxFrequency = 12;
        public float level2MinLengthAtBase = 0.1f;
        public float level2MaxLengthAtBase = 2f;
        public float level2MinLengthAtTop = 0.1f;
        public float level2MaxLengthAtTop = 2f;
        public bool level2PlaneAlignmentEnabled = false;
        public int level2MinSproutFrequency = 0;
        public int level2MaxSproutFrequency = 25;
        #endregion

        #region Level 3
        [Header("Level 3")]
        public int level3MinFrequency = 0;
        public int level3MaxFrequency = 12;
        public float level3MinLengthAtBase = 0.1f;
        public float level3MaxLengthAtBase = 2f;
        public float level3MinLengthAtTop = 0.1f;
        public float level3MaxLengthAtTop = 2f;
        public bool level3PlaneAlignmentEnabled = false;
        public int level3MinSproutFrequency = 0;
        public int level3MaxSproutFrequency = 25;
        #endregion

        #region Crown
        [Header("Crown")]
        public bool hasCrown = false; // Used for topmost flowers.
        public string crownName = "Flowers";
        public string crownTooltip = "Flower settings.";
        public float crownMinRange = 0f;
        public float crownMaxRange = 0.4f;
        public float crownMinProbability = 0.2f;
        public int crownMinFrequency = 1;
        public int crownMaxFrequency = 5;
        #endregion

        #region Singleton
        
        private static bool isInit = false;
        private static SnapshotSettings _instance;
        private static Dictionary<string, SnapshotSettings> _snapSettings = new Dictionary<string, SnapshotSettings> ();
        private static void Init () {
            if (!isInit) {
                #if UNITY_EDITOR
                SnapshotSettings[] assets = Resources.LoadAll<SnapshotSettings>("");
                _snapSettings.Clear ();
                _instance = ScriptableObject.CreateInstance <SnapshotSettings> ();
                if (assets != null && assets.Length > 0) {
                    for (int i = 0; i < assets.Length; i++) {
                        if (!_snapSettings.ContainsKey (assets[i].snapshotType)) {
                            _snapSettings.Add (assets[i].snapshotType, assets [i]);
                        }
                    }
                }
                #endif
                isInit = true;
            }
        }
        public static SnapshotSettings Get (string snapshotType = null) {
            Init ();
            if (!string.IsNullOrEmpty (snapshotType) && snapshotType.Equals("Main")) {
                snapshotType = "Frond";
            }
            if (string.IsNullOrEmpty (snapshotType) || !_snapSettings.ContainsKey(snapshotType)) {
                if (_snapSettings.ContainsKey ("Frond")) return _snapSettings ["Frond"];
                else return _instance;
            } else {
                return _snapSettings [snapshotType];
            }
        }
        public static Dictionary<string, SnapshotSettings> GetAll () {
            Init ();
            return _snapSettings;
        }
        #endregion
	}
}