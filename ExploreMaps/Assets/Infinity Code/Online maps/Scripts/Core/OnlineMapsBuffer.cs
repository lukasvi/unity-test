﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if !UNITY_WEBGL
using System.Threading;
#endif

/// <summary>
/// This class is responsible for drawing the map.\n
/// <strong>Please do not use it if you do not know what you're doing.</strong>\n
/// Perform all operations with the map through other classes.
/// </summary>
public class OnlineMapsBuffer
{
    /// <summary>
    /// Allows you to manually control the sorting marker in a mode Drawing to Texture.
    /// </summary>
    public static Func<IEnumerable<OnlineMapsMarker>, IEnumerable<OnlineMapsMarker>> OnSortMarker;

    /// <summary>
    /// Can the buffer unload tiles?
    /// </summary>
    public bool allowUnloadTiles = true;

    /// <summary>
    /// Reference to OnlineMaps.
    /// </summary>
    public OnlineMaps api;

    /// <summary>
    /// Zoom for which the map displayed.
    /// </summary>
    public int apiZoom;

    /// <summary>
    /// Position the tile, which begins buffer.
    /// </summary>
    public OnlineMapsVector2i bufferPosition;

    public Color32[] frontBuffer;

    public bool generateSmartBuffer = false;

    /// <summary>
    /// Height of the map.
    /// </summary>
    public int height;

    /// <summary>
    /// Type redraw the map.
    /// </summary>
    public OnlineMapsRedrawType redrawType;

    /// <summary>
    /// The current status of the buffer.
    /// </summary>
    public OnlineMapsBufferStatus status = OnlineMapsBufferStatus.wait;
    public Color32[] smartBuffer;
    public bool updateBackBuffer;

    /// <summary>
    /// Width of the map.
    /// </summary>
    public int width;

    /// <summary>
    /// List of tiles that are already loaded, but not yet applied to the buffer.
    /// </summary>
    private List<OnlineMapsTile> newTiles;

    private Color32[] backBuffer;
    private int bufferZoom;
    private bool disposed;
    private OnlineMapsVector2i frontBufferPosition;
    private bool needUnloadTiles;
    private double apiLongitude;
    private double apiLatitude;

    /// <summary>
    /// Position for which the map displayed.
    /// </summary>
    public Vector2 apiPosition
    {
        get
        {
            return new Vector2((float)apiLongitude, (float)apiLatitude);
        }
    }

    /// <summary>
    /// The coordinates of the top-left the point of map that displays.
    /// </summary>
    public Vector2 topLeftPosition
    {
        get
        {
            int countX = api.width / OnlineMapsUtils.tileSize;
            int countY = api.height / OnlineMapsUtils.tileSize;

            double px, py;
            api.projection.CoordinatesToTile(apiLongitude, apiLatitude, apiZoom, out px, out py);

            px -= countX / 2f;
            py -= countY / 2f;

            api.projection.TileToCoordinates(px, py, apiZoom, out px, out py);
            return new Vector2((float)px, (float)py);
        }
    }

    public OnlineMapsBuffer(OnlineMaps api)
    {
        this.api = api;
        apiZoom = api.zoom;
        api.GetPosition(out apiLongitude, out apiLatitude);
        newTiles = new List<OnlineMapsTile>();
    }

    private void ApplyNewTiles()
    {
        if (newTiles == null || newTiles.Count == 0) return;

        lock (newTiles)
        {
            foreach (OnlineMapsTile tile in newTiles)
            {
                if (disposed) return;
                if (tile.status == OnlineMapsTileStatus.disposed) continue;

#if !UNITY_WEBGL
                int counter = 20;
                while (tile.colors.Length < OnlineMapsUtils.sqrTileSize && counter > 0)
                {
                    OnlineMapsUtils.ThreadSleep(1);
                    counter--;
                }
#endif
                    tile.ApplyColorsToChilds();
            }
            if (newTiles.Count > 0) newTiles.Clear();
        }
    }

