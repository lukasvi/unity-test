/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Search a route between two locations and draws the route.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/FindDirectionExample")]
    public class FindDirectionExample : MonoBehaviour
    {
        private void Start()
        {
            // Begin to search a route from Los Angeles to the specified coordinates.
            OnlineMapsGoogleDirections query = OnlineMapsGoogleDirections.Find("Los Angeles",
                new Vector2(-118.178960f, 35.063995f));

            // Specifies that search results must be sent to OnFindDirectionComplete.
            query.OnComplete += OnFindDirectionComplete;
        }

        private void OnFindDirectionComplete(string response)
        {
            // Get the resut object.
            OnlineMapsGoogleDirectionsResult result = OnlineMapsGoogleDirections.GetResult(response);

            // Check that the result is not null, and the number of routes is not zero.
            if (result == null || result.routes.Length == 0) 
            {
                Debug.Log("Find direction failed");
                Debug.Log(response);
                return;
            }

            // Showing the console instructions for each step.
            foreach (OnlineMapsGoogleDirectionsResult.Leg leg in result.routes[0].legs)
            {
                foreach (OnlineMapsGoogleDirectionsResult.Step step in leg.steps)
                {
                    Debug.Log(step.string_instructions);
                }
            }

            // Create a line, on the basis of points of the route.
            OnlineMapsDrawingLine route = new OnlineMapsDrawingLine(result.routes[0].overview_polylineD, Color.green);

            // Draw the line route on the map.
            OnlineMaps.instance.AddDrawingElement(route);
        }
    }
}