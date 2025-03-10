﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Instance of Billboard marker.
/// </summary>
[AddComponentMenu("")]
public class OnlineMapsMarkerBillboard : OnlineMapsMarkerInstanceBase
{
    /// <summary>
    /// Indicates whether to display the marker.
    /// </summary>
    public bool used;

    public override OnlineMapsMarkerBase marker
    {
        get { return _marker; }
        set { _marker = value as OnlineMapsMarker; }
    }

    [SerializeField] 
    private OnlineMapsMarker _marker;

    /// <summary>
    /// Creates a new instance of the billboard marker.
    /// </summary>
    /// <param name="marker">Marker</param>
    /// <returns>Instance of billboard marker</returns>
    public static OnlineMapsMarkerBillboard Create(OnlineMapsMarker marker)
    {
        GameObject billboardGO = new GameObject("Marker");
        SpriteRenderer spriteRenderer = billboardGO.AddComponent<SpriteRenderer>();
        OnlineMapsMarkerBillboard billboard = billboardGO.AddComponent<OnlineMapsMarkerBillboard>();
        
        billboard.marker = marker;
        marker.OnInitComplete += billboard.OnInitComplete;
        Texture2D texture = marker.texture;
        if (marker.texture == null) texture = OnlineMaps.instance.defaultMarkerTexture;
        if (texture != null)
        {
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
            spriteRenderer.flipX = true;
#endif
        }
        
        return billboard;
    }

    /// <summary>
    /// Dispose billboard instance
    /// </summary>
    public void Dispose()
    {
        if (gameObject != null) OnlineMapsUtils.DestroyImmediate(gameObject);
        if (marker != null) marker.OnInitComplete -= OnInitComplete;
        marker = null;
    }

    private void OnInitComplete(OnlineMapsMarkerBase markerBase)
    {
        OnlineMapsMarker marker = markerBase as OnlineMapsMarker;
        Texture2D texture = marker.texture;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (marker.texture == null) texture = OnlineMaps.instance.defaultMarkerTexture;
        if (texture != null)
        {
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
            spriteRenderer.flipX = true;
#endif
        }
    }

#if !UNITY_ANDROID
    protected void OnMouseDown()
    {
        OnlineMapsControlBase.instance.InvokeBasePress();
    }

    protected void OnMouseUp()
    {
        OnlineMapsControlBase.instance.InvokeBaseRelease();
    }
#endif

    private void Start()
    {
        gameObject.AddComponent<BoxCollider>();
        Update();
    }

    private void Update()
    {
        transform.LookAt(OnlineMapsControlBase3D.instance.activeCamera.transform.position);
        Vector3 euler = transform.rotation.eulerAngles;
        euler.y = 0;
        transform.rotation = Quaternion.Euler(euler);
    }
}