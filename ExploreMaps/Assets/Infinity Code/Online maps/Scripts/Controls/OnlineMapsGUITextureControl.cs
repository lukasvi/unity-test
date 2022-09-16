/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using UnityEngine;

/// <summary>
/// Class control the map for the GUITexture.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Controls/GUITexture")]
#if !UNITY_2017_2_OR_NEWER || ONLINEMAPS_GUITEXTURE
[RequireComponent(typeof(GUITexture))]
#endif
public class OnlineMapsGUITextureControl : OnlineMapsControlBase2D
{
#if !UNITY_2017_2_OR_NEWER || ONLINEMAPS_GUITEXTURE
    private GUITexture _gTexture;

    /// <summary>
    /// Singleton instance of OnlineMapsGUITextureControl control.
    /// </summary>
    public new static OnlineMapsGUITextureControl instance
    {
        get { return OnlineMapsControlBase.instance as OnlineMapsGUITextureControl; }
    }

    /// <summary>
    /// Reference to GUITexture
    /// </summary>
    public GUITexture gTexture
    {
        get
        {
            if (_gTexture == null)
            {
#if UNITY_4_6 || UNITY_4_7
                _gTexture = guiTexture;
#else
                _gTexture = GetComponent<GUITexture>();
#endif
            }
            return _gTexture;
        }
    }

    public override bool GetCoords(out double lng, out double lat, Vector2 position)
    {
        Rect rect = screenRect;
        int countX = map.texture.width / OnlineMapsUtils.tileSize;
        int countY = map.texture.height / OnlineMapsUtils.tileSize;
        double px, py;
        map.GetTilePosition(out px, out py);
        double rx = (rect.center.x - position.x) / rect.width * 2;
        double ry = (rect.center.y - position.y) / rect.height * 2;
        px -= countX / 2f * rx;
        py += countY / 2f * ry;
        map.projection.TileToCoordinates(px, py, map.zoom, out lng, out lat);
        return true;
    }

    public override Rect GetRect()
    {
        return gTexture.GetScreenRect();
    }

    protected override bool HitTest(Vector2 position)
    {
        return gTexture.HitTest(position);
    }

    protected override void OnEnableLate()
    {
        if (gTexture == null)
        {
            Debug.LogError("Can not find GUITexture.");
            OnlineMapsUtils.DestroyImmediate(this);
        }
    }

    public override void SetTexture(Texture2D texture)
    {
        base.SetTexture(texture);
        gTexture.texture = texture;
    }
#else
    public override bool GetCoords(out double lng, out double lat, Vector2 position)
    {
        lng = lat = 0;
        return false;
    }
#endif
}