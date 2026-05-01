using System.Collections.Generic;
using System.Threading.Tasks;
using Tebex.Common;
using Tebex.Headless;
using UnityEngine;

namespace Tebex.TebexUnity
{
    public static class Tebex
    {
        public static Webstore Webstore;
        public static List<Category> Categories = new();
        public static List<Package> Packages = new();
        public static Dictionary<int, Package> PackageLookup = new();
        public static Dictionary<int, Texture2D> ProductTextures = new();

        public static Texture2D GetTexture(Package package)
        {
            ProductTextures.TryGetValue(package.id, out var tex);
            return tex;
        }

        public static async Task PreloadTexturesAsync()
        {
            foreach (var package in Packages)
            {
                if (ProductTextures.ContainsKey(package.id)) continue;
                try
                {
                    ProductTextures[package.id] = await Http.LoadTexture(package.image);
                }
                catch
                {
                    ProductTextures[package.id] = null;
                }
            }
        }
    }
}
