/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class implements the basic functionality control of the map.
/// </summary>
[Serializable]
[AddComponentMenu("")]
public abstract class OnlineMapsControlBase : MonoBehaviour
{
    /// <summary>
    /// Delay before invoking event OnMapLongPress.
    /// </summary>
    public static float longPressDelay = 1;

    /// <summary>
    /// Distance (pixels) after which will start drag the map.
    /// </summary>
    public static float startDragDistance = 4;

    /// <summary>
    /// Singleton of control
    /// </summary>
    protected static OnlineMapsControlBase _instance;

    /// <summary>
    /// Event intercepts getting current cursor position.
    /// </summary>
    public Func<Vector2> OnGetInputPosition;

    /// <summary>
    /// Event intercepts getting number of touches.
    /// </summary>
    public Func<int> OnGetTouchCount;

    /// <summary>
    /// Event that occurs when you click on the map.
    /// </summary>
    public Action OnMapClick;

    /// <summary>
    /// Event that occurs when you double-click on the map.
    /// </summary>
    public Action OnMapDoubleClick;

    /// <summary>
    /// Event that occurs when you drag the map.
    /// </summary>
    public Action OnMapDrag;

    /// <summary>
    /// Event that occurs when you long press the map.
    /// </summary>
    public Action OnMapLongPress;

    /// <summary>
    /// Event that occurs when you press on the map.
    /// </summary>
    public Action OnMapPress;

    /// <summary>
    /// Event that occurs when you release the map.
    /// </summary>
    public Action OnMapRelease;

    /// <summary>
    /// Event that occurs when you zoom the map.
    /// </summary>
    public Action OnMapZoom;

    /// <summary>
    /// Event that occurs at end Update.
    /// </summary>
    public Action OnUpdateAfter;

    /// <summary>
    /// Event that occurs at start Update.
    /// </summary>
    public Action OnUpdateBefore;

    /// <summary>
    /// Event validating that on current GameObject allowed zoom on wheel.\n
    /// True - zoom is allowed, false - forbidden.
    /// </summary>
    public Predicate<GameObject> OnValidateMouseWheel;

    /// <summary>
    /// Texture, which will draw the map. \n
    /// To change the texture use OnlineMapsControlBase.SetTexture.
    /// </summary>
    [HideInInspector]
    public Texture2D activeTexture;

    /// <summary>
    /// Specifies whether to create a marker by pressing M under the cursor.
    /// </summary>
    public bool allowAddMarkerByM = true;

    /// <summary>
    /// Specifies whether the user can change zoom of the map.
    /// </summary>
    public bool allowZoom = true;

    /// <summary>
    /// Specifies whether the user can manipulate the map.
    /// </summary>
    public bool allowUserControl = true;

    /// <summary>
    /// Specifies whether to move map.
    /// </summary>
    [HideInInspector]
    public bool isMapDrag;

    /// <summary>
    /// Inverts the map touch zoom for mobile devices.
    /// </summary>
    public bool invertTouchZoom = false;

    /// <summary>
    /// Allows you to zoom the map when double-clicked.
    /// </summary>
    public bool zoomInOnDoubleClick = true;

    /// <summary>
    /// Mode of zoom.
    /// </summary>
    public OnlineMapsZoomMode zoomMode = OnlineMapsZoomMode.target;

    protected Rect _screenRect;

    /// <summary>
    /// Reference to map instance.
    /// </summary>
    protected OnlineMaps map;
    
    protected float lastGestureDistance;
    protected Vector2 lastGestureCenter = Vector2.zero;
    protected Vector2 lastInputPosition;
    protected int lastTouchCount;
    protected bool lockClick;
    protected bool waitZeroTouches = false;

    private IOnlineMapsInteractiveElement activeElement;
    private OnlineMapsMarkerBase _dragMarker;
    private long[] lastClickTimes = {0, 0};
    private Vector2 pressPoint;
    private IEnumerator longPressEnumenator;
    private double lastPositionLng;
    private double lastPositionLat;
    private bool isMapPress;
    private bool mapDragStarted;