    /// <summary>
    /// Adds a tile into the buffer.
    /// </summary>
    /// <param name="tile">Tile</param>
    public void ApplyTile(OnlineMapsTile tile)
    {
        if (newTiles == null) newTiles = new List<OnlineMapsTile>();
        lock (newTiles)
        {
            newTiles.Add(tile);
        }
    }

    private List<OnlineMapsTile> CreateParents(List<OnlineMapsTile> tiles, int zoom)
    {
        List<OnlineMapsTile> newParentTiles = new List<OnlineMapsTile>(tiles.Count);

        for (int i = 0; i < tiles.Count; i++)
        {
            OnlineMapsTile tile = tiles[i];
            if (tile.parent == null) CreateTileParent(zoom, tile, newParentTiles);
            else newParentTiles.Add(tile.parent);

            tile.used = true;
            tile.parent.used = true;
        }

        return newParentTiles;
    }

    private void CreateTileParent(int zoom, OnlineMapsTile tile, List<OnlineMapsTile> newParentTiles)
    {
        int px = tile.x / 2;
        int py = tile.y / 2;

        OnlineMapsTile parent;
        if (!OnlineMapsTile.GetTile(zoom, px, py, out parent))
        {
            parent = new OnlineMapsTile(px, py, zoom, api) {OnSetColor = OnTileSetColor};
        }

        newParentTiles.Add(parent);
        parent.used = true;
        tile.SetParent(parent);
    }

