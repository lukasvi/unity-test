/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example how to find the name of the location by coordinates.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/FindLocationNameExample")]
    public class FindLocationNameExample : MonoBehaviour
    {
        private void Start()
        {
            // Subscribe to click event.
            OnlineMapsControlBase.instance.OnMapClick += OnMapClick;
        }

        private void OnMapClick()
        {
            // Get the coordinates where the user clicked.
            Vector2 mouseCoords = OnlineMapsControlBase.instance.GetCoords();

            // Try find location name by coordinates.
            OnlineMapsGoogleGeocoding query = OnlineMapsGoogleGeocoding.Find(mouseCoords);
            query.OnComplete += OnQueryComplete;
        }

        private void OnQueryComplete(string s)
        {
            // Show response in console.
            Debug.Log(s);
        }
    }
}