    /// <summary>
    /// Singleton instance of map control.
    /// </summary>
    public static OnlineMapsControlBase instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Indicates whether it is possible to get the screen coordinates store. True - for 2D map, false - for the 3D map.
    /// </summary>
    public virtual bool allowMarkerScreenRect
    {
        get { return false; }
    }

    protected virtual bool allowTouchZoom
    {
        get { return true; }
    }

    /// <summary>
    /// Marker that draged at the moment.
    /// </summary>
    public OnlineMapsMarkerBase dragMarker
    {
        get { return _dragMarker; }
        set
        {
            _dragMarker = value;
            if (_dragMarker != null) UpdateLastPosition();
        }
    }

    /// <summary>
    /// Mipmap for tiles.
    /// </summary>
    public virtual bool mipmapForTiles
    {
        get { return false; }
        set { throw new Exception("This control does not support mipmap for tiles.");}
    }

    /// <summary>
    /// Screen area occupied by the map.
    /// </summary>
    public virtual Rect screenRect
    {
        get { return _screenRect; }
    }

    /// <summary>
    /// UV rectangle used by the texture of the map.
    /// NGUI: uiTexture.uvRect.
    /// Other: new Rect(0, 0, 1, 1);
    /// </summary>
    public virtual Rect uvRect
    {
        get { return new Rect(0, 0, 1, 1); }
    }

    /// <summary>
    /// Function, which is executed after map updating.
    /// </summary>
    protected virtual void AfterUpdate()
    {
        
    }

    /// <summary>
    /// Function, which is executed before map updating.
    /// </summary>
    protected virtual void BeforeUpdate()
    {
        
    }

    /// <summary>
    /// Creates a marker at the location of the cursor.
    /// </summary>
    protected void CreateMarker()
    {
        map.AddMarker(GetCoords());
    }

    /// <summary>
    /// Moves the marker to the location of the cursor.
    /// </summary>
    protected void DragMarker()
    {
        double lat, lng;
        bool hit = GetCoords(out lng, out lat);

        if (hit)
        {
            double offsetX = lng - lastPositionLng;
            double offsetY = lat - lastPositionLat;

            if (Math.Abs(offsetX) > double.Epsilon || Math.Abs(offsetY) > double.Epsilon)
            {
                double px, py;
                dragMarker.GetPosition(out px, out py);
                
                px = px + offsetX;
                py = py + offsetY;

                dragMarker.SetPosition(px, py);
                if (dragMarker.OnDrag != null) dragMarker.OnDrag(dragMarker);
                if (dragMarker is OnlineMapsMarker) map.Redraw();
            }
            lastPositionLng = lng;
            lastPositionLat = lat;
        }
    }

    /// <summary>
    /// Returns the geographical coordinates of the location where the cursor is.
    /// </summary>
    /// <returns>Geographical coordinates</returns>
    public Vector2 GetCoords()
    {
        return GetCoords(GetInputPosition());
    }

    /// <summary>
    /// Returns the geographical coordinates at the specified coordinates of the screen.
    /// </summary>
    /// <param name="position">Screen coordinates</param>
    /// <returns>Geographical coordinates</returns>
    public Vector2 GetCoords(Vector2 position)
    {
        double lng, lat;
        GetCoords(out lng, out lat, position);
        return new Vector2((float)lng, (float)lat);
    }

    /// <summary>
    /// Returns the geographical coordinates of the location where the cursor is.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns>True - success, False - otherwise.</returns>
    public bool GetCoords(out double lng, out double lat)
    {
        return GetCoords(out lng, out lat, GetInputPosition());
    }

    /// <summary>
    /// Returns the geographical coordinates at the specified coordinates of the screen.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="position">Screen coordinates</param>
    /// <returns>True - success, False - otherwise.</returns>
    public abstract bool GetCoords(out double lng, out double lat, Vector2 position);

    /// <summary>
    /// Returns the current cursor position.
    /// </summary>
    /// <returns>Current cursor position</returns>
    public Vector2 GetInputPosition()
    {
        if (OnGetInputPosition != null) return OnGetInputPosition();

        return Input.mousePosition;
    }

