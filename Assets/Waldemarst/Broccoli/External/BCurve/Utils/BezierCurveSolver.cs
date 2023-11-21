using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Broccoli.Model;

namespace Broccoli.Utils
{
	public class BezierCurveSolver {
		#region Structs
		public struct DistributionParams {
			/// <summary>
			/// The frequency of points.
			/// </summary>
			public Vector2 frequency;
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
			public DistributionMode distributionMode;
			/// <summary>
			/// The children points per step.
			/// </summary>
			public int pointsPerStep;
			/// <summary>
			/// The probability of producing points.
			/// </summary>
			public float probability;
			/// <summary>
			/// Variance applied to spacing variation on a distribuition group.
			/// </summary>
			public float spacingVariance;
			/// <summary>
			/// Variance applied to angle variation on a distribuition group.
			/// </summary>
			public float angleVariance;
			/// <summary>
			/// The distribution curve.
			/// </summary>
			public AnimationCurve spreadCurve;// = AnimationCurve.Linear (0f, 0f, 1f, 1f);
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
			public OriginMode originMode;
			/// <summary>
			/// The twirl value around the parent curve.
			/// </summary>
			public Vector2 twirl;
			/// <summary>
			/// Use randomized twirl offset if enabled.
			/// </summary>
			public bool randomTwirlOffsetEnabled;
			/// <summary>
			/// The global twirl offset.
			/// </summary>
			public float twirlOffset;
			/// <summary>
			/// Range.
			/// </summary>
			public Vector2 range;
			/// <summary>
			/// Masked range.
			/// </summary>
			public Vector2 maskRange;
			/// <summary>
			/// DistributionParams constructor.
			/// </summary>
			/// <param name="frequency">Frequency of elements to generate..</param>
			/// <param name="probability">Probrability to produce points.</param>
			public DistributionParams (
				Vector2 frequency, 
				DistributionMode distributionMode = DistributionMode.Alternative,
				int pointsPerStep = 1,
				float probability = 1f
			) {
				this.frequency = frequency;
				this.distributionMode = distributionMode;
				this.pointsPerStep = pointsPerStep;
				this.probability = probability;
				this.spacingVariance = 0f;
				this.angleVariance = 0f;
				this.spreadCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
				this.originMode = OriginMode.FromTip;
				this.twirl = Vector2.zero;
				this.randomTwirlOffsetEnabled = true;
				this.twirlOffset = 0f;
				this.range = new Vector2 (0f, 1f);
				this.maskRange = new Vector2 (0f, 1f);
			}
			public void SetDensity (
				Vector2 frequency,
				DistributionMode distributionMode = DistributionMode.Alternative,
				int pointsPerStep = 1,
				float probability = 1f
			) {
				this.frequency = frequency;
				this.distributionMode = distributionMode;
				this.pointsPerStep = pointsPerStep;
				this.probability = probability;
			}
			public void SetSpread (
				float spacingVariance,
				float angleVariance = 0f,
				AnimationCurve spreadAnimationCurve = null,
				OriginMode originMode = OriginMode.FromTip
			) {
				this.spacingVariance = spacingVariance;
				this.angleVariance = angleVariance;
				if (spreadAnimationCurve != null) this.spreadCurve = spreadAnimationCurve;
				else this.spreadCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
				this.originMode = originMode;
			}
			public void SetTwirl (
				Vector2 twirl,
				bool randomTwirlOffsetEnabled = true,
				float twirlOffset = 0f
			) {
				this.twirl = twirl;
				this.randomTwirlOffsetEnabled = randomTwirlOffsetEnabled;
				this.twirlOffset = twirlOffset;
			}
			public void SetRange (
				Vector2 range, 
				Vector2 maskRange
			) {
				this.range = range;
				this.maskRange = maskRange;
			}
		}
		public struct AlignParams {
			/// <summary>
			/// Grade of alignment with the curve at the min range.
			/// </summary>
			public Vector2 parallelAlignAtBase;
			/// <summary>
			/// Grade of alignment with the curve at the max range.
			/// </summary>
			public Vector2 parallelAlignAtTop;
			/// <summary>
			/// The parallel align curve.
			/// </summary>
			public AnimationCurve parallelAlignCurve; // = AnimationCurve.Linear (0f, 0f, 1f, 1f);
			/// <summary>
			/// Grade of alignment with the gravity vector at the min range.
			/// </summary>
			public Vector2 gravityAlignAtBase;
			/// <summary>
			/// Grade of alignment with the gravity vector at the max range.
			/// </summary>
			public Vector2 gravityAlignAtTop;
			/// <summary>
			/// The gravity align curve.
			/// </summary>
			public AnimationCurve gravityAlignCurve; // = AnimationCurve.Linear (0f, 0f, 1f, 1f);
			/// <summary>
			/// Grade of alignment with the horizontal vector at the min range.
			/// </summary>
			public Vector2 horizontalAlignAtBase;
			/// <summary>
			/// Grade of alignment with the horizontal vector at the max range.
			/// </summary>
			public Vector2 horizontalAlignAtTop;
			/// <summary>
			/// The horizontal align curve.
			/// </summary>
			public AnimationCurve horizontalAlignCurve; // = AnimationCurve.Linear (0f, 0f, 1f, 1f);
			/// <summary>
			/// AlignParams constructor.
			/// </summary>
			/// <param name="frequency">Frequency of elements to generate..</param>
			/// <param name="probability">Probrability to produce points.</param>
			public AlignParams (Vector2 parallelAlignAtBase, Vector2 parallelAlignAtTop) {
				this.parallelAlignAtBase = parallelAlignAtBase;
				this.parallelAlignAtTop = parallelAlignAtTop;
				this.parallelAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
				this.gravityAlignAtBase = Vector2.zero;
				this.gravityAlignAtTop = Vector2.zero;
				this.gravityAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
				this.horizontalAlignAtBase = Vector2.zero;
				this.horizontalAlignAtTop = Vector2.zero;
				this.horizontalAlignCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
			}
			public void SetParallelAlign (
				Vector2 parallelAlignAtBase,
				Vector2 parallelAlignAtTop,
				AnimationCurve parallelAlignCurve = null)
			{
				this.parallelAlignAtBase = parallelAlignAtBase;
				this.parallelAlignAtTop = parallelAlignAtTop;
				this.parallelAlignCurve = parallelAlignCurve;
			}

