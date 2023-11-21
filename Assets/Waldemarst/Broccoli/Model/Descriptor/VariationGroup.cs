using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Broccoli.Model;

namespace Broccoli.Pipe {
    /// <summary>
    /// Variation group class.
    /// </summary>
    [Serializable]
    public class VariationGroup {
        #region Vars
        /// <summary>
        /// Characters used to generate a random name for the framing.
        /// </summary>
        const string glyphs= "abcdefghijklmnopqrstuvwxyz0123456789";
        public int id = 0;
        public string name = "";
        public bool enabled = true;
        public int seed = 0;
        public Vector2 frequency = new Vector2 (1, 4);
        public Vector3 center = Vector3.zero;
        public Vector2 radius = new Vector2 (0, 0);
        public float centerFactor = 0f;
        public enum OrientationMode {
            CenterToPeriphery,
            PeripheryToCenter,
            clockwise,
            counterClockwise
        }
        /// <summary>
        /// Distribution mode.
        /// </summary>
        public enum DistributionMode
        {
            Alternative,
            Opposite,
            Whorled
        }
        /// <summary>
        /// The distribution mode used for this point production.
        /// </summary>
        public DistributionMode distributionMode = DistributionMode.Alternative;
        /// <summary>
        /// The children points per step.
        /// </summary>
        public int pointsPerStep = 1;
        /// <summary>
        /// The probability of producing points.
        /// </summary>
        public float probability = 1f;
        /// <summary>
        /// Variance applied to spacing variation on a distribuition group.
        /// </summary>
        public float spacingVariance = 0f;
        /// <summary>
        /// Variance applied to angle variation on a distribuition group.
        /// </summary>
        public float angleVariance = 0f;
        /// <summary>
        /// The distribution curve.
        /// </summary>
        public AnimationCurve spreadCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
        /// <summary>
        /// Distribution origin.
        /// </summary>
        public enum OriginMode
        {
            FromTip,
            FromBase
        }
        /// <summary>
        /// The distribution origin mode.
        /// </summary>
        public OriginMode originMode = OriginMode.FromTip;
        /// <summary>
        /// Flag to flip the snapshot mesh.
        /// </summary>
        public bool hasFlip = true;
        /// <summary>
        /// The twirl value around the parent curve.
        /// </summary>
        public Vector2 twirl = Vector2.zero;
        /// <summary>
        /// Use randomized twirl offset if enabled.
        /// </summary>
        public bool randomTwirlOffsetEnabled = false;
        /// <summary>
        /// The global twirl offset.
        /// </summary>
        public float twirlOffset = 0f;
        /// <summary>
        /// Range.
        /// </summary>
        public Vector2 range = new Vector2(0f, 1f);
        /// <summary>
        /// Masked range.
        /// </summary>
        public Vector2 maskRange = new Vector2(0f, 1f);
        /// <summary>
        /// Grade of alignment with the curve at the min range.
        /// </summary>
        public Vector2 parallelAlignAtBase = Vector2.zero;
        /// <summary>
        /// Grade of alignment with the curve at the max range.
        /// </summary>
        public Vector2 parallelAlignAtTop = Vector2.zero;
        /// <summary>
        /// The parallel align curve.
        /// </summary>
        public AnimationCurve parallelAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
        /// <summary>
        /// Grade of alignment with the gravity vector at the min range.
        /// </summary>
        public Vector2 gravityAlignAtBase = Vector2.zero;
        /// <summary>
        /// Grade of alignment with the gravity vector at the max range.
        /// </summary>
        public Vector2 gravityAlignAtTop = Vector2.zero;
        /// <summary>
        /// The gravity align curve.
        /// </summary>
        public AnimationCurve gravityAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
        /// <summary>
        /// Grade of alignment with the horizontal vector at the min range.
        /// </summary>
        public Vector2 horizontalAlignAtBase = Vector2.zero;
        /// <summary>
        /// Grade of alignment with the horizontal vector at the max range.
        /// </summary>
        public Vector2 horizontalAlignAtTop = Vector2.zero;
        /// <summary>
        /// The horizontal align curve.
        /// </summary>
        public AnimationCurve horizontalAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
        public OrientationMode orientation = OrientationMode.CenterToPeriphery;
        public float orientationRandomness = 0f;
        public Vector2 tiltAtCenter = new Vector2 (0, 0);
        public Vector2 tiltAtBorder = new Vector2 (0, 0);
        public Vector2 scaleAtCenter = new Vector2 (1, 1);
        public Vector2 scaleAtBorder = new Vector2 (1, 1);
        public enum BendMode {
            CenterToPeriphery,
            PeripheryToCenter,
            clockwise,
            counterClockwise
        }
        public BendMode bendMode = BendMode.CenterToPeriphery;
        public Vector2 bendAtCenter = new Vector2 (0, 0);
        public Vector2 bendAtBorder = new Vector2 (0, 0);
        public BendMode sBendMode = BendMode.CenterToPeriphery;
        public Vector2 sBendAtCenter = new Vector2 (0, 0);
        public Vector2 sBendAtBorder = new Vector2 (0, 0);
        public Vector2 stemOffsetAtCenter = new Vector2 (0, 0);
        public Vector2 stemOffsetAtBorder = new Vector2 (0, 0);
        public List<int> snapshotIds = new List<int> (); 
        public List<int> snapshotLods = new List<int> ();
        public enum GroupType {
            Steam,
            Branching
        }
        public GroupType groupType = GroupType.Steam;
        public bool isSteam {
            get { return groupType == GroupType.Steam;}
        }
        public Vector2 nodePosition = Vector2.zero;
        public int tag = -1;
        public int sidePanelOption = 0;
        public int parentId = -1;
        [System.NonSerialized]
        private VariationGroup _parent = null;
        public VariationGroup parent {
            set {
                if (value != null) {
                    _parent = value;
                    parentId = value.id;
                    if (!_parent.children.Contains (this)) {
                        _parent.children.Add (this);
                    }
                } else {
                    if (_parent != null) {
                        _parent.children.Remove (this);
                    }
                    _parent = null;
                    parentId = -1;
                }
            }
            get { return _parent; }
        }
        [System.NonSerialized]
        public List<VariationGroup> children = new List<VariationGroup> ();
        #endregion

