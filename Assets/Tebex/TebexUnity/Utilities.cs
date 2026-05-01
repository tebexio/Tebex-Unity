using System.Net.Http;
using Tebex.QR;
using UnityEngine;

namespace Tebex.TebexUnity
{
    public static class Utilities
    {
        public static Texture2D QrToTexture(QrCode qr, int scale = 8, int quietZone = 4)
        {
            bool[,] m = qr.GetModules();
            int size = m.GetLength(0);

            int outSize = (size + quietZone * 2) * scale;
            var tex = new Texture2D(outSize, outSize, TextureFormat.RGBA32, false);

            var pixels = new Color32[outSize * outSize];
            var white = new Color32(255, 255, 255, 255);
            var black = new Color32(0, 0, 0, 255);

            // Fill white
            for (int i = 0; i < pixels.Length; i++) pixels[i] = white;

            // Draw modules with quiet zone and scaling
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (!m[r, c]) continue;

                int rr0 = (r + quietZone) * scale;
                int cc0 = (c + quietZone) * scale;

                for (int y = 0; y < scale; y++)
                for (int x = 0; x < scale; x++)
                {
                    int px = cc0 + x;
                    int py = (outSize - 1) - (rr0 + y); // flip Y so it looks right in Unity
                    pixels[py * outSize + px] = black;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
        
        public static GameObject SpawnCenteredSprite(Texture2D tex, string name, float desiredWorldHeight, int sortingOrder)
        {
            // Create sprite
            // Pixels Per Unit: if we set this to tex.height, the sprite is 1 world unit tall by default.
            float ppu = tex.height;
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                ppu
            );

            // Create GameObject + SpriteRenderer
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            // Center in view (camera center)
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("No Camera.main found. Placing at (0,0,0).");
                go.transform.position = Vector3.zero;
            }
            else
            {
                // Put it in front of the camera so it is visible
                float z = 0f;
                if (cam.orthographic)
                {
                    // For ortho camera: place in the camera plane (commonly z=0)
                    z = 0f;
                }
                else
                {
                    // For perspective: put it some units in front of the camera
                    z = cam.nearClipPlane + 2f;
                }

                Vector3 centerViewport = new Vector3(0.5f, 0.5f, z);
                Vector3 centerWorld = cam.ViewportToWorldPoint(centerViewport);
                go.transform.position = centerWorld;
            }

            // Scale to desired size (world height)
            // Because we used ppu=tex.height, sprite height in world units is 1.0.
            // So set localScale uniformly to desiredWorldHeight.
            go.transform.localScale = Vector3.one * desiredWorldHeight;

            return go;
        }


    }
}