			public void SetGravityAlign (
				Vector2 gravityAlignAtBase,
				Vector2 gravityAlignAtTop,
				AnimationCurve gravityAlignCurve = null)
			{
				this.gravityAlignAtBase = gravityAlignAtBase;
				this.gravityAlignAtTop = gravityAlignAtTop;
				this.gravityAlignCurve = gravityAlignCurve;
			}

			public void SetHorizontalAlign (
				Vector2 horizontalAlignAtBase,
				Vector2 horizontalAlignAtTop,
				AnimationCurve horizontalAlignCurve = null)
			{
				this.horizontalAlignAtBase = horizontalAlignAtBase;
				this.horizontalAlignAtTop = horizontalAlignAtTop;
				this.horizontalAlignCurve = horizontalAlignCurve;
			}
		}
		#endregion

		#region Vars
		public DistributionParams distributionParameters = new DistributionParams (new Vector2(1, 10));
		public AlignParams alignParameters = new AlignParams (Vector2.zero, Vector2.zero);
		public List<float> _curvePositions = new List<float> ();
		public List<float> _rollAngles = new List<float> ();
		Vector3 gravityVector = Vector3.down;
		Vector3 gravityPerpendicularVector = Vector3.forward;
		Plane horizontalPlane = new Plane (Vector3.up, Vector3.zero);
		#endregion

