using System;

namespace Broccoli.Catalog {
    /// <summary>
    /// Attribute to apply to SproutLabEditor implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SproutCatalogImplAttribute : Attribute
    {
        public readonly int order;

        public SproutCatalogImplAttribute (int order) {
            this.order = order;
        }
    }
}