        #region Constants
        public const int SNAPSHOT_LOD_0 = 1;
        public const int SNAPSHOT_LOD_1 = 2;
        public const int SNAPSHOT_LOD_2 = 4;
        #endregion

        #region Constructor
        public VariationGroup () {}
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public VariationGroup Clone () {
            VariationGroup clone = new VariationGroup ();
            clone.id = id;
            clone.name = name;
            clone.enabled = enabled;
            clone.seed = seed;
            clone.frequency = frequency;
            clone.center = center;
            clone.radius = radius;
            clone.centerFactor = centerFactor;
            clone.distributionMode = distributionMode;
            clone.pointsPerStep = pointsPerStep;
			clone.probability = probability;
			clone.spacingVariance = spacingVariance;
			clone.angleVariance = angleVariance;
            clone.spreadCurve = new AnimationCurve (spreadCurve.keys);
			clone.originMode = originMode;
			clone.twirl = twirl;
			clone.randomTwirlOffsetEnabled = randomTwirlOffsetEnabled;
			clone.twirlOffset = twirlOffset;
			clone.range = range;
			clone.maskRange = maskRange;
            clone.parallelAlignAtBase = parallelAlignAtBase;
            clone.parallelAlignAtTop = parallelAlignAtTop;
            clone.parallelAlignCurve = new AnimationCurve (parallelAlignCurve.keys);
            clone.gravityAlignAtBase = gravityAlignAtBase;
            clone.gravityAlignAtTop = gravityAlignAtTop;
            clone.gravityAlignCurve = new AnimationCurve (gravityAlignCurve.keys);
            clone.horizontalAlignAtBase = horizontalAlignAtBase;
            clone.horizontalAlignAtTop = horizontalAlignAtTop;
            clone.horizontalAlignCurve = new AnimationCurve (horizontalAlignCurve.keys);
            clone.orientation = orientation;
            clone.orientationRandomness = orientationRandomness;
            clone.tiltAtCenter = tiltAtCenter;
            clone.tiltAtBorder = tiltAtBorder;
            clone.scaleAtCenter = scaleAtCenter;
            clone.scaleAtBorder = scaleAtBorder;
            clone.bendMode = bendMode;
            clone.bendAtCenter = bendAtCenter;
            clone.bendAtBorder = bendAtBorder;
            clone.sBendAtCenter = sBendAtCenter;
            clone.sBendAtBorder = sBendAtBorder;
            clone.stemOffsetAtCenter = stemOffsetAtCenter;
            clone.stemOffsetAtBorder = stemOffsetAtBorder;
            for (int i = 0; i < snapshotIds.Count; i++) {
                clone.snapshotIds.Add (snapshotIds [i]);
            }
            for (int i = 0; i < snapshotLods.Count; i++) {
                clone.snapshotLods.Add (snapshotLods [i]);
            }
            clone.groupType = groupType;
            clone.nodePosition = nodePosition;
            clone.tag = tag;
            clone.sidePanelOption = sidePanelOption;
            clone.parentId = parentId;
            return clone;
        }
        /// <summary>
		/// Get a random string name.
		/// </summary>
		/// <param name="length">Number of characters.</param>
		/// <returns>Random string name.</returns>
        public static string GetRandomName (int length = 6) {
            string randomName = "";
            UnityEngine.Random.InitState ((int)System.DateTime.Now.Ticks);
            for(int i = 0; i < 6; i++) {
                randomName += glyphs [UnityEngine.Random.Range (0, glyphs.Length)];
            }
            return randomName;
        }
        #endregion

