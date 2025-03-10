﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class implements the basic functionality control of the 3D map.
/// </summary>
[Serializable]
[AddComponentMenu("")]
public abstract class OnlineMapsControlBase3D: OnlineMapsControlBase
{
    /// <summary>
    /// Intercepts creates a 3d marker.\n
    /// Return null to create 3d marker using built-in manager.\n
    /// Return instance of 3d marker to prevent using built-in manager.
    /// </summary>
    public Func<double, double, GameObject, OnlineMapsMarker3D> OnAddMarker3D;

    /// <summary>
    /// The event, which occurs when controls the camera.
    /// </summary>
    public Action OnCameraControl;

    /// <summary>
    /// Event occurs after the addition of 3D marker.
    /// </summary>
    public Action<OnlineMapsMarker3D> OnMarker3DAdded;

    /// <summary>
    /// Intercepts removes a 3d marker.\n
    /// Return FALSE to remove 3d marker using built-in manager.\n
    /// Return TRUE to prevent using built-in manager.
    /// </summary>
    public Predicate<OnlineMapsMarker3D> OnRemoveMarker3D;

    /// <summary>
    /// Intercepts removes a 3d marker.\n
    /// Return FALSE to remove 3d marker using built-in manager.\n
    /// Return TRUE to prevent using built-in manager.
    /// </summary>
    public Predicate<int> OnRemoveMarker3DAt;

    /// <summary>
    /// The camera you are using to display the map.
    /// </summary>
    public Camera activeCamera;

    /// <summary>
    /// Specifies whether to create a 3D marker by pressing N under the cursor.
    /// </summary>
    public bool allowAddMarker3DByN = true;

    [Obsolete]
    public bool allowDefaultMarkerEvents = true;

    /// <summary>
    /// Specifies controls the camera.\n
    /// Use the right mouse button or two-finger swipe to control the camera.
    /// </summary>
    public bool allowCameraControl = false;

    /// <summary>
    /// Indicates the point at which the camera should look.
    /// </summary>
    public OnlineMapsCameraAdjust cameraAdjustTo = OnlineMapsCameraAdjust.maxElevationInArea;

    /// <summary>
    /// The distance from the camera to the center point of the map.\n
    /// Used when the allowCameraControl.
    /// </summary>
    public float cameraDistance = 1000;

    /// <summary>
    /// Rotation angle of the camera.\n
    /// Used when the allowCameraControl.
    /// </summary>
    public Vector2 cameraRotation = Vector2.zero;

    /// <summary>
    /// Camera movement speed.\n
    /// Used when the allowCameraControl.
    /// </summary>
    public Vector2 cameraSpeed = Vector2.one;

    /// <summary>
    /// Default 3D marker.
    /// </summary>
    public GameObject default3DMarker;

    /// <summary>
    /// Mode of 2D markers. Bake in texture or Billboard.
    /// </summary>
    public OnlineMapsMarker2DMode marker2DMode = OnlineMapsMarker2DMode.flat;

    /// <summary>
    /// Size of billboard markers.
    /// </summary>
    public float marker2DSize = 100;

    /// <summary>
    /// List of 3D markers.
    /// </summary>
    public OnlineMapsMarker3D[] markers3D;

    /// <summary>
    /// Scaling of 3D markers by default.
    /// </summary>
    public float marker3DScale = 1;

    /// <summary>
    /// Maximum camera rotation by X axis.
    /// </summary>
    public float maxCameraRotationX = 80;

    /// <summary>
    /// The original position of the map GameObject.
    /// </summary>
    public Vector3 originalPosition;

    /// <summary>
    /// The original scale of the map GameObject.
    /// </summary>
    public Vector3 originalScale;

    protected List<GameObject> markersGameObjects;
    protected List<Mesh> markersMeshes;
    protected List<Renderer> markersRenderers;

#if (!UNITY_ANDROID && !UNITY_IPHONE) || UNITY_EDITOR
    private bool isCameraControl = false;
#endif
    private Dictionary<int, OnlineMapsMarkerBillboard> markerBillboards;

    private Collider _cl;
    private Renderer _renderer;

    /// <summary>
    /// Reference to the collider.
    /// </summary>
    public Collider cl
    {
        get
        {
            if (_cl == null)
            {
#if UNITY_4_6 || UNITY_4_7
                _cl = collider;
#else
                _cl = GetComponent<Collider>();
#endif
            }
            return _cl;
        }
    }

