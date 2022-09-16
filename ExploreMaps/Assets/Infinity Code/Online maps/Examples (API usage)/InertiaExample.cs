/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.OnlineMapsExamples
{
    /// <summary>
    /// Example of how to make the inertia, when you move the map.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Examples (API Usage)/InertiaExample")]
    public class InertiaExample : MonoBehaviour
    {
        /// <summary>
        /// Deceleration rate (0 - 1).
        /// </summary>
        public float friction = 0.9f;

        private bool isInteract;
        private List<double> speedX;
        private List<double> speedY;
        private double rsX;
        private double rsY;
        private double lng;
        private double lat;
        private const int maxSamples = 5;

        private bool isSmoothZoomProceed;
        private bool waitZeroTouches;

        private void FixedUpdate()
        {
            if (isSmoothZoomProceed || waitZeroTouches) return;

            // If there is interaction with the map.
            if (isInteract)
            {
                // Calculates speeds.
                double nlng, nlat;
                OnlineMaps.instance.GetPosition(out nlng, out nlat);
                double cSpeedX = nlng - lng;
                double cSpeedY = nlat - lat;

                speedX.Add(cSpeedX);
                speedY.Add(cSpeedY);

                while (speedX.Count > maxSamples) speedX.RemoveAt(0);
                while (speedY.Count > maxSamples) speedY.RemoveAt(0);

                lng = nlng;
                lat = nlat;
            }
            // If no interaction with the map.
            else
            {
                // Continue to move the map with the current speed.
                double clng, clat;
                OnlineMaps.instance.GetPosition(out clng, out clat);
                clng += rsX;
                clat += rsY;
                OnlineMaps.instance.SetPosition(clng, clat);

                // Reduces the current speed.
                rsX *= friction;
                rsY *= friction;
            }
        }

        /// <summary>
        /// This method is called when you press on the map.
        /// </summary>
        private void OnMapPress()
        {
            // Get coordinates of map
            OnlineMaps.instance.GetPosition(out lng, out lat);

            // Is marked, that is the interaction with the map.
            isInteract = true;
        }

        /// <summary>
        /// This method is called when you release on the map.
        /// </summary>
        private void OnMapRelease()
        {
            // Is marked, that ended the interaction with the map.
            isInteract = false;

            // Calculates the average speed.
            rsX = speedX.Count > 0 ? speedX.Average() : 0;
            rsY = speedY.Count > 0 ? speedY.Average() : 0;

            if (waitZeroTouches && OnlineMapsControlBase.instance.GetTouchCount() == 0)
            {
                waitZeroTouches = false;
                rsX = rsY = 0;
            }

            speedX.Clear();
            speedY.Clear();
        }

        private void OnSmoothZoomFinish()
        {
            speedX.Clear();
            speedY.Clear();
            rsX = 0;
            rsY = 0;

            isSmoothZoomProceed = false;

            if (OnlineMapsControlBase.instance.GetTouchCount() != 0) waitZeroTouches = true;
        }

        private void OnSmoothZoomBegin()
        {
            speedX.Clear();
            speedY.Clear();
            rsX = 0;
            rsY = 0;
            isSmoothZoomProceed = true;
        }

        private void Start()
        {
            // Subscribe to map events
            OnlineMapsControlBase.instance.OnMapPress += OnMapPress;
            OnlineMapsControlBase.instance.OnMapRelease += OnMapRelease;

            // Prevents inertia with smooth zoom.
            if (OnlineMapsTileSetControl.instance != null)
            {
                OnlineMapsTileSetControl.instance.OnSmoothZoomBegin += OnSmoothZoomBegin;
                OnlineMapsTileSetControl.instance.OnSmoothZoomFinish += OnSmoothZoomFinish;
            }

            // Initialize arrays of speed
            speedX = new List<double>();
            speedY = new List<double>();
        }
    }
}