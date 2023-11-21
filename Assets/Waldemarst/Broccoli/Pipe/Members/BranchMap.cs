using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Broccoli.Pipe {
	/// <summary>
	/// Branch map.
	/// Used to apply texture and materials to branches.
	/// </summary>
	[System.Serializable]
	public class BranchMap {
		#region Vars
        /// <summary>
		/// The group id this map apply to.
		/// </summary>
		public int groupId = 0;
        /// <summary>
        /// True if this map area is enabled.
        /// </summary>
        public bool enabled = true;
        /// <summary>
        /// The x offset (0 to 1).
        /// </summary>
        public float x = 0f;
        /// <summary>
        /// The y offset (0 to 1).
        /// </summary>
        public float y = 0f;
        /// <summary>
        /// The width of the area.
        /// </summary>
        public float width = 1f;
        /// <summary>
        /// The height of the area.
        /// </summary>
        public float height = 1f;
        /// <summary>
        /// Get the rect of the area.
        /// </summary>
        public Rect rect {
            get {
                return new Rect (x, y, width, height);
            }
        }
        /// <summary>
        /// Albedo texture.
        /// </summary>
        public Texture2D albedoTexture;
        /// <summary>
        /// Normal texture.
        /// </summary>
        public Texture2D normalTexture;
        /// <summary>
        /// Extras texture.
        /// </summary>
        public Texture2D extrasTexture;
        /// <summary>
        /// Subsurface texture.
        /// </summary>
        public Texture2D subsurfaceTexture;
		#endregion

		#region Ops
        /// <summary>
        /// Validate this map area.
        /// </summary>
        public void Validate () {
            if (x > 1) {
                x = 1;
            } else if (x < 0) {
                x = 0;
            }
            if (y > 1) {
                y = 1;
            } else if (y < 0) {
                y = 0;
            }
            if (x + width > 1) {
                width = 1f - x;
            } else if (x + width < 0) {
                x = 0;
                width = 1f;
            }
            if (y + height > 1) {
                height = 1f - y;
            } else if (y + height < 0) {
                y = 0;
                height = 1f;
            }
        }
		#endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public BranchMap Clone () {
			BranchMap clone = new BranchMap();
			clone.groupId = groupId;
            clone.enabled = enabled;
            clone.x = x;
            clone.y = y;
            clone.width = width;
            clone.height = height;
            clone.albedoTexture = albedoTexture;
            clone.normalTexture = normalTexture;
            clone.extrasTexture = extrasTexture;
            clone.subsurfaceTexture = subsurfaceTexture;
			return clone;
		}
		#endregion
	}
}