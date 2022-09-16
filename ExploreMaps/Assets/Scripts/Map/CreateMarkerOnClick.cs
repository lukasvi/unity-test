using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateMarkerOnClick : MonoBehaviour
{

    public GameObject poiData;
    public GameObject submitButton;


    public float radius = 64; //pixels
    public int segments = 16;
    private OnlineMapsMarker marker;
    private OnlineMaps map;
    private List<Vector2> points;


    private void Start()
    {
        // Subscribe to the click event.
        OnlineMapsControlBase.instance.OnMapClick += OnMapClick;
    }

    private void OnMapClick()
    {
        if (Handler.markerAddable)
        {

            map = OnlineMaps.instance;
            // Get the coordinates under the cursor.
            double lng, lat;
            OnlineMapsControlBase.instance.GetCoords(out lng, out lat);

            // Create a label for the marker.
            string label = "lat: " + lat + "\n" + "long: " + lng;

            // Create a new marker.
            marker = map.AddMarker(lng, lat, label);
            //marker.SetDraggable();

            
            // Init points
            List<Vector2> points = new List<Vector2>(segments);
            for (int i = 0; i < segments; i++) points.Add(new Vector2());
            OnlineMapsDrawingPoly poly;

            poly = new OnlineMapsDrawingPoly(points, Color.green, 3);

            // Draw circle
            UpdateCircle();

            map.AddDrawingElement(poly);


            Handler.markerPosition = marker.position;
            Handler.markerAddable = false;

            poiData.SetActive(true);
            submitButton.SetActive(true);

        }
    }



    private void UpdateCircle()
    {
        float r = radius / OnlineMapsUtils.tileSize;
        float step = 360f / segments;
        double x, y;
        marker.GetPosition(out x, out y);

        // Old way (Online Maps v2.3):
        // OnlineMapsUtils.LatLongToTiled(x, y, map.zoom, out x, out y);

        OnlineMapsProjection projection = map.projection;
        projection.CoordinatesToTile(x, y, map.zoom, out x, out y);

        for (int i = 0; i < segments; i++)
        {
            double px = x + Mathf.Cos(step * i * Mathf.Deg2Rad) * r;
            double py = y + Mathf.Sin(step * i * Mathf.Deg2Rad) * r;

            // Old way (Online Maps v2.3):
            // OnlineMapsUtils.TileToLatLong(px, py, map.zoom, out px, out py);

            projection.TileToCoordinates(px, py, map.zoom, out px, out py);

            points[i] = new Vector2((float)px, (float)py);
        }
    }
}