using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using Broccoli.Pipe;

namespace Broccoli.Factory
{
    /// <summary>
    /// Base class for snapshot processors.
    /// </summary>
    [SnapshotProcessor (0)]
    public class SnapshotPlanarProcessor : SnapshotProcessor {
        #region Vars
        protected float biasMinPlaneAlign = 0f;
        protected float biasMaxPlaneAlign = 0f;
        #endregion

        #region Bias
        /// <summary>
        /// Gets the fragmentation parameters according to the
        /// tree max hierarchy level and the LOD.
        /// </summary>
        /// <param name="maxLevel">Tree max hierarchy level.</param>
        /// <param name="lod">Level of detail. From 0 to 2.</param>
        /// <param name="fragLevels">How many fragmentation levels to support. From 1 to n.</param>
        /// <param name="minFragLevel">Where the fragmentation level begins. From 0 to n.</param>
        /// <returns>Fragmentation bias type to generate the fragments.</returns>
        public override FragmentBias GetFragmentationBias (
            int maxLevel, 
            int lod, 
            out int fragLevels,
            out int minFragLevel,
            out int maxFragLevel)
        {
            // For LOD 2
            fragLevels = 1;
            minFragLevel = 0;
            maxFragLevel = 0;
            // For LOD 0, LOD 1
            if (lod == 0 || lod == 1) {
                if (maxLevel > 1) {
                    minFragLevel = 1;
                    maxFragLevel = 1;
                }
            }
            if (lod == 2) {
                return FragmentBias.None;
            }
            return FragmentBias.PlaneAlignment;
        }
        /// <summary>
        /// Sets the fragmentation method to directional bias, which takes the 
        /// angle of the children branches to create the fragments.
        /// </summary>
        /// <param name="minPlaneAlign">Minimum plane alignment.</param>
        /// <param name="maxPlaneAlign">Maximum plane alignment.</param>
        public void SetFragmentsDirectionalBias (
            float minPlaneAlign, 
            float maxPlaneAlign)
        {
            biasMinPlaneAlign = minPlaneAlign;
            biasMaxPlaneAlign = maxPlaneAlign;
            fragmentBias = FragmentBias.PlaneAlignment;
        }
        public void SetNoFragmentBias () {
            fragmentBias = FragmentBias.None;
        }
        #endregion

