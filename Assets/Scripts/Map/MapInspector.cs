/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Map
{
    [ExecuteInEditMode]
    public class MapInspector : MonoBehaviour
    {
        public double Latitude;
        public double Longitude;
        public double Northing;
        public double Easting;

        void Update()
        {
            var map = MapOrigin.Find();
            if (map == null)
            {
                Debug.Log("MapOrigin Not Found!");
                return;
            }

            var location = map.GetGpsLocation(transform.position);
            
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Northing = location.Northing;
            Easting = location.Easting;
        }
    }
}