        #region Snapshot Management
        /// <summary>
        /// Adds a snapshot id to be part of this group.
        /// </summary>
        /// <param name="snapshotId">Id fo the snapshot.</param>
        /// <param name="snapshotLODs">Mask for the LODs to include.</param>
        /// <returns><c>True</c> if the snapshot gets added.</returns>
        public bool AddSnapshot (int snapshotId, 
            int snapshotLODs = SNAPSHOT_LOD_0 | SNAPSHOT_LOD_1 | SNAPSHOT_LOD_2)
        {
            if (!snapshotIds.Contains (snapshotId)) {
                AssureSnapshotLods ();
                snapshotIds.Add (snapshotId);
                snapshotLods.Add (snapshotLODs);
            }
            return false;
        }
        /// <summary>
        /// Removes a snapshot from this group given its id.
        /// </summary>
        /// <param name="snapshotId">Id of the snapshot.</param>
        /// <returns><c>True</c> if the snapshot was removed.</returns>
        public bool RemoveSnapshot (int snapshotId) {
            int index = snapshotIds.IndexOf (snapshotId);
            if (index >= 0) {
                AssureSnapshotLods ();
                snapshotIds.RemoveAt (index);
                snapshotLods.RemoveAt (index);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Makes sure the group contains only existing snapshots ids.
        /// </summary>
        /// <param name="validSnapshots">Existing snapshots to validate against.</param>
        public void ValidateSnapshotsIds (List<BranchDescriptor> validSnapshots) {
            List<int> validSnapshotsIds = new List<int> ();
            List<int> validSnapshotsLods = new List<int> ();
            for (int i = 0; i < validSnapshots.Count; i++) {
                if (snapshotIds.Contains (validSnapshots [i].id)) {
                    int indexOfSnapId = snapshotIds.IndexOf (validSnapshots [i].id);
                    validSnapshotsIds.Add (validSnapshots [i].id);
                    if (indexOfSnapId < snapshotLods.Count) {
                        validSnapshotsLods.Add (snapshotLods [indexOfSnapId]);
                    } else {
                        validSnapshotsLods.Add (SNAPSHOT_LOD_0 | SNAPSHOT_LOD_1 | SNAPSHOT_LOD_2);
                    }
                }
            }
            snapshotIds = validSnapshotsIds;
            snapshotLods = validSnapshotsLods;
        }
        public void AssureSnapshotLods () {
            if (snapshotIds.Count > 0 && snapshotLods.Count == 0) {
                for (int i = 0; i < snapshotIds.Count; i++) {
                    snapshotLods.Add (SNAPSHOT_LOD_0 | SNAPSHOT_LOD_1 | SNAPSHOT_LOD_2);
                }
            }
        }
        #endregion

        #region Debug
        public int debugFlags = 0;
        public const int DEBUG_SHOW_ORIENTATION = 1;
        public const int DEBUG_SHOW_SCALE = 2;
        public const int DEBUG_SHOW_FORWARD = 4;
        public const int DEBUG_SHOW_NORMAL = 8;
        public const int DEBUG_SHOW_BITANGENT= 16;
        public bool HasFlags () {
            return debugFlags > 0;
        }
        public bool HasFlag (int flag) {
            return (debugFlags & flag) != 0;
        }
        public void AddFlag (int flag) {
            debugFlags |= flag;
        }
        public void RemoveFlag (int flag) {
            debugFlags ^= flag;
        }
        public string GetDebugInfo () {
            string info = string.Format ("Id: {0}, Name: {1}, Enabled: {2}, Seed: {3}\n", id, name, enabled, seed);
            info += string.Format ("Freq: {0}, Prob: {1}, Flip: {2}\n", frequency, probability, hasFlip);
            info += string.Format ("Center: {0}, Radius: {1}, C Factor: {2}\n", center, radius, centerFactor);
            info += string.Format ("Orientation Mode: {0}, Dist Mode: {1}, Origin Mode: {2}\n", orientation, distributionMode, originMode);
            info += string.Format ("Snapshots: {0}\n", snapshotIds.Count);
            for (int i = 0; i < snapshotIds.Count; i++) {
                info += "  " + snapshotIds [i] + " [lods: " + snapshotLods[i] + "]\n";
            }
            return info;

            /*
            public DistributionMode distributionMode = DistributionMode.Alternative;
            public int pointsPerStep = 1;
            public float spacingVariance = 0f;
            public Vector2 range = new Vector2(0f, 1f);
            public Vector2 maskRange = new Vector2(0f, 1f);
            public AnimationCurve spreadCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);

            public OriginMode originMode = OriginMode.FromTip;
            public Vector2 twirl = Vector2.zero;
            public bool randomTwirlOffsetEnabled = false;
            public float twirlOffset = 0f;
            public float angleVariance = 0f;
            
            public Vector2 parallelAlignAtBase = Vector2.zero;
            public Vector2 parallelAlignAtTop = Vector2.zero;
            public AnimationCurve parallelAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public Vector2 gravityAlignAtBase = Vector2.zero;
            public Vector2 gravityAlignAtTop = Vector2.zero;
            public AnimationCurve gravityAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public Vector2 horizontalAlignAtBase = Vector2.zero;
            public Vector2 horizontalAlignAtTop = Vector2.zero;
            public AnimationCurve horizontalAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public OrientationMode orientation = OrientationMode.CenterToPeriphery;
            public float orientationRandomness = 0f;

            public Vector2 tiltAtCenter = new Vector2 (0, 0);
            public Vector2 tiltAtBorder = new Vector2 (0, 0);

            public Vector2 scaleAtCenter = new Vector2 (1, 1);
            public Vector2 scaleAtBorder = new Vector2 (1, 1);

            public BendMode bendMode = BendMode.CenterToPeriphery;
            public Vector2 bendAtCenter = new Vector2 (0, 0);
            public Vector2 bendAtBorder = new Vector2 (0, 0);
            public BendMode sBendMode = BendMode.CenterToPeriphery;
            public Vector2 sBendAtCenter = new Vector2 (0, 0);
            public Vector2 sBendAtBorder = new Vector2 (0, 0);

            public Vector2 stemOffsetAtCenter = new Vector2 (0, 0);
            public Vector2 stemOffsetAtBorder = new Vector2 (0, 0);

            public List<int> snapshotIds = new List<int> (); 
            public List<int> snapshotLods = new List<int> ();

            public GroupType groupType = GroupType.Steam;
            public bool isSteam {
                get { return groupType == GroupType.Steam;}
            }
            public Vector2 nodePosition = Vector2.zero;
            public int tag = -1;
            public int sidePanelOption = 0;
            */
        }
        #endregion
    }
}