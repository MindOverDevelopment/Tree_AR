using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Utils;

namespace Broccoli.Catalog
{
	/// <summary>
	/// Catalog package.
	/// </summary>
	#if BROCCOLI_DEVEL
	[CreateAssetMenu(fileName = "SproutCatalogPackage", menuName = "Broccoli Devel/Sprout Catalog Package", order = 1)]
	#endif
	[System.Serializable]
	public class SproutCatalogPackage : ScriptableObject {
		/// <summary>
		/// Name for the catalog.
		/// </summary>
		public string catalogName = string.Empty;
		/// <summary>
		/// Description for the content of the catalog.
		/// </summary>
		public string catalogDescription = string.Empty;
		/// <summary>
		/// Implementation identifier of the catalog.
		/// </summary>
		public int catalogImplId = -1;
		/// <summary>
		/// The catalog items. 
		/// </summary>
		public List<SproutCatalog.CatalogItem> catalogItems = new List<SproutCatalog.CatalogItem> ();
	}
}