    /// <summary>
    /// Reference to the renderer.
    /// </summary>
    public Renderer rendererInstance
    {
        get
        {
            if (_renderer == null)
            {
#if UNITY_4_6 || UNITY_4_7
                _renderer = renderer;
#else
                _renderer = GetComponent<Renderer>();
#endif
            }
            return _renderer;
        }
    }

    /// <summary>
    /// Singleton instance of OnlineMapsControlBase3D control.
    /// </summary>
    public new static OnlineMapsControlBase3D instance
    {
        get { return OnlineMapsControlBase.instance as OnlineMapsControlBase3D; }
    }

    /// <summary>
    /// Adds a new 3D marker on the map.
    /// </summary>
    /// <param name="markerPosition">Vector2. X - Longituge, Y - Latitude.</param>
    /// <param name="prefab">Marker prefab.</param>
    /// <returns>Marker instance.</returns>
    public OnlineMapsMarker3D AddMarker3D(Vector2 markerPosition, GameObject prefab)
    {
        return AddMarker3D(markerPosition.x, markerPosition.y, prefab);
    }

    /// <summary>
    /// Adds a new 3D marker on the map.
    /// </summary>
    /// <param name="markerLng">Marker longitude.</param>
    /// <param name="markerLat">Marker latitude.</param>
    /// <param name="prefab">Marker prefab.</param>
    /// <returns>Marker instance.</returns>
    public OnlineMapsMarker3D AddMarker3D(double markerLng, double markerLat, GameObject prefab)
    {
        OnlineMapsMarker3D marker;

        if (OnAddMarker3D != null)
        {
            marker = OnAddMarker3D(markerLng, markerLat, prefab);
            if (marker != null) return marker;
        }

        Array.Resize(ref markers3D, markers3D.Length + 1);
        marker = new OnlineMapsMarker3D
        {
            prefab = prefab,
            control = this,
            scale = marker3DScale,
        };
        marker.SetPosition(markerLng, markerLat);
        marker.Init(transform);
        markers3D[markers3D.Length - 1] = marker;

        if (OnMarker3DAdded != null) OnMarker3DAdded(marker);

        return marker;
    }

    /// <summary>
    /// Adds a existing 3D marker on the map.
    /// </summary>
    /// <param name="marker">Instance of 3D marker.</param>
    /// <returns>Instance of 3D marker.</returns>
    public OnlineMapsMarker3D AddMarker3D(OnlineMapsMarker3D marker)
    {
        Array.Resize(ref markers3D, markers3D.Length + 1);
        markers3D[markers3D.Length - 1] = marker;
        if (OnMarker3DAdded != null) OnMarker3DAdded(marker);
        return marker;
    }

    protected override void AfterUpdate()
    {
        base.AfterUpdate();

        Vector2 inputPosition = GetInputPosition();

        if (allowCameraControl && !IsCursorOnUIElement(inputPosition))
        {
#if (!UNITY_ANDROID && !UNITY_IPHONE) || UNITY_EDITOR
            if (Input.GetMouseButton(1))
            {
                isCameraControl = true;
                if (lastInputPosition != inputPosition && lastInputPosition != Vector2.zero)
                {
                    Vector2 offset = lastInputPosition - inputPosition;
                    cameraRotation.x -= offset.y / 10f * cameraSpeed.x;
                    cameraRotation.y -= offset.x / 10f * cameraSpeed.y;
                }
                lastInputPosition = inputPosition;
            }
            else if (isCameraControl)
            {
                lastInputPosition = Vector2.zero;
                isCameraControl = false;
            }
#else
            if (!allowZoom && allowCameraControl)
            {
                if (Input.touchCount == 2)
                {
                    Vector2 p1 = Input.GetTouch(0).position;
                    Vector2 p2 = Input.GetTouch(1).position;

                    Vector2 center = Vector2.Lerp(p1, p2, 0.5f);
                    if (lastGestureCenter == Vector2.zero) lastGestureCenter = center;
                    else if (lastGestureCenter != center)
                    {
                        Vector2 offset = lastGestureCenter - center;
                        cameraRotation.x -= offset.y / 10f * cameraSpeed.x;
                        cameraRotation.y -= offset.x / 10f * cameraSpeed.y;
                        lastGestureCenter = center;
                    }

                    lastInputPosition = center;
                }
                else
                {
                    lastGestureCenter = Vector2.zero;
                }
            }
#endif

            UpdateCameraPosition();
        }

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onHover)
        {
            OnlineMapsMarkerInstanceBase markerInstance = GetBillboardMarkerFromScreen(inputPosition);
            if (markerInstance != null)
            {
                map.tooltip = markerInstance.marker.label;
                map.tooltipMarker = markerInstance.marker;
            }
        }

