﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

#if !UNITY_WEBPLAYER && (!UNITY_WP_8_1 || UNITY_EDITOR)

using System.IO;
using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example how to make a runtime caching tiles.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/CacheTilesExample")]
    public class CacheTilesExample : MonoBehaviour
    {
        /// <summary>
        /// Gets the local path for tile.
        /// </summary>
        /// <param name="tile">Reference to tile</param>
        /// <returns>Local path for tile</returns>
        private static string GetTilePath(OnlineMapsTile tile)
        {
            string[] parts =
            {
                Application.persistentDataPath,
                "OnlineMapsTileCache",
                tile.mapType.provider.id,
                tile.mapType.id,
                tile.zoom.ToString(),
                tile.x.ToString(),
                tile.y + ".png"
            };
            return string.Join("/", parts);
        }

        /// <summary>
        /// This method is called when loading the tile.
        /// </summary>
        /// <param name="tile">Reference to tile</param>
        private void OnStartDownloadTile(OnlineMapsTile tile)
        {
            // Get local path.
            string path = GetTilePath(tile);

            // If the tile is cached.
            if (File.Exists(path))
            {
                // Load tile texture from cache.
                Texture2D tileTexture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                tileTexture.LoadImage(File.ReadAllBytes(path));
                tileTexture.wrapMode = TextureWrapMode.Clamp;

                // Send texture to map.
                if (OnlineMaps.instance.target == OnlineMapsTarget.texture)
                {
                    tile.ApplyTexture(tileTexture);
                    OnlineMaps.instance.buffer.ApplyTile(tile);
                    OnlineMapsUtils.DestroyImmediate(tileTexture);
                }
                else
                {
                    tile.texture = tileTexture;
                    tile.status = OnlineMapsTileStatus.loaded;
                }

                // Redraw map.
                OnlineMaps.instance.Redraw();
            }
            else
            {
                // If the tile is not cached, download tile with a standard loader.
                OnlineMaps.instance.StartDownloadTile(tile);
            }
        }

        /// <summary>
        /// This method is called when tile is success downloaded.
        /// </summary>
        /// <param name="tile">Reference to tile.</param>
        private void OnTileDownloaded(OnlineMapsTile tile)
        {
            // Get local path.
            string path = GetTilePath(tile);

            // Cache tile.
            FileInfo fileInfo = new FileInfo(path);
            DirectoryInfo directory = fileInfo.Directory;
            if (!directory.Exists) directory.Create();

            File.WriteAllBytes(path, tile.www.bytes);
        }

        private void Start()
        {
            // Subscribe to the event of success download tile.
            OnlineMapsTile.OnTileDownloaded += OnTileDownloaded;

            // Intercepts requests to the download of the tile.
            if (OnlineMapsCache.instance != null) OnlineMapsCache.instance.OnStartDownloadTile += OnStartDownloadTile;
            else OnlineMaps.instance.OnStartDownloadTile += OnStartDownloadTile;
        }
    }
}

#endif