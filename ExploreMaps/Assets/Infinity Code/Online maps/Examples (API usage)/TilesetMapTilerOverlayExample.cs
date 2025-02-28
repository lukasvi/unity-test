﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example of how to make the overlay from MapTiler tiles.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/TilesetMapTilerOverlayExample")]
    public class TilesetMapTilerOverlayExample : MonoBehaviour
    {
        // Overlay transparency
        [Range(0, 1)]
        public float alpha = 1;

        private float _alpha = 1;

        private static void LoadTileOverlay(OnlineMapsTile tile)
        {
            // Load overlay for tile from Resources.
            tile.overlayBackTexture = Resources.Load<Texture2D>(string.Format("OnlineMapsOverlay/{0}/{1}/{2}", tile.zoom, tile.x, tile.y));
        }

        private void OnStartDownloadTile(OnlineMapsTile tile)
        {
            // Load overlay for tile.
            LoadTileOverlay(tile);

            // Load the tile using a standard loader.
            OnlineMaps.instance.StartDownloadTile(tile);
        }

        private void Start()
        {
            if (OnlineMapsCache.instance != null)
            {
                // Subscribe to the cache events.
                OnlineMapsCache.instance.OnLoadedFromCache += LoadTileOverlay;
                OnlineMapsCache.instance.OnStartDownloadTile += OnStartDownloadTile;
            }
            else
            {
                // Subscribe to the tile download event.
                OnlineMaps.instance.OnStartDownloadTile += OnStartDownloadTile;
            }
        }

        private void Update()
        {
            // Update the transparency of overlay.
            if (Math.Abs(_alpha - alpha) > float.Epsilon)
            {
                _alpha = alpha;
                lock (OnlineMapsTile.lockTiles)
                {
                    foreach (OnlineMapsTile tile in OnlineMapsTile.tiles) tile.overlayBackAlpha = alpha;
                }
            }
        }
    }
}