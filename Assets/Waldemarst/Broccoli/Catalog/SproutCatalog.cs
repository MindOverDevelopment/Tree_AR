using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Utils;
using Broccoli.Pipe;

namespace Broccoli.Catalog
{
	/// <summary>
	/// Sprout lab manager class.
	/// </summary>
	[SproutCatalogImpl(0)]
	public class SproutCatalog {
		#region CatalogItem class
		/// <summary>
		/// Catalog item.
		/// </summary>
		[System.Serializable]
		public class CatalogItem {
			/// <summary>
			/// Name of the item.
			/// </summary>
			public string name = "";
			/// <summary>
			/// The thumbnail texture.
			/// </summary>
			public Texture2D thumb = null;
			/// <summary>
			/// The path to the pipeline asset.
			/// </summary>
			public string path = "";
			/// <summary>
			/// The name of the category the item belongs to.
			/// </summary>
			public string category = "";
			/// <summary>
			/// The order within the category.
			/// </summary>
			public int order = 0;
			/// <summary>
			/// The catalog instance this item belongs to.
			/// </summary>
			[System.NonSerialized]
			public SproutCatalog parentCatalog = null;
		}
		#endregion

		#region Constants
        public const int IMPL_BRANCH = 0;
        #endregion

		#region Vars
		/// <summary>
		/// Name of the catalog.
		/// </summary>
		public string name = string.Empty;
		/// <summary>
		/// Description for this catalog.
		/// </summary>
		public string description = string.Empty;
		/// <summary>
		/// Implementation for the catalog.
		/// </summary>
		public int implId = -1;
		/// <summary>
		/// Order for this catalog.
		/// </summary>
		public int order = -1;
		/// <summary>
		/// Flag to check if the content for this catalog has been loaded.
		/// </summary>
		bool isPackLoaded = false;
		/// <summary>
		/// The total items.
		/// </summary>
		int _totalItems = 0;
		/// <summary>
		/// The total categories.
		/// </summary>
		int _totalCategories = 0;
		/// <summary>
		/// Gets the total items.
		/// </summary>
		/// <value>The total items.</value>
		public int totalItems {
			get { return _totalItems; }
		}
		/// <summary>
		/// Gets the total categories.
		/// </summary>
		/// <value>The total categories.</value>
		public int totalCategories {
			get { return _totalCategories; }
		}
		/// <summary>
		/// The GUI contents.
		/// </summary>
		[System.NonSerialized]
		public Dictionary<string, List<GUIContent>> contents = new Dictionary<string, List<GUIContent>> ();
		/// <summary>
		/// The catalog items.
		/// </summary>
		public Dictionary<string, List<CatalogItem>> items = new Dictionary<string, List<CatalogItem>>();
		/// <summary>
		/// The catalog asset relative path.
		/// </summary>
		protected string catalogAssetRelativePath = "Catalog";
		/// <summary>
		/// Gets the catalog asset path.
		/// </summary>
		/// <value>The catalog asset path.</value>
		protected virtual string catalogAssetPath { get { return ExtensionManager.extensionPath + catalogAssetRelativePath; } }
		/// <summary>
		/// Gets the full extension path to build the catalog item path to asset.
		/// </summary>
		/// <value>Path to the extension this catalog belongs to.</value>
		protected virtual string fullExtensionPath { get { return ExtensionManager.fullExtensionPath; } }
		#endregion

