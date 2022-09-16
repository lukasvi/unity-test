using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    public class MarkerClick : MonoBehaviour
    {
        private void Start()
        {
            OnlineMaps map = OnlineMaps.instance;
            
        // Add OnClick events to static markers
        foreach (OnlineMapsMarker marker in map.markers)
            {
                marker.OnClick += OnMarkerClick;
            //marker.SetDraggable();
            }

            // Add OnClick events to dynamic markers
            OnlineMapsMarker dynamicMarker = map.AddMarker(Vector2.zero, null, "Dynamic marker");
            dynamicMarker.OnClick += OnMarkerClick;
            //dynamicMarker.SetDraggable();
        }

        private void OnMarkerClick(OnlineMapsMarkerBase marker)
        {
        // Try get XML from customData.
        OnlineMapsXML xml = marker.customData as OnlineMapsXML;

        if (xml == null)
        {
            Debug.Log("The marker does not contain XML.");
            return;
        }

        // Show xml in console.
        Debug.Log(xml.outerXml);
        Debug.Log(xml.Get("ID"));
    }
}