    /// <summary>
    /// Gets interactive element by screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position</param>
    /// <returns>Interactive element</returns>
    public virtual IOnlineMapsInteractiveElement GetInteractiveElement(Vector2 screenPosition)
    {
        if (IsCursorOnUIElement(screenPosition)) return null;

        OnlineMapsMarker marker = map.GetMarkerFromScreen(screenPosition);
        if (marker != null) return marker;

        OnlineMapsDrawingElement drawingElement = map.GetDrawingElement(screenPosition);
        return drawingElement;
    }

    /// <summary>
    /// Converts geographical coordinate to position in the scene relative to the top-left corner of the map in map space.
    /// </summary>
    /// <param name="coords">Geographical coordinate (X - Longitude, Y - Latitude)</param>
    /// <returns>Scene position (in map space)</returns>
    public virtual Vector2 GetPosition(Vector2 coords)
    {
        double px, py, tlx, tly;
        map.projection.CoordinatesToTile(coords.x, coords.y, map.zoom, out px, out py);
        map.GetTopLeftPosition(out tlx, out tly);
        map.projection.CoordinatesToTile(tlx, tly, map.zoom, out tlx, out tly);
        px -= tlx;
        py -= tly;

        int maxX = 1 << (map.zoom - 1);
        if (px < -maxX) px += maxX << 1;
        return new Vector2((float)(px * OnlineMapsUtils.tileSize), (float)(py * OnlineMapsUtils.tileSize));
    }

    /// <summary>
    /// Converts geographical coordinate to position in the scene relative to the top-left corner of the map in map space.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="px">Relative position X</param>
    /// <param name="py">Relative position Y</param>
    public virtual void GetPosition(double lng, double lat, out double px, out double py)
    {
        double tx, ty;
        double dx, dy, dtx, dty;
        map.projection.CoordinatesToTile(lng, lat, map.zoom, out dx, out dy);
        map.GetTopLeftPosition(out tx, out ty);
        map.projection.CoordinatesToTile(tx, ty, map.zoom, out dtx, out dty);
        dx -= dtx;
        dy -= dty;
        int maxX = 1 << (map.zoom - 1);
        if (dx < -maxX) dx += maxX << 1;
        px = dx * OnlineMapsUtils.tileSize;
        py = dy * OnlineMapsUtils.tileSize;
    }

    /// <summary>
    /// Screen area occupied by the map.
    /// </summary>
    /// <returns>Screen rectangle</returns>
    public virtual Rect GetRect()
    {
        return new Rect();
    }

    /// <summary>
    /// Converts geographical coordinate to position in screen space.
    /// </summary>
    /// <param name="coords">Geographical coordinate (X - longitude, Y - latitude)</param>
    /// <returns>Screen space position</returns>
    public Vector2 GetScreenPosition(Vector2 coords)
    {
        return GetScreenPosition(coords.x, coords.y);
    }

    /// <summary>
    /// Converts geographical coordinate to position in screen space.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns>Screen space position</returns>
    public virtual Vector2 GetScreenPosition(double lng, double lat)
    {
        double mx, my;
        GetPosition(lng, lat, out mx, out my);
        mx /= map.width;
        my /= map.height;
        Rect mapRect = GetRect();
        mx = mapRect.x + mapRect.width * mx;
        my = mapRect.y + mapRect.height - mapRect.height * my;
        return new Vector2((float)mx, (float)my);
    }

    /// <summary>
    /// Returns the current number of touches.
    /// </summary>
    /// <returns>Number of touches</returns>
    public int GetTouchCount()
    {
        if (OnGetTouchCount != null) return OnGetTouchCount();

#if UNITY_WEBGL && !UNITY_EDITOR
        return Input.GetMouseButton(0) ? 1 : 0;
#else
        if (Input.touchSupported)
        {
            if (Input.touchCount > 0) return Input.touchCount;
        }
        return Input.GetMouseButton(0) ? 1 : 0;
#endif
    }

    /// <summary>
    /// Checks whether the cursor over the map.
    /// </summary>
    /// <returns>True - if the cursor over the map, false - if not.</returns>
    protected bool HitTest()
    {
        return HitTest(GetInputPosition());
    }