		#region Catalog Operations
		/// <summary>
		/// Gets the path relative to this project root to an asset of a catalog item.
		/// </summary>
		/// <param name="item">Item to get the asset path from.</param>
		/// <returns>Path to asset in the project.</returns>
		public virtual string GetPathToItemAsset (CatalogItem item) {
			if (item != null && !string.IsNullOrEmpty (item.path)) {
				return fullExtensionPath + item.path;
			}
			return string.Empty;
		}
		/// <summary>
		/// Loads the packages available for the catalog.
		/// </summary>
		/// <returns><c>True</c> if the packages get loaded.</returns>
		public bool LoadPackages () {
			#if UNITY_EDITOR
			if (!isPackLoaded) { 
				Clear ();
				string[] catalogPath = {catalogAssetPath};
				string[] packagesPath = UnityEditor.AssetDatabase.FindAssets ("t:sproutCatalogPackage", catalogPath);
				for (int i = 0; i < packagesPath.Length; i++) {
					SproutCatalogPackage package = UnityEditor.AssetDatabase.LoadAssetAtPath<SproutCatalogPackage> (
						UnityEditor.AssetDatabase.GUIDToAssetPath (packagesPath [i])); 
					if (package) {
						this.name = package.catalogName; 
						this.description = package.catalogDescription;
						this.implId = package.catalogImplId; 
						for (int j = 0; j < package.catalogItems.Count; j++) {
							AddCatalogItem (package.catalogItems[j]);
						}
					}
				}
				PrepareGUIContents ();
				isPackLoaded = true;
				return true;
			}
			#endif
			return false;
		}
		/// <summary>
		/// Reloads the packages available for the catalog.
		/// </summary>
		public void ReloadPackages () {
			isPackLoaded = false;
			LoadPackages ();
		}
		/// <summary>
		/// Adds a category to the catalog.
		/// </summary>
		/// <param name="name">Name of the category.</param>
		/// <param name="order">Order.</param>
		void AddCatalogCategory (string name, int order = 0) {
		}
		/// <summary>
		/// Adds an item to the catalog.
		/// </summary>
		/// <param name="catalogItem">Catalog item.</param>
		void AddCatalogItem (CatalogItem catalogItem) {
			//if (catalogItem != null && !string.IsNullOrEmpty (catalogItem.path)) {
            if (catalogItem != null) {
				if (catalogItem.category == null) {
					catalogItem.category = "";
				}
				AddCatalogCategory (catalogItem.category);
				if (!items.ContainsKey (catalogItem.category)) {
					items [catalogItem.category] = new List<CatalogItem> ();
				}
				items [catalogItem.category].Add (catalogItem);
			}
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			_totalCategories = 0;
			_totalItems = 0;
			var enumerator = items.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				enumerator.Current.Value.Clear ();
			}
			items.Clear ();
			ClearContent ();
		}
		/// <summary>
		/// Clears the GUI content.
		/// </summary>
		private void ClearContent () {
			var enumerator = contents.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				enumerator.Current.Value.Clear ();
			}
			contents.Clear ();
		}
		/// <summary>
		/// Prepares the GUI contents.
		/// </summary>
		public void PrepareGUIContents () {
			#if UNITY_EDITOR
			ClearContent ();
			_totalItems = 0;
			_totalCategories = 0;
			var enumerator = items.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				var itemsPair = enumerator.Current;
				if (!contents.ContainsKey (itemsPair.Key)) {
					contents [itemsPair.Key] = new List<GUIContent> ();
					_totalCategories++;
				}
				for (int i = 0; i < itemsPair.Value.Count; i++) {
					if (itemsPair.Value[i].thumb != null) {
						contents [itemsPair.Key].Add (new GUIContent (itemsPair.Value[i].name, itemsPair.Value[i].thumb)); 
					} else {
						contents [itemsPair.Key].Add (new GUIContent (itemsPair.Value[i].name, GUITextureManager.GetLogoBox ()));
					}
					_totalItems++;
				}
			}
			#endif
		}
		/// <summary>
		/// Gets the GUI contents.
		/// </summary>
		/// <returns>The GUI contents.</returns>
		public Dictionary<string, List<GUIContent>> GetGUIContents () {
			LoadPackages ();
			return contents;
		}
		/// <summary>
		/// Gets a catalog item at a given index.
		/// </summary>
		/// <returns>The item at index.</returns>
		/// <param name="categoryName">Category name.</param>
		/// <param name="index">Index.</param>
		public virtual CatalogItem GetItemAtIndex (string categoryName, int index) {
			CatalogItem item = null;
			if (items.ContainsKey(categoryName) && index >= 0 && index < items[categoryName].Count) {
				item = items[categoryName][index];
				item.parentCatalog = this;
			}
			return item;
		}
		/// <summary>
		/// Gets the first item at the catalog.
		/// </summary>
		/// <returns>The first catalog item in the catalog.</returns>
		public virtual CatalogItem GetFirstItem () {
			var catalogEnum = items.GetEnumerator ();
			if (catalogEnum.MoveNext ()) {
				if (catalogEnum.Current.Value.Count > 0) {
					catalogEnum.Current.Value [0].parentCatalog = this;
					return catalogEnum.Current.Value [0];
				}
			}
			return null;
		}
		#endregion
	}
}