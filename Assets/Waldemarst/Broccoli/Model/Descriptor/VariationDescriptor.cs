using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;

namespace Broccoli.Pipe {
    /// <summary>
    /// Composite variation container class.
    /// </summary>
    [System.Serializable]
    public class VariationDescriptor {
        #region Variation Group Cluster
        [System.Serializable]
        public class VariationUnit {
            public float random = 0f;
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 orientation = Vector3.right;
            public bool flip = false;
            public float scale = 1f;
            public float fBending = 0f;
            public float sBending = 0f;
            public float fBendingScale = 1f;
            public float sBendingScale = 1f;
            public Vector3 offset = Vector3.zero;
            public int snapshotIndex = -1;
            public int snapshotId = -1;
            public int snapshotLods = 0;
            public BezierCurve curve = null;
        }
        [System.Serializable]
        public class VariationGroupCluster {
            #region Vars
            public int groupId = 0;
            public float radius = 0f;
            public float centerFactor = 0f;
            public List<VariationUnit> variationUnits = new List<VariationUnit> ();
            #endregion
        }
        #endregion

        #region Structure Vars
        public int id = 0;
        public int seed = 0;
        [System.NonSerialized]
        public Dictionary<int,VariationGroupCluster> variationGroupClusters = new Dictionary<int,VariationGroupCluster> ();
        public List<VariationGroup> variationGroups = new List<VariationGroup> ();
        /// <summary>
        /// Id to VariationGroup instance.
        /// </summary>
        [System.NonSerialized]
        public Dictionary<int, VariationGroup> idToVariationGroup = new Dictionary<int, VariationGroup> ();
        /// <summary>
        /// List of snapshot ids used by this variation.
        /// </summary>
        public List<int> snapshotIds = new List<int> ();
        /// <summary>
        /// List of snapshot ids and lods used by this variation.
        /// </summary>
        [System.NonSerialized]
        public List<(int, int)> snapshotIdsLods = new List<(int, int)> ();
        /// <summary>
        /// List of texture hashes per submesh.
        /// </summary>
        /// <typeparam name="string">Hash of texture.</typeparam>
        /// <returns>List of hashes, with index as submesh.</returns>
        [System.NonSerialized]
        public List<Hash128> hashes = new List<Hash128> ();
        /// <summary>
        /// Canvas offset.
        /// </summary>
        public Vector2 canvasOffset = Vector2.zero;
        #endregion

        #region Constructor
        public VariationDescriptor () {}
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public VariationDescriptor Clone () {
            VariationDescriptor clone = new VariationDescriptor ();
            clone.id = id;
            clone.seed = seed;
            for (int i = 0; i < variationGroups.Count; i++) {
                clone.variationGroups.Add (variationGroups [i].Clone ());
            }
            clone.canvasOffset = canvasOffset;
            return clone;
        }
        #endregion

        #region Groups Management
        public void BuildGroupTree () {
            // Populate dictionary.
            idToVariationGroup.Clear ();
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i] != null) {
                    if (!idToVariationGroup.ContainsKey (variationGroups [i].id)) {
                        idToVariationGroup.Add (variationGroups [i].id, variationGroups [i]);
                    }
                }
            }
            // Build groups hierarchy.
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i].parentId >= 0 && idToVariationGroup.ContainsKey (variationGroups [i].parentId)) {
                    variationGroups [i].parent = idToVariationGroup [variationGroups [i].parentId];
                } else {
                    variationGroups [i].parent = null;
                }
            }
        }
        /// <summary>
        /// Adds a Variation Group to this Variation Descriptor.
        /// </summary>
        /// <param name="groupToAdd"></param>
        public void AddGroup (VariationGroup groupToAdd) {
            groupToAdd.id = GetGroupId ();
            variationGroups.Add (groupToAdd);
            idToVariationGroup.Add (groupToAdd.id, groupToAdd);
            if (groupToAdd.parentId >= 0 && idToVariationGroup.ContainsKey (groupToAdd.parentId)) {
                groupToAdd.parent = idToVariationGroup [groupToAdd.parentId];
            } else {
                groupToAdd.parent = null;
            }
        }
        public bool RemoveGroup (int groupId) {
            if (idToVariationGroup.ContainsKey (groupId)) {
                VariationGroup groupToRemove = idToVariationGroup [groupId];
                if (variationGroups.Contains (groupToRemove)) {
                    variationGroups.Remove (groupToRemove);
                }
                groupToRemove.parent = null;
                idToVariationGroup.Remove (groupId);
                return true;
            }
            return false;
        }
        int GetGroupId () {
            int id = 0;
            for (int i = 0; i < variationGroups.Count; i++) {
                if (variationGroups [i] != null) {
                    if (variationGroups [i].id >= id) {
                        id = variationGroups [i].id + 1;
                    }
                }
            }
            return id;
        }
        #endregion
    }
}