        if (allowAddMarker3DByN && Input.GetKeyUp(KeyCode.N))
        {
            GameObject prefabInstance = default3DMarker != null ? Instantiate(default3DMarker) as GameObject : null;
            OnlineMapsMarker3D marker = AddMarker3D(GetCoords(), prefabInstance);
            if (prefabInstance != null) OnlineMapsUtils.DestroyImmediate(prefabInstance);
            marker.control = this;
            marker.scale = marker3DScale;
        }
    }

    protected void Clear2DMarkerBillboards()
    {
        if (markerBillboards != null)
        {
            foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> pair in markerBillboards)
            {
                if (pair.Value != null) pair.Value.Dispose();
            }
        }
        
        markerBillboards = null;
        foreach (GameObject go in markersGameObjects) OnlineMapsUtils.DestroyImmediate(go);
        markersGameObjects = null;
        markersMeshes = null;
        markersRenderers = null;
    }

    /// <summary>
    /// Clears instances of 2D markers.
    /// </summary>
    /// <param name="mode"></param>
    public virtual void Clear2DMarkerInstances(OnlineMapsMarker2DMode mode)
    {
        if (mode == OnlineMapsMarker2DMode.billboard) Clear2DMarkerBillboards();
        else OnlineMaps.instance.Redraw(); 
    }

    /// <summary>
    /// Returns best value yScale, based on the coordinates corners of map.
    /// </summary>
    /// <param name="topLeftPosition">Top-Left corner coordinates.</param>
    /// <param name="bottomRightPosition">Bottom-Right corner coordinates.</param>
    /// <returns>Best yScale</returns>
    public virtual float GetBestElevationYScale(Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        return 0;
    }

    /// <summary>
    /// Returns best value yScale, based on the coordinates corners of map.
    /// </summary>
    /// <param name="tlx">Top-left longitude.</param>
    /// <param name="tly">Top-left latitude.</param>
    /// <param name="brx">Bottom-right longitude.</param>
    /// <param name="bry">Bottom-right latitude.</param>
    /// <returns></returns>
    public virtual float GetBestElevationYScale(double tlx, double tly, double brx, double bry)
    {
        double dx, dy;
        OnlineMapsUtils.DistanceBetweenPoints(tlx, tly, brx, bry, out dx, out dy);
        return (float)Math.Min(map.width / dx, map.height / dy) / 1000;
    }

    /// <summary>
    /// Gets billboard marker on the screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position.</param>
    /// <returns>Marker instance or null.</returns>
    public OnlineMapsMarkerInstanceBase GetBillboardMarkerFromScreen(Vector2 screenPosition)
    {
        if (IsCursorOnUIElement(screenPosition)) return null;

        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            return hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
        }
        return null;
    }

    /// <summary>
    /// Returns the elevation value, based on the local position in the scene.
    /// </summary>
    /// <param name="x">Local position X.</param>
    /// <param name="z">Local position Z.</param>
    /// <param name="yScale">Scale factor for elevation value.</param>
    /// <param name="topLeftPosition">Top-Left corner coordinates of map.</param>
    /// <param name="bottomRightPosition">Bottom-Right corner coordinates of map.</param>
    /// <returns>Elevation value.</returns>
    public virtual float GetElevationValue(double x, double z, float yScale, Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        return 0;
    }

    /// <summary>
    /// Returns the elevation value, based on the local position in the scene.
    /// </summary>
    /// <param name="x">Local position X.</param>
    /// <param name="z">Local position Z.</param>
    /// <param name="yScale">Scale factor for elevation value.</param>
    /// <param name="tlx">Top-left longitude of map.</param>
    /// <param name="tly">Top-left latitude of map.</param>
    /// <param name="brx">Bottom-right longitude of map.</param>
    /// <param name="bry">Bottom-right latitude of map.</param>
    /// <returns>Elevation value.</returns>
    public virtual float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        return 0;
    }

    public override IOnlineMapsInteractiveElement GetInteractiveElement(Vector2 screenPosition)
    {
        if (IsCursorOnUIElement(screenPosition)) return null;

        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            OnlineMapsMarkerInstanceBase markerInstance = hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
            if (markerInstance != null) return markerInstance.marker;
        }

        OnlineMapsMarker marker = map.GetMarkerFromScreen(screenPosition);
        if (marker != null) return marker;

        OnlineMapsDrawingElement drawingElement = map.GetDrawingElement(screenPosition);
        return drawingElement;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double px, py;
        GetPosition(lng, lat, out px, out py);
        px /= map.width;
        py /= map.height;
        Vector3 worldPos = new Vector3();

        Bounds bounds = cl.bounds;
        worldPos.x = (float)(bounds.max.x - bounds.size.x * px);
        worldPos.y = bounds.min.y;
        worldPos.z = (float)(bounds.min.z + bounds.size.z * py);

        Camera cam = activeCamera ?? Camera.main;
        return cam.WorldToScreenPoint(worldPos);
    }

    protected Mesh InitMarkersMesh(int index)
    {
        string name = "Markers";
        if (index > 0) name += index;
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        go.layer = gameObject.layer;

        if (this is OnlineMapsTileSetControl)
        {
            go.transform.localPosition = new Vector3(0, OnlineMaps.instance.tilesetSize.magnitude / 2896, 0);
            go.transform.localRotation = Quaternion.Euler(Vector3.zero);
        }
        else go.transform.localPosition = new Vector3(0, 0.2f, 0);
        go.transform.localScale = Vector3.one;

        if (markersGameObjects == null) markersGameObjects = new List<GameObject>();
        if (markersRenderers == null) markersRenderers = new List<Renderer>();
        if (markersMeshes == null) markersMeshes = new List<Mesh>();

        markersGameObjects.Add(go);
        markersRenderers.Add(go.AddComponent<MeshRenderer>());

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "MarkersMesh";
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;
        markersMeshes.Add(mesh);

        return mesh;
    }

    protected override void OnDestroyLate()
    {
        base.OnDestroyLate();

        OnCameraControl = null;

        if (markersGameObjects != null)
        {
            foreach (GameObject go in markersGameObjects)
            {
                OnlineMapsUtils.DestroyImmediate(go);
            }
        }
        markersGameObjects = null;

        markers3D = null;
        markersMeshes = null;
        markersRenderers = null;
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        if (activeCamera == null) activeCamera = Camera.main;
    }

    protected override void OnGestureZoom(Vector2 p1, Vector2 p2)
    {
        base.OnGestureZoom(p1, p2);
        if (allowCameraControl)
        {
            Vector2 center = Vector2.Lerp(p1, p2, 0.5f);
            if (lastGestureCenter != center)
            {
                Vector2 offset = lastGestureCenter - center;
                cameraRotation.x -= offset.y / 10f * cameraSpeed.x;
                cameraRotation.y -= offset.x / 10f * cameraSpeed.y;
            }
            lastInputPosition = center;
        }
    }

    /// <summary>
    /// Remove all 3D markers from map.
    /// </summary>
    public void RemoveAllMarker3D()
    {
        foreach (OnlineMapsMarker3D marker in markers3D)
        {
            if (OnRemoveMarker3D != null && OnRemoveMarker3D(marker)) continue;
            if (marker.instance != null) OnlineMapsUtils.DestroyImmediate(marker.instance);
            marker.Dispose();
        }
        markers3D = new OnlineMapsMarker3D[0];
    }

    /// <summary>
    /// Removes the specified 3D marker.
    /// </summary>
    /// <param name="marker">3D marker</param>
    public void RemoveMarker3D(OnlineMapsMarker3D marker)
    {
        if (OnRemoveMarker3D != null && OnRemoveMarker3D(marker)) return;

        List<OnlineMapsMarker3D> ms = markers3D.ToList();
        ms.Remove(marker);
        markers3D = ms.ToArray();
        if (marker.instance != null) OnlineMapsUtils.DestroyImmediate(marker.instance);
        marker.Dispose();
    }

    /// <summary>
    /// Removes 3D marker by index.
    /// </summary>
    /// <param name="markerIndex">Marker index.</param>
    public void RemoveMarker3DAt(int markerIndex)
    {
        if (OnRemoveMarker3DAt != null && OnRemoveMarker3DAt(markerIndex)) return;

        if (markerIndex < 0 || markerIndex >= markers3D.Length) return;

        OnlineMapsMarker3D marker = markers3D[markerIndex];
        List<OnlineMapsMarker3D> ms = markers3D.ToList();
        ms.Remove(marker);
        markers3D = ms.ToArray();
        if (marker.instance != null) OnlineMapsUtils.DestroyImmediate(marker.instance);
        marker.Dispose();
    }

    /// <summary>
    /// Removes all 3D markers that contain a tag.
    /// </summary>
    /// <param name="tags">Tags of 3D markers that must be removed.</param>
    public void RemoveMarkers3DByTag(params string[] tags)
    {
        if (tags.Length == 0) return;

        int countRemoved = 0;
        for (int i = 0; i < markers3D.Length; i++)
        {
            OnlineMapsMarker3D marker = markers3D[i];
            if (marker.tags == null || marker.tags.Count == 0) continue;

            bool hasTag = false;
            for (int j = 0; j < tags.Length; j++)
            {
                if (marker.tags.Contains(tags[j]))
                {
                    hasTag = true;
                    break;
                }
            }

            if (hasTag)
            {
                marker.Dispose();
                if (marker.instance != null) OnlineMapsUtils.DestroyImmediate(marker.instance);
                countRemoved++;
            }
            else markers3D[i - countRemoved] = marker;
        }

        if (countRemoved > 0) Array.Resize(ref markers3D, markers3D.Length - countRemoved);
    }

    public OnlineMapsXML SaveMarkers3D(OnlineMapsXML parent)
    {
        if (markers3D == null || markers3D.Length == 0) return null;
        OnlineMapsXML element = parent.Create("Markers3D");
        foreach (OnlineMapsMarker3D marker in markers3D) marker.Save(element);
        return element;
    }

    public override OnlineMapsXML SaveSettings(OnlineMapsXML parent)
    {
        OnlineMapsXML element = base.SaveSettings(parent);
        element.Create("AllowAddMarker3DByN", allowAddMarker3DByN);
        element.Create("AllowCameraControl", allowCameraControl);

        if (allowCameraControl)
        {
            element.Create("CameraDistance", cameraDistance);
            element.Create("CameraRotation", cameraRotation);
            element.Create("CameraSpeed", cameraSpeed);
        }

        element.Create("Marker2DMode", (int) marker2DMode);
        element.Create("Marker2DSize", marker2DSize);
        element.Create("Marker3DScale", marker3DScale);
        element.Create("Camera", activeCamera);

        return element;
    }

    private void Start()
    {
        foreach (OnlineMapsMarker3D marker in markers3D.Where(m => !m.inited))
        {
            marker.control = this;
            marker.Init(transform);
        }
        UpdateMarkers3D();
    }

    protected void UpdateCameraPosition()
    {
        if (cameraRotation.x > maxCameraRotationX) cameraRotation.x = maxCameraRotationX;
        else if (cameraRotation.x < 0) cameraRotation.x = 0;

        float rx = 90 - cameraRotation.x;
        if (rx > 89.9) rx = 89.9f;

        double px = Math.Cos(rx * Mathf.Deg2Rad) * cameraDistance;
        double py = Math.Sin(rx * Mathf.Deg2Rad) * cameraDistance;
        double pz = Math.Cos(cameraRotation.y * Mathf.Deg2Rad) * px;
        px = Math.Sin(cameraRotation.y * Mathf.Deg2Rad) * px;

        Vector3 targetPosition = transform.position;
        if (this is OnlineMapsTileSetControl)
        {
            Vector3 offset = new Vector3(map.tilesetSize.x / -2, 0, map.tilesetSize.y / 2);
            OnlineMapsTileSetControl control = OnlineMapsTileSetControl.instance;

            if (control.smoothZoom && control.smoothZoomStarted) targetPosition = originalPosition;
            
            if (control.useElevation && control.elevationZoomRange.InRange(map.zoom))
            {
                double tlx, tly, brx, bry;
                map.GetCorners(out tlx, out tly, out brx, out bry);
                float yScale = control.GetBestElevationYScale(tlx, tly, brx, bry);
                if (cameraAdjustTo == OnlineMapsCameraAdjust.maxElevationInArea) offset.y = control.GetMaxElevationValue(yScale);
                else offset.y = control.GetElevationValue(targetPosition.x, targetPosition.z, yScale, tlx, tly, brx, bry);
            }
            
            offset.Scale(control.smoothZoomStarted? control.originalScale: transform.lossyScale);
            targetPosition += transform.rotation * offset;
        }

        Vector3 oldPosition = activeCamera.transform.position;
        Vector3 newPosition = transform.rotation * new Vector3((float)px, (float)py, (float)pz) + targetPosition;

        activeCamera.transform.position = newPosition;
        activeCamera.transform.LookAt(targetPosition);

        if (oldPosition != newPosition && OnCameraControl != null) OnCameraControl();
    }

    /// <summary>
    /// Updates the current control.
    /// </summary>
    public virtual void UpdateControl()
    {
        if (marker2DMode == OnlineMapsMarker2DMode.billboard) UpdateMarkersBillboard();
        UpdateMarkers3D();
    }

    protected void UpdateMarkers3D()
    {
        if (markers3D == null) return;

        int zoom = map.zoom;

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);

        double ttlx, ttly, tbrx, tbry;
        map.projection.CoordinatesToTile(tlx, tly, zoom, out ttlx, out ttly);
        map.projection.CoordinatesToTile(brx, bry, zoom, out tbrx, out tbry);

        Bounds bounds = cl.bounds;
        float bestYScale = GetBestElevationYScale(tlx, tly, brx, bry);

        for (int i = 0; i < markers3D.Length; i++)
        {
            OnlineMapsMarker3D marker = markers3D[i];
            if (marker != null) marker.Update(map, this, bounds, tlx, tly, brx, bry, zoom, ttlx, ttly, tbrx, tbry, bestYScale);
        }
    }

    /// <summary>
    /// Updates billboard markers.
    /// </summary>
    protected void UpdateMarkersBillboard()
    {
        if (markersGameObjects == null) InitMarkersMesh(0);
        if (markerBillboards == null) markerBillboards = new Dictionary<int, OnlineMapsMarkerBillboard>();

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        if (brx < tlx) brx += 360;

        int maxX = 1 << map.buffer.apiZoom;

        double px, py;
        map.projection.CoordinatesToTile(tlx, tly, map.zoom, out px, out py);

        float yScale = GetBestElevationYScale(tlx, tly, brx, bry);

        Bounds mapBounds = cl.bounds;
        Vector3 positionOffset = transform.position - mapBounds.min;
        Vector3 size = mapBounds.size;
        size = transform.rotation * size;
        if (map.target == OnlineMapsTarget.tileset) positionOffset.x -= size.x;

        foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> billboard in markerBillboards) billboard.Value.used = false;

        foreach (OnlineMapsMarker marker in map.markers)
        {
            if (!marker.enabled || !marker.range.InRange(map.zoom)) continue;

            double mx, my;
            marker.GetPosition(out mx, out my);
            
            if (!((mx > tlx && mx < brx || mx + 360 > tlx && mx + 360 < brx ||
                 mx - 360 > tlx && mx - 360 < brx) &&
                my < tly && my > bry)) continue;

            int markerHashCode = marker.GetHashCode();
            OnlineMapsMarkerBillboard markerBillboard;

            if (!markerBillboards.ContainsKey(markerHashCode))
            {
                markerBillboard = OnlineMapsMarkerBillboard.Create(marker);
                markerBillboard.transform.parent = markersGameObjects[0].transform;
                markerBillboard.gameObject.layer = markersGameObjects[0].layer;

                markerBillboards.Add(markerHashCode, markerBillboard);
            }
            else markerBillboard = markerBillboards[markerHashCode];

            if (markerBillboard == null) continue;

            float sx = size.x / map.width * marker2DSize * marker.scale;
            float sz = size.z / map.height * marker2DSize * marker.scale;
            float s = Mathf.Max(sx, sz);

#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
            markerBillboard.transform.localScale = new Vector3(s, s, s);
#else
            markerBillboard.transform.localScale = new Vector3(-s, s, s);
#endif

            double mpx, mpy;
            map.projection.CoordinatesToTile(mx, my, map.buffer.apiZoom, out mpx, out mpy);

            mpx = OnlineMapsUtils.Repeat(mpx - px, 0, maxX);
            mpy -= py;

            float x = (float)(-mpx / map.width * OnlineMapsUtils.tileSize *  size.x + positionOffset.x);
            float z = (float)(mpy / map.height * OnlineMapsUtils.tileSize * size.z - positionOffset.z);

            float y = GetElevationValue(x, z, yScale, tlx, tly, brx, bry);

            markerBillboard.transform.localPosition = transform.rotation * new Vector3(x, y, z);
            markerBillboard.used = true;
        }

        List<int> keysForRemove = new List<int>();

        foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> billboard in markerBillboards)
        {
            if (!billboard.Value.used)
            {
                billboard.Value.Dispose();
                keysForRemove.Add(billboard.Key);
            }
        }

        foreach (int key in keysForRemove) markerBillboards.Remove(key);
    }
}