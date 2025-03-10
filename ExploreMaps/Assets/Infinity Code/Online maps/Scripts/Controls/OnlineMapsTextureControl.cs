﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using UnityEngine;

/// <summary>
/// Class control the map for the Texture.
/// </summary>
[System.Serializable]
[AddComponentMenu("Infinity Code/Online Maps/Controls/Texture")]
[RequireComponent(typeof(MeshRenderer))]
public class OnlineMapsTextureControl : OnlineMapsControlBase3D
{
    /// <summary>
    /// Singleton instance of OnlineMapsTextureControl control.
    /// </summary>
    public new static OnlineMapsTextureControl instance
    {
        get { return OnlineMapsControlBase.instance as OnlineMapsTextureControl; }
    }

    public override bool GetCoords(out double lng, out double lat, Vector2 position)
    {
        RaycastHit hit;

        lng = lat = 0;
        if (!cl.Raycast(activeCamera.ScreenPointToRay(position), out hit, OnlineMapsUtils.maxRaycastDistance)) return false;

        Renderer render = cl.GetComponent<Renderer>();
        if (render == null || render.sharedMaterial == null || render.sharedMaterial.mainTexture == null) return false;

        Vector2 r = hit.textureCoord;

        r.x = r.x - 0.5f;
        r.y = r.y - 0.5f;

        int countX = map.width / OnlineMapsUtils.tileSize;
        int countY = map.height / OnlineMapsUtils.tileSize;

        double px, py;
        map.GetTilePosition(out px, out py);

        px += countX * r.x;
        py -= countY * r.y;
        map.projection.TileToCoordinates(px, py, map.zoom, out lng, out lat);
        return true;
    }

    public override void SetTexture(Texture2D texture)
    {
        base.SetTexture(texture);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }
}