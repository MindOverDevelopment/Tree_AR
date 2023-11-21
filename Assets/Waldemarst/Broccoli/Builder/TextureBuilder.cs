using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Broccoli.Builder
{
    public class TextureBuilder {
        #region Vars
        /// <summary>
		/// The size of the texture to generate.
		/// </summary>
		public Vector2 textureSize = new Vector2(1024, 1024);
        /// <summary>
        /// If true, then one of the dimension of the texture is modified to mantain the 
        /// aspect ratio with the target mesh.
        /// </summary>
        public bool useTextureSizeToTargetRatio = false;
        /// <summary>
        /// Multiplies the bounds used to calculate the texture size.
        /// </summary>
        public Vector3 boundsMultiplier = Vector3.one;
        /// <summary>
        /// Adds an offset to the center of the bounds used to calculate the target position.
        /// </summary>
        public Vector3 boundsOffset = Vector3.zero;
        /// <summary>
        /// Shader to use to render the texture.
        /// </summary>
        public Shader shader = null;
        Plane _cameraPlane = new Plane (Vector3.forward, Vector3.zero);
        Vector3 _cameraUp = Vector3.up;
        Mesh _targetMesh = null;
        GameObject _targetGameObject = null;
        /// <summary>
		/// The camera rendering the texture.
		/// </summary>
		Camera camera = null;
		/// <summary>
		/// Game object containing the camera.
		/// </summary>
		GameObject cameraGameObject = null;
        /// <summary>
		/// Layer to move the object when rendering with the camera.
		/// </summary>
		private const int PREVIEW_LAYER = 22;
        /// <summary>
        /// Debug flag: the texture rendering camera does not get destroyed after usage.
        /// </summary>
        public bool debugKeepCameraAfterUsage = false;
        /// <summary>
        /// Background color for the camera.
        /// </summary>
        public Color backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
        /// <summary>
        /// Pixel format.
        /// </summary>
        public TextureFormat textureFormat = TextureFormat.ARGB32;
        /// <summary>
        /// Use after calculating the required aspect ratio for the textures.
        /// </summary>
        Vector2 finalTextureSize;
        /// <summary>
        /// Temp variable to save the actual render pipeline why rendering textures.
        /// </summary>
        RenderPipelineAsset _graphRP = null;
        /// <summary>
        /// Temp variable to save the actual render pipeline why rendering textures.
        /// </summary>
        RenderPipelineAsset _qualityRP = null;
        #endregion  

        #region Accessors
        public Plane lastCameraPlane {
            get { return _cameraPlane; }
        }
        public Vector3 lastCameraUp {
            get { return _cameraUp; }
        }
        #endregion

        #region Render methods
        public void BeginUsage (GameObject target, Mesh targetMesh) {
            _targetGameObject = target;
            _targetMesh = targetMesh;
            SetupCamera ();
        }
        public void BeginUsage (GameObject target) {
            _targetGameObject = target;
            _targetMesh = _targetGameObject.GetComponent<MeshFilter> ().sharedMesh;
            SetupCamera ();
        }
        public void EndUsage () {
            _targetMesh = null;
            if (!debugKeepCameraAfterUsage) {
                RemoveCamera ();
            }
        }
        public Texture2D GetTexture (Plane cameraPlane, Vector3 cameraUp, string texturePath = "") {
            return GetTexture (cameraPlane, cameraUp, _targetMesh.bounds, texturePath);
        }
        public Texture2D GetTexture (Plane cameraPlane, Vector3 cameraUp, Bounds bounds, string texturePath = "") {
            if (_targetGameObject == null) {
                Debug.LogWarning ("Target mesh not set on TextureBuilder.");
            } else {
                // Set params
                if (boundsMultiplier != Vector3.one) {
                    Vector3 minPoint = bounds.min;
                    Vector3 maxPoint = bounds.max;
                    minPoint.x *= boundsMultiplier.x;
                    minPoint.y *= boundsMultiplier.y;
                    minPoint.z *= boundsMultiplier.z;
                    maxPoint.x *= boundsMultiplier.x;
                    maxPoint.y *= boundsMultiplier.y;
                    maxPoint.z *= boundsMultiplier.z;
                    bounds.SetMinMax (minPoint, maxPoint);
                }
                _cameraPlane = new Plane (cameraPlane.normal, bounds.center + boundsOffset);
                _cameraUp = Vector3.ProjectOnPlane (cameraUp, _cameraPlane.normal).normalized;

                // Prepare target
                int originalLayer = _targetGameObject.layer;
			    SetLayerRecursively (_targetGameObject.transform);

                HideFlags _targetHideFlags = _targetGameObject.hideFlags;
                bool _targetIsActive = _targetGameObject.activeSelf;
                _targetGameObject.hideFlags = HideFlags.None;
                _targetGameObject.SetActive (true);
                
                // Prepare camera
                PositionCameraZY (bounds);

                // Render without SRP, save the render pipeline to a temp var, then assign it back after rendering
                _graphRP = GraphicsSettings.renderPipelineAsset;
                _qualityRP = QualitySettings.renderPipeline;
                GraphicsSettings.renderPipelineAsset = null;
                QualitySettings.renderPipeline = null;

                // Prepare textures
                CalculateTextureSize ();
                Texture2D targetTexture = new Texture2D ((int)finalTextureSize.x, (int)finalTextureSize.y, textureFormat, true);
                RenderTexture renderTexture = RenderTexture.GetTemporary ((int)finalTextureSize.x, (int)finalTextureSize.y, 16, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                camera.targetTexture = renderTexture;

                // Render
                if (shader != null) {
					camera.RenderWithShader (shader, "");
				} else {
					camera.Render ();
				}

                // Write to texture
                targetTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
				targetTexture.Apply();

                // Assign back the render pipeline.
                GraphicsSettings.renderPipelineAsset = _graphRP;
                QualitySettings.renderPipeline = _qualityRP;

                // Restore target to its original Layer
                SetLayerRecursively (_targetGameObject.transform, originalLayer);

                // Cleanup
                RenderTexture.ReleaseTemporary (renderTexture);
                RenderTexture.active = null;
                _targetGameObject.hideFlags = _targetHideFlags;
                _targetGameObject.SetActive (_targetIsActive);

                #if UNITY_EDITOR
                if (!string.IsNullOrEmpty (texturePath)) {
                    var bytes = targetTexture.EncodeToPNG();
                    File.WriteAllBytes (texturePath, bytes);
                    targetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath);
                    AssetDatabase.ImportAsset (texturePath);
                }
                #endif

                return targetTexture;
            }
            return null;
        }
        public Texture2D GetTexture (Vector3 cameraCenter, Vector3 cameraPlaneNormal, Vector3 cameraPlaneUp, Bounds cameraBounds, string texturePath = "") {
            if (_targetGameObject == null) {
                Debug.LogWarning ("Target mesh not set on TextureBuilder.");
            } else {
                // Prepare target
                int originalLayer = _targetGameObject.layer;
			    SetLayerRecursively (_targetGameObject.transform);

                HideFlags _targetHideFlags = _targetGameObject.hideFlags;
                bool _targetIsActive = _targetGameObject.activeSelf;
                _targetGameObject.hideFlags = HideFlags.None;
                _targetGameObject.SetActive (true);
                
                // Aspect and size of the camera.
                camera.aspect = cameraBounds.size.z / cameraBounds.size.y;
                camera.orthographicSize = cameraBounds.size.y / 2f;

                // Positioning the camera.
                float num = cameraBounds.extents.magnitude;
                camera.nearClipPlane = num * 0.1f;
                camera.farClipPlane = num * 2.2f;
                cameraGameObject.transform.position = cameraCenter + _targetGameObject.transform.position + cameraPlaneNormal * num;
                cameraGameObject.transform.LookAt (cameraCenter + _targetGameObject.transform.position, cameraPlaneUp);

                // Render without SRP, save the render pipeline to a temp var, then assign it back after rendering
                _graphRP = GraphicsSettings.renderPipelineAsset;
                _qualityRP = QualitySettings.renderPipeline;
                GraphicsSettings.renderPipelineAsset = null;
                QualitySettings.renderPipeline = null;

                // Prepare textures
                CalculateTextureSize ();
                Texture2D targetTexture = new Texture2D ((int)finalTextureSize.x, (int)finalTextureSize.y, textureFormat, true);
                RenderTexture renderTexture = RenderTexture.GetTemporary ((int)finalTextureSize.x, (int)finalTextureSize.y, 16, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                camera.targetTexture = renderTexture;

                // Render
                if (shader != null) {
					camera.RenderWithShader (shader, "");
				} else {
					camera.Render ();
				}

                // Write to texture
                targetTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
				targetTexture.Apply();

                // Assign back the render pipeline.
                GraphicsSettings.renderPipelineAsset = _graphRP;
                QualitySettings.renderPipeline = _qualityRP;

                // Restore target to its original Layer
                SetLayerRecursively (_targetGameObject.transform, originalLayer);

                // Cleanup
                RenderTexture.ReleaseTemporary (renderTexture);
                RenderTexture.active = null;
                _targetGameObject.hideFlags = _targetHideFlags;
                _targetGameObject.SetActive (_targetIsActive);

                #if UNITY_EDITOR
                if (!string.IsNullOrEmpty (texturePath)) {
                    var bytes = targetTexture.EncodeToPNG();
                    File.WriteAllBytes (texturePath, bytes);
                    targetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath);
                    AssetDatabase.ImportAsset (texturePath);
                }
                #endif

                return targetTexture;
            }
            return null;
        }
		Shader GetUnlitShader () {
			return Shader.Find ("Broccoli/Billboard Unlit");
		}
		Shader GetNormalShader () {
			return Shader.Find ("Broccoli/Billboard Normals");
		}
        /// <summary>
		/// Sets the layer of an object recursively.
		/// </summary>
		/// <param name="obj">Object.</param>
		private static void SetLayerRecursively (Transform obj, int layer = -1) {
			if (layer < 0)
				layer = PREVIEW_LAYER;
			obj.gameObject.layer = layer;
			for( int i = 0; i < obj.childCount; i++ )
				SetLayerRecursively( obj.GetChild( i ) );
		}
        /// <summary>
        /// Calculates the final texture size based on the aspect ratio if the flag is set to true.
        /// </summary>
        private void CalculateTextureSize () {
            finalTextureSize = textureSize;
            if (useTextureSizeToTargetRatio) {
                if (camera.aspect > 1) { // Wider than taller, modify height.
                    finalTextureSize.y /= camera.aspect;
                } else { // Taller than wider, modify width.
                    finalTextureSize.x *= camera.aspect;
                }
            }
        }
        #endregion

        #region Camera methods
        /// <summary>
		/// Setups the camera.
		/// </summary>
		/// <param name="target">Target.</param>
		private void SetupCamera () {
			if (camera != null) {
				Object.DestroyImmediate (camera);
			}
			if (cameraGameObject != null) {
				Object.DestroyImmediate (cameraGameObject);
				cameraGameObject = null;
			}

			cameraGameObject = new GameObject ("TextureBuilderCameraContainer");
			camera = cameraGameObject.AddComponent<Camera> ();

			// Set camera properties
			camera.cameraType = CameraType.Preview;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = backgroundColor;
			camera.cullingMask = 1 << PREVIEW_LAYER;
			camera.orthographic = true;
			camera.enabled = false;
        /// <summary>
		}
		/// Destroy the camera objects.
		/// </summary>
		public void RemoveCamera () {
			// Remove camera.
			if (camera != null)
				Object.DestroyImmediate (camera);
			if (cameraGameObject != null)
				Object.DestroyImmediate (cameraGameObject);
		}
        private void PositionCameraZY (Bounds projectBounds) {
			// Aspect and size of the camera.
			camera.aspect = projectBounds.size.z / projectBounds.size.y;
            camera.orthographicSize = projectBounds.size.y / 2f;
			// Positioning the camera.
			float num = projectBounds.extents.magnitude;
			camera.nearClipPlane = num * 0.1f;
			camera.farClipPlane = num * 2.2f;
			cameraGameObject.transform.position = projectBounds.center + boundsOffset + _targetGameObject.transform.position + _cameraPlane.normal * num;
			cameraGameObject.transform.LookAt (projectBounds.center + boundsOffset + _targetGameObject.transform.position, _cameraUp);
            //Debug.DrawLine (cameraGameObject.transform.position, projectBounds.center, Color.magenta, 15f);
        }
        #endregion
    }
}