    /// <summary>
    /// Dispose of buffer.
    /// </summary>
    public void Dispose()
    {
        try
        {
            OnSortMarker = null;

            lock (OnlineMapsTile.lockTiles)
            {
                foreach (OnlineMapsTile tile in OnlineMapsTile.tiles) tile.Dispose();
                OnlineMapsTile.tiles = null;
            }

            frontBuffer = null;
            backBuffer = null;
            smartBuffer = null;

            status = OnlineMapsBufferStatus.disposed;
            newTiles = null;
            disposed = true;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }

    public void GenerateFrontBuffer()
    {
        try
        {
            api.GetPosition(out apiLongitude, out apiLatitude);
            apiZoom = api.zoom;

            while (!disposed)
            {
#if !UNITY_WEBGL
                while (status != OnlineMapsBufferStatus.start && api.renderInThread)
                {
                    if (disposed) return;
                    OnlineMapsUtils.ThreadSleep(1);
                }
#endif

                status = OnlineMapsBufferStatus.working;
                double px = 0, py = 0;

                try
                {
                    api.GetPosition(out px, out py);
                    int zoom = api.zoom;

                    bool fullRedraw = redrawType == OnlineMapsRedrawType.full;
                    if (newTiles != null && api.target == OnlineMapsTarget.texture) ApplyNewTiles();

                    if (disposed) return;

                    bool backBufferUpdated = UpdateBackBuffer(px, py, zoom, fullRedraw);
                    if (disposed) return;

                    if (api.target == OnlineMapsTarget.texture)
                    {
                        GetFrontBufferPosition(px, py, bufferPosition, zoom, api.width, api.height);

                        if (backBufferUpdated)
                        {
                            for (int i = 0; i < api.drawingElements.Count; i++)
                            {
                                OnlineMapsDrawingElement element = api.drawingElements[i];
                                if (disposed) return;
                                element.Draw(backBuffer, bufferPosition, width, height, zoom);
                            }
                            SetMarkersToBuffer(api.markers);
                        }

                        if (disposed) return;
                        if (generateSmartBuffer && api.useSmartTexture) UpdateSmartBuffer(api.width, api.height);
                        else UpdateFrontBuffer(api.width, api.height);
                    }
                }
                catch (Exception exception)
                {
                    if (disposed) return;
                    Debug.Log(exception.Message + "\n" + exception.StackTrace);
                }

                status = OnlineMapsBufferStatus.complete;
                apiLongitude = px;
                apiLatitude = py;
                apiZoom = api.zoom;

                if (needUnloadTiles) UnloadOldTiles();

#if !UNITY_WEBGL
                if (!api.renderInThread) break;
#else
                break;
#endif
            }
        }
        catch
        {
        }
    }

    private OnlineMapsVector2i GetBackBufferPosition(double px, double py, OnlineMapsVector2i _bufferPosition, int zoom, int apiWidth, int apiHeight)
    {
        api.projection.CoordinatesToTile(px, py, zoom, out px, out py);

        int countX = apiWidth / OnlineMapsUtils.tileSize + 2;
        int countY = apiHeight / OnlineMapsUtils.tileSize + 2;

        px -= countX / 2f + _bufferPosition.x - 1;
        py -= countY / 2f + _bufferPosition.y - 1;

        int ix = (int) (px / countX * width);
        int iy = (int) (py / countY * height);

        return new OnlineMapsVector2i(ix, iy);
    }

    public void GetCorners(out double tlx, out double tly, out double brx, out double bry)
    {
        int countX = api.width / OnlineMapsUtils.tileSize;
        int countY = api.height / OnlineMapsUtils.tileSize;

        api.projection.CoordinatesToTile(apiLongitude, apiLatitude, apiZoom, out tlx, out tly);

        brx = tlx + countX / 2f;
        bry = tly + countY / 2f;
        tlx -= countX / 2f;
        tly -= countY / 2f;

        api.projection.TileToCoordinates(tlx, tly, apiZoom, out tlx, out tly);
        api.projection.TileToCoordinates(brx, bry, apiZoom, out brx, out bry);

        int max = (1 << apiZoom) * OnlineMapsUtils.tileSize;
        if (max == api.width)
        {
            double lng = apiLongitude + 180;
            tlx = lng + 0.001;
            if (tlx > 180) tlx -= 360;

            brx = lng - 0.001;
            if (brx > 180) brx -= 360;
        }
    }

    private void GetFrontBufferPosition(double px, double py, OnlineMapsVector2i _bufferPosition, int zoom, int apiWidth, int apiHeight)
    {
        OnlineMapsVector2i pos = GetBackBufferPosition(px, py, _bufferPosition, zoom, apiWidth, apiHeight);
        int ix = pos.x;
        int iy = pos.y;

        if (iy < 0) iy = 0;
        else if (iy >= height - apiHeight) iy = height - apiHeight;

        frontBufferPosition = new OnlineMapsVector2i(ix, iy);
    }

    private Rect GetMarkerRect(OnlineMapsMarker marker)
    {
        const int s = OnlineMapsUtils.tileSize;

        double tx, ty;
        marker.GetTilePosition(out tx, out ty);

        tx -= bufferPosition.x;
        ty -= bufferPosition.y;
        OnlineMapsVector2i ip = marker.GetAlignedPosition((int)(tx * s), (int)(ty * s));
        return new Rect(ip.x, ip.y, marker.width, marker.height);
    }

    private void InitTile(int zoom, OnlineMapsVector2i pos, int maxY, List<OnlineMapsTile> newBaseTiles, int y, int px)
    {
        int py = y + pos.y;
        if (py < 0 || py >= maxY) return;

        OnlineMapsTile tile;

        if (!OnlineMapsTile.GetTile(zoom, px, py, out tile))
        {
            OnlineMapsTile parent = null;

            if (!api.useCurrentZoomTiles)
            {
                int ptx = px / 2;
                int pty = py / 2;
                if (OnlineMapsTile.GetTile(zoom - 1, ptx, pty, out parent)) parent.used = true;
            }

            tile = new OnlineMapsTile(px, py, zoom, api, parent) { OnSetColor = OnTileSetColor };
        }

        newBaseTiles.Add(tile);
        tile.used = true;
    }

    private void InitTiles(int zoom, int countX, OnlineMapsVector2i pos, int countY, int maxY, List<OnlineMapsTile> newBaseTiles)
    {
        int maxX = 1 << bufferZoom;
        for (int x = 0; x < countX; x++)
        {
            int px = x + pos.x;
            if (px < 0) px += maxX;
            else if (px >= maxX) px -= maxX;

            for (int y = 0; y < countY; y++) InitTile(zoom, pos, maxY, newBaseTiles, y, px);
        }
    }

    private void OnTileSetColor(OnlineMapsTile tile)
    {
        if (tile.zoom == bufferZoom) SetBufferTile(tile);
    }

    private Rect SetBufferTile(OnlineMapsTile tile, int? offsetX = null)
    {
        if (api.target == OnlineMapsTarget.tileset) return default(Rect);

        const int s = OnlineMapsUtils.tileSize;
        int i = 0;
        int px = tile.x - bufferPosition.x;
        int py = tile.y - bufferPosition.y;

        int maxX = 1 << tile.zoom;

        if (px < 0) px += maxX;
        else if (px >= maxX) px -= maxX;

        if (api.width == maxX * s && px < 2 && !offsetX.HasValue) SetBufferTile(tile, maxX);

        if (offsetX.HasValue) px += offsetX.Value;

        px *= s;
        py *= s;

        if (px + s < 0 || py + s < 0 || px > width || py > height) return new Rect(0, 0, 0, 0);

        if (!tile.hasColors || tile.status != OnlineMapsTileStatus.loaded)
        {
            const int hs = s / 2;
            int sx = tile.x % 2 * hs;
            int sy = tile.y % 2 * hs;
            if (SetBufferTileFromParent(tile, px, py, s / 2, sx, sy)) return new Rect(px, py, OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize);
        }

        Color32[] colors = tile.colors;

        lock (colors)
        {
            int maxSize = width * height;

            for (int y = py + s - 1; y >= py; y--)
            {
                int bp = y * width + px;
                if (bp + s < 0 || bp >= maxSize) continue;
                int l = s;
                if (bp < 0)
                {
                    l -= bp;
                    bp = 0;
                }
                else if (bp + s > maxSize)
                {
                    l -= maxSize - (bp + s);
                    bp = maxSize - s - 1;
                }

                try
                {
                    Array.Copy(colors, i, backBuffer, bp, l);
                }
                catch
                {
                }

                i += s;
            }

            return new Rect(px, py, OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize);
        }
    }

    private bool SetBufferTileFromParent(OnlineMapsTile tile, int px, int py, int size, int sx, int sy)
    {
        OnlineMapsTile parent = tile.parent;
        if (parent == null) return false;

        const int s = OnlineMapsUtils.tileSize;
        const int hs = s / 2;

        if (parent.status != OnlineMapsTileStatus.loaded || !parent.hasColors)
        {
            sx = sx / 2 + parent.x % 2 * hs;
            sy = sy / 2 + parent.y % 2 * hs;
            return SetBufferTileFromParent(parent, px, py, size / 2, sx, sy);
        }

        Color32[] colors = parent.colors;
        int scale = s / size;

        if (colors.Length != OnlineMapsUtils.sqrTileSize) return false;

        int ry = s - sy - 1;

        lock (colors)
        {
            for (int y = 0; y < size; y++)
            {
                int oys = (ry - y) * s + sx;
                int scaledY = y * scale + py;
                for (int x = 0; x < size; x++)
                {
                    Color32 clr = colors[oys + x];
                    int scaledX = x * scale + px;

                    for (int by = scaledY; by < scaledY + scale; by++)
                    {
                        int bpy = by * width + scaledX;
                        for (int bx = bpy; bx < bpy + scale; bx++) backBuffer[bx] = clr;
                    }
                }
            }
        }

        return true;
    }

    private void SetColorToBuffer(Color clr, OnlineMapsVector2i ip, int y, int x)
    {
        if (Math.Abs(clr.a) < float.Epsilon) return;
        int bufferIndex = (ip.y + y) * width + ip.x + x;
        if (clr.a < 1)
        {
            float alpha = clr.a;
            Color bufferColor = backBuffer[bufferIndex];
            clr.a = 1;
            clr.r = Mathf.Lerp(bufferColor.r, clr.r, alpha);
            clr.g = Mathf.Lerp(bufferColor.g, clr.g, alpha);
            clr.b = Mathf.Lerp(bufferColor.b, clr.b, alpha);
        }
        backBuffer[bufferIndex] = clr;
    }

    private void SetMarkerToBuffer(OnlineMapsMarker marker, double sx, double sy, double ex, double ey)
    {
        const int s = OnlineMapsUtils.tileSize;

        double mx, my;
        marker.GetPosition(out mx, out my);
        double px, py;
        api.projection.CoordinatesToTile(mx, my, bufferZoom, out px, out py);

        int maxX = 1 << bufferZoom;

        bool isEntireWorld = api.width == maxX * s;
        if (isEntireWorld)
        {
            
        }
        else if (!(((mx > sx && mx < ex) || (mx + 360 > sx && mx + 360 < ex) ||
             (mx - 360 > sx && mx - 360 < ex)) &&
            my < sy && my > ey)) return;

#if !UNITY_WEBGL
        int maxCount = 20;
        while (marker.locked && maxCount > 0)
        {
            OnlineMapsUtils.ThreadSleep(1);
            maxCount--;
        }
#endif

        marker.locked = true;

        px -= bufferPosition.x;
        py -= bufferPosition.y;

        if (isEntireWorld)
        {
            double tx, ty;
            api.projection.CoordinatesToTile(apiLongitude, apiLatitude, bufferZoom, out tx, out ty);
            tx -= api.width / s / 2;

            if (px < tx) px += maxX;
        }
        else
        {
            if (px < 0) px += maxX;
            else if (px > maxX) px -= maxX;
        }

        OnlineMapsVector2i ip = marker.GetAlignedPosition((int) (px * s), (int) (py * s));

        Color32[] markerColors = marker.colors;
        if (markerColors == null || markerColors.Length == 0) return;

        int markerWidth = marker.width;
        int markerHeight = marker.height;

        for (int y = 0; y < marker.height; y++)
        {
            if (disposed) return;
            if (ip.y + y < 0 || ip.y + y >= height) continue;

            int cy = (markerHeight - y - 1) * markerWidth;
            
            for (int x = 0; x < marker.width; x++)
            {
                if (ip.x + x < 0 || ip.x + x >= width) continue;
            
                try
                {
                    SetColorToBuffer(markerColors[cy + x], ip, y, x);
                }
                catch
                {
                }
            }
        }

        marker.locked = false;
    }

    public void SetMarkersToBuffer(IEnumerable<OnlineMapsMarker> markers)
    {
        if (OnlineMapsControlBase.instance is OnlineMapsControlBase3D)
        {
            if (((OnlineMapsControlBase3D)OnlineMapsControlBase.instance).marker2DMode == OnlineMapsMarker2DMode.billboard)
            {
                return;
            }
        }

        const int s = OnlineMapsUtils.tileSize;
        int countX = api.width / s + 2;
        int countY = api.height / s + 2;

        double sx, sy, ex, ey;
        api.projection.TileToCoordinates(bufferPosition.x, bufferPosition.y, bufferZoom, out sx, out sy);
        api.projection.TileToCoordinates(bufferPosition.x + countX, bufferPosition.y + countY + 1, bufferZoom, out ex, out ey);

        if (ex < sx) ex += 360;

        IEnumerable<OnlineMapsMarker> usedMarkers = markers.Where(m => m.enabled && m.range.InRange(bufferZoom));
        usedMarkers = OnSortMarker != null ? OnSortMarker(usedMarkers) : usedMarkers.OrderByDescending(m => m, new MarkerComparer());

        foreach (OnlineMapsMarker marker in usedMarkers)
        {
            if (disposed) return;
            SetMarkerToBuffer(marker, sx, sy, ex, ey);
        }
    }

    private void UnloadOldTiles()
    {
        needUnloadTiles = false;

#if !UNITY_WEBGL
        int count = 100;

        while (api.renderInThread && !allowUnloadTiles && count > 0)
        {
            OnlineMapsUtils.ThreadSleep(1);
            count--;
        }

        if (count == 0) return;
#endif
        lock (OnlineMapsTile.lockTiles)
        {
            foreach (OnlineMapsTile tile in OnlineMapsTile.tiles)
            {
                if (!tile.used && !tile.isBlocked) tile.Dispose();
            }
        }
    }

    private bool UpdateBackBuffer(double px, double py, int zoom, bool fullRedraw)
    {
        const int s = OnlineMapsUtils.tileSize;
        int countX = api.width / s + 2;
        int countY = api.height / s + 2;

        double cx, cy;
        api.projection.CoordinatesToTile(px, py, zoom, out cx, out cy);
        OnlineMapsVector2i pos = new OnlineMapsVector2i((int)cx - countX / 2, (int)cy - countY / 2);

        int maxY = 1 << zoom;

        if (pos.y < 0) pos.y = 0;
        if (pos.y >= maxY - countY) pos.y = maxY - countY;

        if (api.target == OnlineMapsTarget.texture)
        {
            if (frontBuffer == null || frontBuffer.Length != api.width * api.height)
            {
                frontBuffer = new Color32[api.width * api.height];
                fullRedraw = true;
            }

            if (backBuffer == null || width != countX * s || height != countY * s)
            {
                width = countX * s;
                height = countY * s;
                backBuffer = new Color32[height * width];

                fullRedraw = true;
            }
        }

        if (!updateBackBuffer && !fullRedraw && bufferZoom == zoom && bufferPosition != null && bufferPosition == pos) return false;

        updateBackBuffer = false;

        bufferPosition = pos;
        bufferZoom = zoom;

        List<OnlineMapsTile> newBaseTiles = new List<OnlineMapsTile>();

        lock (OnlineMapsTile.lockTiles)
        {
            for (int i = 0; i < OnlineMapsTile.tiles.Count; i++) OnlineMapsTile.tiles[i].used = false;

            InitTiles(zoom, countX, pos, countY, maxY, newBaseTiles);

            if (!api.useCurrentZoomTiles)
            {
                List<OnlineMapsTile> newParentTiles = newBaseTiles;
                for (int z = zoom - 1; z > Mathf.Max(zoom - 5, 2); z--) newParentTiles = CreateParents(newParentTiles, z);
            }

            if (api.target != OnlineMapsTarget.tileset) for (int i = 0; i < newBaseTiles.Count; i++) SetBufferTile(newBaseTiles[i]);
        }

        needUnloadTiles = true;

        return true;
    }

    private void UpdateFrontBuffer(int apiWidth, int apiHeight)
    {
        int i = 0;

        for (int y = frontBufferPosition.y + apiHeight - 1; y >= frontBufferPosition.y; y--)
        {
            if (disposed) return;
            Array.Copy(backBuffer, frontBufferPosition.x + y * width, frontBuffer, i, apiWidth);
            i += apiWidth;
        }
    }

    private void UpdateSmartBuffer(int apiWidth, int apiHeight)
    {
        int w = apiWidth;
        int hw = w / 2;
        int hh = apiHeight / 2;

        if (smartBuffer == null || smartBuffer.Length != hw * hh) smartBuffer = new Color32[hw * hh];

        for (int y = 0; y < hh; y++)
        {
            if (disposed) return;

            int sy = (hh - y - 1) * hw;
            int fy = (y * 2 + frontBufferPosition.y) * width + frontBufferPosition.x;
            int fny = (y * 2 + frontBufferPosition.y + 1) * width + frontBufferPosition.x + 1;
            for (int x = 0, x2 = 0; x < hw; x++, x2 += 2)
            {
                Color32 clr1 = backBuffer[fy + x2];
                Color32 clr2 = backBuffer[fny + x2];

                clr1.r = (byte)((clr1.r + clr2.r) / 2);
                clr1.g = (byte)((clr1.g + clr2.g) / 2);
                clr1.b = (byte)((clr1.b + clr2.b) / 2);

                smartBuffer[sy + x] = clr1;
            }
        }
    }

    internal class MarkerComparer : IComparer<OnlineMapsMarkerBase>
    {
        public int Compare(OnlineMapsMarkerBase m1, OnlineMapsMarkerBase m2)
        {
            double m1x, m1y, m2x, m2y;
            m1.GetPosition(out m1x, out m1y);
            m2.GetPosition(out m2x, out m2y);

            if (m1y > m2y) return 1;
            if (Math.Abs(m1y - m2y) < double.Epsilon)
            {
                if (m1x < m2x) return 1;
                return 0;
            }
            return -1;
        }
    }
}