    /// <summary>
    /// Checks whether the cursor over the map.
    /// </summary>
    /// <param name="position">Screen position</param>
    /// <returns>True - if the cursor over the map, false - if not.</returns>
    protected virtual bool HitTest(Vector2 position)
    {
        return true;
    }

    /// <summary>
    /// Invokes OnMapBasePress.
    /// </summary>
    public void InvokeBasePress()
    {
        OnMapBasePress();
    }

    /// <summary>
    /// Invokes OnMapBaseRelease.
    /// </summary>
    public void InvokeBaseRelease()
    {
        OnMapBaseRelease();
    }

    protected bool IsCursorOnUIElement(Vector2 position)
    {
        if (!map.notInteractUnderGUI) return false;
#if !IGUI && ((!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR)
        if (GUIUtility.hotControl != 0) return true;
#endif
        if (EventSystem.current != null)
        {
            PointerEventData pe = new PointerEventData(EventSystem.current);
            pe.position = position;
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pe, hits);
            if (hits.Count > 0 && hits[0].gameObject != gameObject) return true;
        }

        return false;
    }

    /// <summary>
    /// Event that occurs before Awake.
    /// </summary>
    public virtual void OnAwakeBefore()
    {
        _instance = this;
    }

    private void OnDestroy()
    {
        OnMapClick = null;
        OnMapDoubleClick = null;
        OnMapDrag = null;
        OnMapLongPress = null;
        OnMapPress = null;
        OnMapRelease = null;
        OnMapZoom = null;
        lastClickTimes = null;
        map = null;
        _dragMarker = null;
        activeTexture = null;
        longPressEnumenator = null;
        _instance = null;

        OnDestroyLate();
    }

    /// <summary>
    /// Event is called after the control has been disposed.
    /// </summary>
    protected virtual void OnDestroyLate()
    {
        
    }

    private void OnEnable()
    {
        _instance = this;
        dragMarker = null;
        map = GetComponent<OnlineMaps>();
        if (map == null)
        {
            Debug.LogError("Can not find a script OnlineMaps.");
            OnlineMapsUtils.DestroyImmediate(this);
            return;
        }
        activeTexture = map.texture;
        map.control = this;
        OnEnableLate();
    }

    /// <summary>
    /// Function that is called after control of the map enabled.
    /// </summary>
    protected virtual void OnEnableLate()
    {
        
    }

    /// <summary>
    /// Called when a gesture zoom.
    /// </summary>
    /// <param name="p1">Screen coordinates of touch point 1</param>
    /// <param name="p2">Screen coordinates of touch point 2</param>
    protected virtual void OnGestureZoom(Vector2 p1, Vector2 p2)
    {
        
    }

    /// <summary>
    /// Method that is called when you press the map.
    /// </summary>
    protected void OnMapBasePress()
    {
        isMapPress = false;
        lockClick = false;

        if (waitZeroTouches)
        {
            if (GetTouchCount() <= 1) waitZeroTouches = false;
            else return;
        }

        dragMarker = null;
        if (!HitTest()) return;

        Vector2 inputPosition = GetInputPosition();

        if (IsCursorOnUIElement(inputPosition)) return;

        if (OnMapPress != null) OnMapPress();

        lastClickTimes[0] = lastClickTimes[1];
        lastClickTimes[1] = DateTime.Now.Ticks;

        bool hit = GetCoords(out lastPositionLng, out lastPositionLat);
        lastInputPosition = pressPoint = inputPosition;
        if (!hit) return;

        isMapPress = true;

        OnlineMapsMarkerBase marker = null;
        OnlineMapsDrawingElement drawingElement = null;

        IOnlineMapsInteractiveElement interactiveElement = GetInteractiveElement(inputPosition);

        if (interactiveElement != null)
        {
            if (interactiveElement is OnlineMapsMarkerBase) marker = interactiveElement as OnlineMapsMarkerBase;
            else if (interactiveElement is OnlineMapsDrawingElement) drawingElement = interactiveElement as OnlineMapsDrawingElement;
        }

        if (marker != null)
        {
            if (marker.OnPress != null) marker.OnPress(marker);
            if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onPress)
            {
                map.tooltipMarker = marker;
                map.tooltip = marker.label;
            }
            if (Input.GetKey(KeyCode.LeftControl)) dragMarker = marker;
        }
        else if (drawingElement != null)
        {
            if (drawingElement.OnPress != null) drawingElement.OnPress(drawingElement);
            if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onPress)
            {
                map.tooltipDrawingElement = drawingElement;
                map.tooltip = drawingElement.tooltip;
            }
        }

        if (dragMarker == null) isMapDrag = true;

        longPressEnumenator = WaitLongPress();
        StartCoroutine(longPressEnumenator);

        activeElement = interactiveElement;

        if (allowUserControl) OnlineMaps.isUserControl = true;
    }

    /// <summary>
    /// Method that is called when you release the map.
    /// </summary>
    protected void OnMapBaseRelease()
    {
        if (waitZeroTouches && GetTouchCount() == 0) waitZeroTouches = false;
        if (GUIUtility.hotControl != 0) return;

        Vector2 inputPosition = GetInputPosition();
        bool isClick = (pressPoint - inputPosition).sqrMagnitude < 400 && !lockClick;
        isMapDrag = false;
        mapDragStarted = false;
        dragMarker = null;

        if (longPressEnumenator != null)
        {
            StopCoroutine(longPressEnumenator);
            longPressEnumenator = null;
        }

        lastInputPosition = Vector2.zero;
        OnlineMaps.isUserControl = false;

        if (!isMapPress) return;
        isMapPress = false;
        if (OnMapRelease != null) OnMapRelease();

        OnlineMapsMarkerBase marker = null;
        OnlineMapsDrawingElement drawingElement = null;

        IOnlineMapsInteractiveElement interactiveElement = GetInteractiveElement(inputPosition);

        if (interactiveElement != null)
        {
            if (interactiveElement is OnlineMapsMarkerBase) marker = interactiveElement as OnlineMapsMarkerBase;
            else if (interactiveElement is OnlineMapsDrawingElement) drawingElement = interactiveElement as OnlineMapsDrawingElement;
        }

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onPress && (map.tooltipMarker != null || map.tooltipDrawingElement != null))
        {
            map.tooltipMarker = null;
            map.tooltipDrawingElement = null;
            map.tooltip = null;
        }

        bool isClicked = false;

        if (marker != null)
        {
            if (marker.OnRelease != null) marker.OnRelease(marker);
            if (isClick && marker.OnClick != null)
            {
                marker.OnClick(marker);
                isClicked = true;
            }
        }
        else if (drawingElement != null)
        {
            if (drawingElement.OnRelease != null) drawingElement.OnRelease(drawingElement);
        }

        if (activeElement != null && activeElement != interactiveElement)
        {
            if (activeElement is OnlineMapsMarkerBase)
            {
                OnlineMapsMarkerBase m = activeElement as OnlineMapsMarkerBase;
                if (m.OnRelease != null) m.OnRelease(m);
            }
            else if (activeElement is OnlineMapsDrawingElement)
            {
                OnlineMapsDrawingElement d = activeElement as OnlineMapsDrawingElement;
                if (d.OnRelease != null) d.OnRelease(d);
            }
            activeElement = null;
        }

        if (isClick && DateTime.Now.Ticks - lastClickTimes[0] < 5000000)
        {
            if (marker != null && marker.OnDoubleClick != null) marker.OnDoubleClick(marker);
            else if (drawingElement != null && drawingElement.OnDoubleClick != null) drawingElement.OnDoubleClick(drawingElement); 
            else
            {
                if (OnMapDoubleClick != null) OnMapDoubleClick();

                if (allowZoom && zoomInOnDoubleClick)
                {
                    if (!((marker != null && marker.OnClick != null) || (drawingElement != null && drawingElement.OnClick != null)))
                    {
                        ZoomOnPoint(1, inputPosition);
                    }
                }
            }
            
            lastClickTimes[0] = 0;
            lastClickTimes[1] = 0;
        }
        else if (isClick && !isClicked)
        {
            if (drawingElement != null && drawingElement.OnClick != null) drawingElement.OnClick(drawingElement);
            else if (OnMapClick != null) OnMapClick();
        }

        if (map.bufferStatus == OnlineMapsBufferStatus.wait) map.needRedraw = true;
    }

    public virtual OnlineMapsXML SaveSettings(OnlineMapsXML parent)
    {
        OnlineMapsXML element = parent.Create("Control");
        element.Create("AllowAddMarkerByM", allowAddMarkerByM);
        element.Create("AllowZoom", allowZoom);
        element.Create("AllowUserControl", allowUserControl);
        element.Create("ZoomInOnDoubleClick", zoomInOnDoubleClick);
        return element;
    }

    /// <summary>
    /// Specifies the texture, which will draw the map. In texture must be enabled "Read / Write Enabled".
    /// </summary>
    /// <param name="texture">Texture</param>
    public virtual void SetTexture(Texture2D texture)
    {
        activeTexture = texture;
    }

    protected void Update()
    {
        if (OnUpdateBefore != null) OnUpdateBefore();

        BeforeUpdate();
        _screenRect = GetRect();
        if (allowAddMarkerByM && Input.GetKeyUp(KeyCode.M)) CreateMarker();

        int touchCount = GetTouchCount();

        if (allowTouchZoom && touchCount != lastTouchCount)
        {
            if (touchCount == 1) OnMapBasePress();
            else if (touchCount == 0) OnMapBaseRelease();
        }
        lastTouchCount = touchCount;

        if (isMapDrag) UpdatePosition();

        if (allowZoom)
        {
            UpdateZoom();
            UpdateGestureZoom();
        }

        if (dragMarker != null) DragMarker();
        else if (HitTest()) map.ShowMarkersTooltip(GetInputPosition());
        else
        {
            map.tooltip = string.Empty;
            map.tooltipMarker = null;
        }
        AfterUpdate();

        if (OnUpdateAfter != null) OnUpdateAfter();
    }

    /// <summary>
    /// Pinch to zoom for iOS and Android
    /// </summary>
    protected virtual void UpdateGestureZoom()
    {
        if (!allowUserControl) return;

        if (GetTouchCount() == 2)
        {
            Vector2 p1 = Input.GetTouch(0).position;
            Vector2 p2 = Input.GetTouch(1).position;
            float distance = Vector2.Distance(p1, p2);

            Vector2 center = Vector2.Lerp(p1, p2, 0.5f);
            if (lastGestureCenter == Vector2.zero) lastGestureCenter = center;

            OnGestureZoom(p1, p2);

            if (Math.Abs(lastGestureDistance) < float.Epsilon)
            {
                lastGestureDistance = distance;
                return;
            }
            int z = 0;
            if (lastGestureDistance * 1.2f < distance) z = 1;
            else if (distance * 1.2f < lastGestureDistance) z = -1;

            if (invertTouchZoom) z *= -1;

            if (zoomMode == OnlineMapsZoomMode.target)
            {
                if (ZoomOnPoint(z, center)) lastGestureDistance = distance;
            }
            else if (z != 0)
            {
                map.zoom += z;
                lastGestureDistance = distance;
            }

            lastGestureCenter = center;
        }
        else
        {
            lastGestureDistance = 0;
            lastGestureCenter = Vector2.zero;
        }
    }

    /// <summary>
    /// Force updates the latest coordinates of cursor.
    /// </summary>
    public void UpdateLastPosition()
    {
        GetCoords(out lastPositionLng, out lastPositionLat);
    }

    /// <summary>
    /// Updates the map coordinates for the actions of the user.
    /// </summary>
    protected void UpdatePosition()
    {
        if (!allowUserControl) return;

        if (GetTouchCount() > 1) return;

        double lat, lng;
        bool hit = GetCoords(out lng, out lat);

        Vector2 inputPosition = GetInputPosition();
        if (!mapDragStarted && (lastInputPosition - inputPosition).magnitude < startDragDistance) return;
        mapDragStarted = true;

        if (hit && lastInputPosition != inputPosition)
        {
            double offsetX = lng - lastPositionLng;
            double offsetY = lat - lastPositionLat;

            if (offsetX > 270) offsetX -= 360;
            else if (offsetX < -270) offsetX += 360;
            
            if (Math.Abs(offsetX) > double.Epsilon || Math.Abs(offsetY) > double.Epsilon)
            {
                double px, py;
                map.GetPosition(out px, out py);
                px -= offsetX;
                py -= offsetY;
                map.SetPosition(px, py);

                map.needRedraw = true;

                if (longPressEnumenator != null)
                {
                    StopCoroutine(longPressEnumenator);
                    longPressEnumenator = null;
                }

                if (OnMapDrag != null) OnMapDrag();
            }

            GetCoords(out lastPositionLng, out lastPositionLat);

            lastInputPosition = inputPosition;
        }
    }

    /// <summary>
    /// Updates the map zoom for mouse wheel.
    /// </summary>
    protected void UpdateZoom()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return;