		#region Points Solving
		/// <summary>
		/// Get a list of points from a curve.
		/// </summary>
		/// <param name="curve">Target bezier curve.</param>
		/// <returns>List of CurvePoint instances.</returns>
		public List<CurvePoint> GetPoints (BezierCurve curve) {
			return GetPoints (curve, ref this.distributionParameters, ref this.alignParameters);
		}
		/// <summary>
		/// Get a list of points from a curve.
		/// </summary>
		/// <param name="curve">Target bezier curve.</param>
		/// <param name="distributionParams"></param>
		/// <returns>List of CurvePoint instances.</returns>
		public List<CurvePoint> GetPoints (BezierCurve curve, ref DistributionParams distributionParams, ref AlignParams alignParams) {
			List<CurvePoint> points = new List<CurvePoint> ();
			_curvePositions.Clear ();
			_rollAngles.Clear ();

			// Process PROBABILITY.
			float probability = UnityEngine.Random.Range (0f, 1f);
			if (distributionParams.probability < probability) return points;

			// Process FREQUENCY.
			int frequency = UnityEngine.Random.Range ((int)distributionParams.frequency.x, (int)distributionParams.frequency.y + 1);
			if (frequency <= 0) return points;

			// Process points per STEP.
			int pointsPerStep = GetPointsPerStep (ref distributionParams);

			// Intra STEP information.
			float intraNodesAngle = Mathf.PI * 2f / (float)pointsPerStep;
			int steps = Mathf.CeilToInt (frequency / (float)pointsPerStep);
			float positionPerNode = 1f / (float)steps;
			int pointsAdded = 0;

			// Angular difference between neighbour STEPs.
			float angleBetweenSteps = Mathf.PI * 2f / (float)(pointsPerStep + 1);
			float accumAngleBetweenSteps = 0f;

			// Positioning.
			float stepPosition;
			float halfPointPosition = positionPerNode / 2f;

			// TWIRL.
			float twirlToAdd = Mathf.PI * UnityEngine.Random.Range (distributionParams.twirl.x, distributionParams.twirl.y);
			float twirlOffset;
			if (distributionParams.randomTwirlOffsetEnabled) {
				twirlOffset = UnityEngine.Random.Range (-1f, 1f);
			} else {
				twirlOffset = distributionParams.twirlOffset;
			}
			twirlOffset *= Mathf.PI;
			float halfTwirlStep = intraNodesAngle / 2f;

			// For every node
			for (int i = 0; i < steps; i++) {
				// Calculate the STEP POSITION,
				stepPosition = (i + 1) * positionPerNode;
				stepPosition = Mathf.Clamp(distributionParams.spreadCurve.Evaluate(stepPosition), 0f, 1f);

				// Adjust STEP POSITION to RANGE.
				if (distributionParams.originMode == DistributionParams.OriginMode.FromBase) {
					stepPosition = 1 - stepPosition;
				}
				stepPosition = Mathf.Lerp (distributionParams.range.x, distributionParams.range.y, stepPosition);

				// Add TWIRL STEP.
				twirlToAdd = Mathf.PI * UnityEngine.Random.Range (distributionParams.twirl.x, distributionParams.twirl.y);

				// Get STEP POINTS.
				for (int j = 0; j < pointsPerStep; j++) {
					float curvePosition = stepPosition;
					/*
					BroccoTree.Point spawnedPoint = new BroccoTree.Point ();
					spawnedPoint.fromBranchCenter = fromBranchCenter;
					spawnedPoint.helperStructureLevelId = helperId;
					*/
					// Position.
					curvePosition = Mathf.Clamp (
						stepPosition + (UnityEngine.Random.Range (-halfPointPosition, halfPointPosition) * distributionParams.spacingVariance),
						distributionParams.range.x, 
						distributionParams.range.y);

					// Twirl.
					float rollAngle = accumAngleBetweenSteps + (intraNodesAngle * j) + (twirlToAdd * i) + 
						(UnityEngine.Random.Range (-halfTwirlStep, halfTwirlStep) * distributionParams.angleVariance) + twirlOffset;
					/*
					// Twirl.
					spawnedPoint.rollAngle = accumAngleBetweenSteps + (intraNodesAngle * j) + (twirlToAdd * i) + 
						(Random.Range (-halfTwirlStep, halfTwirlStep) * distributionAngleVariance) + twirlOffset;
					
					SetPointRelativeAngle (spawnedPoint, 
						minParallelAlignAtBase, maxParallelAlignAtBase, minParallelAlignAtTop, maxParallelAlignAtTop, parallelAlignCurve,
						minGravityAlignAtBase, maxGravityAlignAtBase, minGravityAlignAtTop, maxGravityAlignAtTop, gravityAlignCurve,
						minHorizontalAlignAtBase, maxHorizontalAlignAtBase, minHorizontalAlignAtTop, maxHorizontalAlignAtTop, horizontalAlignCurve,
						flipAlign, flipAlignDirection, normalRandomness);
					spawnedPoints.Add (spawnedPoint);
					*/
					_curvePositions.Add (curvePosition);
					_rollAngles.Add (rollAngle);
					pointsAdded++;
					if (pointsAdded > frequency) break;
				}
				accumAngleBetweenSteps += angleBetweenSteps;
			}

			// Adjust POINT POSITION to RANGE and create those in the MASK.
			Vector3 originalForward;
			Vector3 originalNormal;
			Vector3 horizontalVector;
			float normalizedPosition;
			float parallelAlign;
			float gravityAlign;
			float horizontalAlign;
			float planeAngle;
			for (int i = 0; i < _curvePositions.Count; i++) {
				if (IsPointInRange (_curvePositions [i], distributionParams.maskRange.x, distributionParams.maskRange.y)) {
					CurvePoint pointToAdd = curve.GetPointAt (_curvePositions [i]);
					originalForward = pointToAdd.forward;
					originalNormal = pointToAdd.normal;

					// TWIRL.
					pointToAdd.Roll (_rollAngles [i]);
					Vector3 fwd = pointToAdd.forward;
					pointToAdd.forward = pointToAdd.normal;
					pointToAdd.normal = -fwd;

					// PARALLEL ALIGN.
					normalizedPosition = Mathf.InverseLerp (distributionParams.range.x, distributionParams.range.y, _curvePositions [i]);
					parallelAlign = Mathf.Lerp (
						UnityEngine.Random.Range (alignParams.parallelAlignAtBase.x, alignParams.parallelAlignAtBase.y),
						UnityEngine.Random.Range (alignParams.parallelAlignAtTop.x, alignParams.parallelAlignAtTop.y),
						normalizedPosition
					);
					pointToAdd.LookAt (originalForward, originalNormal, parallelAlign);

					// HORIZONTAL ALIGN.
					horizontalAlign = Mathf.Lerp (
						UnityEngine.Random.Range (alignParams.horizontalAlignAtBase.x, alignParams.horizontalAlignAtBase.y),
						UnityEngine.Random.Range (alignParams.horizontalAlignAtTop.x, alignParams.horizontalAlignAtTop.y),
						normalizedPosition
					);
					horizontalVector = Vector3.ProjectOnPlane (pointToAdd.forward, gravityVector);
					if (horizontalVector != Vector3.zero) {
						pointToAdd.LookAt (horizontalVector, -gravityVector, horizontalAlign);
						planeAngle = Vector3.SignedAngle (-gravityVector, pointToAdd.bitangent, pointToAdd.forward);
						if (Mathf.Abs (planeAngle) > 90) {
							if (planeAngle < 0) {
								planeAngle += 180f;
							} else {
								planeAngle -= 180f;
							}
						}
						pointToAdd.Roll (-planeAngle * horizontalAlign * Mathf.Deg2Rad);
					}

					// VERTICAL ALIGN.
					gravityAlign = Mathf.Lerp (
						UnityEngine.Random.Range (alignParams.gravityAlignAtBase.x, alignParams.gravityAlignAtBase.y),
						UnityEngine.Random.Range (alignParams.gravityAlignAtTop.x, alignParams.gravityAlignAtTop.y),
						normalizedPosition 
					);
					pointToAdd.LookAt (-gravityVector, gravityPerpendicularVector, gravityAlign);
					points.Add (pointToAdd);
				}
			}

			return points;
		}
		#endregion

		#region Points Solving Utils
		/// <summary>
		/// Get the number of children elements per step.
		/// </summary>
		/// <returns>Number of children per node.</returns>
		/// <param name="level">Structure level.</param>
		protected int GetPointsPerStep (ref DistributionParams distributionParams) {
			int pointsPerStep = 1;
			if (distributionParams.distributionMode == DistributionParams.DistributionMode.Opposite) {
				pointsPerStep = 2;
			} else if (distributionParams.distributionMode == DistributionParams.DistributionMode.Whorled) {
				pointsPerStep = distributionParams.pointsPerStep;
			}
			return pointsPerStep;
		}
		/// <summary>
		/// Determines if the point position is withing range.
		/// </summary>
		/// <returns><c>true</c> if the point is within range; otherwise, <c>false</c>.</returns>
		/// <param name="position">Position.</param>
		/// <param name="minActionRange">Minimum action range position.</param>
		/// <param name="maxActionRange">Max action range position.</param>
		public static bool IsPointInRange (float position, float minActionRange, float maxActionRange) {
			if (position < maxActionRange + 0.0001f && position > minActionRange - 0.0001f) {
				return true;
			}
			return false;
		}
		#endregion
	}
}