        #region Fragment Bias
        /// <summary>
        /// Generates the fragments for a branch descriptor at a specific LOD.
        /// </summary>
        /// <param name="lodLevel">Level of detail.</param>
        /// <param name="snapshot">Branch descriptor.</param>
        /// <returns>List of fragments for the LOD.</returns>
        public override List<Fragment> GenerateSnapshotFragments (
            int lodLevel,
            BranchDescriptor snapshot)
        {
            // Get Bias.
            int treeMaxLevel = tree.GetOffspringLevel ();
            int fragLevelss, minsFragLevel, maxsFragLevel;
            FragmentBias fragmentBias = GetFragmentationBias (treeMaxLevel, lodLevel,
                out fragLevelss, out minsFragLevel, out maxsFragLevel);

            // Define the plane alignment.
            if (fragmentBias == FragmentBias.PlaneAlignment) {

                BranchDescriptor.BranchLevelDescriptor branchLevelDesc;
                branchLevelDesc = snapshot.branchLevelDescriptors [1];
                SetFragmentsDirectionalBias (
                    branchLevelDesc.minPlaneAlignAtBase, branchLevelDesc.maxPlaneAlignAtBase);
            } else {
                SetNoFragmentBias ();
            }
            
            // Get Levels.
            List<Fragment> fragments;
            switch (fragmentBias) {
                case FragmentBias.PlaneAlignment:
                    fragments = GeneratePlaneAlignmentFragments (lodLevel, snapshot);
                    break;
                default:
                    fragments = GenerateNonBiasFragments (lodLevel, snapshot);
                    break;
            }
            return fragments;
        }
        public List<Fragment> GenerateNonBiasFragments (int lodLevel, BranchDescriptor snapshot) {
            List<Fragment> fragments = new List<Fragment> ();
            for (int i = 0; i < tree.branches.Count; i++) {
                Fragment baseFragment = new Fragment ();
                baseFragment.baseBranchId = tree.branches [i].id;
                baseFragment.offset = tree.branches [i].originOffset;
                baseFragment.minLevel = 0;
                fragments.Add (baseFragment);
            }
            return fragments;
        }
        public List<Fragment> GeneratePlaneAlignmentFragments (int lodLevel, BranchDescriptor snapshot) {
            List<Fragment> fragments = new List<Fragment> ();
            List<BroccoTree.Branch> outBranches = new List<BroccoTree.Branch> ();
            float threshold = 1f;
            if (lodLevel == 0) {
                threshold = 0.17f;
            } else if (lodLevel == 1) {
                threshold = 0.5f;
            } else {
                threshold = 0.7f;
            }
            // Get the threshold range.
            float planeRange = (biasMaxPlaneAlign - biasMinPlaneAlign);
            float midPlane = planeRange / 2f + biasMinPlaneAlign;
            float minThreshold = midPlane - planeRange * 0.5f * threshold;
            float maxThreshold = midPlane + planeRange * 0.5f * threshold;
            // List of branches candidates for base.
            List<BroccoTree.Branch> planeGroup = new List<BroccoTree.Branch> ();
            
            // Process each root branch.
            float branchThresholdPos;
            float followingBranchThresholdPos;
            for (int rootI = 0; rootI < tree.branches.Count; rootI++) {
                // Set the base branch.
                BroccoTree.Branch baseBranch = tree.branches [rootI];
                // Sort children branches by directional angle.
                baseBranch.branches.Sort ((a, b) => a.direction.x.CompareTo (b.direction.x));
                // Iterate each child branch to get a ranged planeGroupCandidate.
                for (int i = 0; i < baseBranch.branches.Count; i++) {
                    // Create the plane group candidate.
                    List<BroccoTree.Branch> planeGroupCandidate = new List<BroccoTree.Branch> ();
                    // Add the initial branch.
                    planeGroupCandidate.Add (baseBranch.branches [i]);
                    branchThresholdPos = Mathf.InverseLerp (minThreshold, maxThreshold, baseBranch.branches [i].direction.x);
                    // Iterate the branches that follow and add them if the fall into the threshold.
                    if (i < baseBranch.branches.Count - 1) {
                         for (int j = i + 1; j < baseBranch.branches.Count; j++) {
                            followingBranchThresholdPos = Mathf.InverseLerp (minThreshold, maxThreshold, baseBranch.branches [j].direction.x);
                            if (followingBranchThresholdPos - branchThresholdPos < threshold) {
                                planeGroupCandidate.Add (baseBranch.branches [j]);
                            } else {
                                break;
                            }
                         }
                    }
                    if (planeGroupCandidate.Count > planeGroup.Count) {
                        planeGroup = planeGroupCandidate;
                    }
                }
                // Get the branches not belonging to the base plane.
                for (int i = 0; i < baseBranch.branches.Count; i++) {
                    if (!planeGroup.Contains (baseBranch.branches [i])) {
                        outBranches.Add (baseBranch.branches [i]);
                    }
                }
                // Create the base fragment.
                Fragment baseFragment = new Fragment ();
                baseFragment.baseBranchId = tree.branches [rootI].id;
                baseFragment.offset = tree.branches [rootI].originOffset;
                baseFragment.minLevel = 0;
                // If the base fragment has no branches, remove one from the outbranches.
                if (outBranches.Count > 0 && outBranches.Count == baseBranch.branches.Count) {
                    outBranches.RemoveAt (0);
                }
                for (int j = 0; j < outBranches.Count; j++){
                    baseFragment.excludes.Add (outBranches [j].guid);
                    baseFragment.excludeIds.Add (outBranches [j].id);
                }
                fragments.Add (baseFragment);
                // Create the child fragments, one per each branch out of range.
                for (int j = 0; j < outBranches.Count; j++) {
                    Fragment childFragment = new Fragment ();
                    childFragment.offset = outBranches [j].GetPointAtPosition (0f);
                    childFragment.minLevel = 1;
                    childFragment.includes.Add (outBranches [j].guid);
                    childFragment.includeIds.Add (outBranches [j].id);
                    fragments.Add (childFragment);
                }
            }
            return fragments;
        }
        #endregion
    }
}