#endif

        if (!allowUserControl) return;
        if (!HitTest()) return;

        Vector2 inputPosition = GetInputPosition();

        if (inputPosition.x <= 0 || inputPosition.x >= Screen.width || inputPosition.y <= 0 || inputPosition.y >= Screen.height) return;
        if (IsCursorOnUIElement(inputPosition)) return;

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Math.Abs(wheel) < float.Epsilon) return;

#if NETFX_CORE
        wheel = -wheel;
#endif

        if (zoomMode == OnlineMapsZoomMode.target) ZoomOnPoint(wheel > 0 ? 1 : -1, inputPosition);
        else map.zoom += wheel > 0 ? 1 : -1;
    }

    private IEnumerator WaitLongPress()
    {
        yield return new WaitForSeconds(longPressDelay);

        OnlineMapsMarkerBase marker = null;
        OnlineMapsDrawingElement drawingElement = null;
        Vector2 inputPosition = GetInputPosition();

        IOnlineMapsInteractiveElement interactiveElement = GetInteractiveElement(inputPosition);

        if (interactiveElement != null)
        {
            if (interactiveElement is OnlineMapsMarkerBase) marker = interactiveElement as OnlineMapsMarkerBase;
            else if (interactiveElement is OnlineMapsDrawingElement) drawingElement = interactiveElement as OnlineMapsDrawingElement;
        }

        if (marker != null && marker.OnLongPress != null) marker.OnLongPress(marker);
        else if (drawingElement != null && drawingElement.OnLongPress != null) drawingElement.OnLongPress(drawingElement);
        else if (OnMapLongPress != null)
        {
            OnMapLongPress();
            isMapDrag = false;
        }

        longPressEnumenator = null;
    }

    /// <summary>
    /// Changes the zoom keeping a specified point on same place.
    /// </summary>
    /// <param name="zoomOffset">Positive - zoom in, Negative - zoom out</param>
    /// <param name="screenPosition">Screen position</param>
    /// <returns>True - if zoom changed, False - if zoom not changed</returns>
    public bool ZoomOnPoint(int zoomOffset, Vector2 screenPosition)
    {
        int newZoom = Mathf.Clamp(map.zoom + zoomOffset, 3, OnlineMaps.MAXZOOM);
        if (newZoom == map.zoom) return false;

        double mx, my;
        bool hit = GetCoords(out mx, out my, screenPosition);
        if (!hit) return false;

        double ox, oy;
        map.GetPosition(out ox, out oy);

        map.dispatchEvents = false;
        map.zoom = newZoom;
        map.SetPosition(mx, my);

        double px, py;
        map.GetPosition(out px, out py);
        if (GetCoords(out mx, out my, screenPosition))
        {
            px += px - mx;
            py += py - my;
            map.SetPosition(px, py);
        }
        else map.SetPosition(ox, oy);

        map.dispatchEvents = true;
        map.DispatchEvent(OnlineMapsEvents.changedPosition, OnlineMapsEvents.changedZoom);

        if (OnMapZoom != null) OnMapZoom();
        return true;
    }
}