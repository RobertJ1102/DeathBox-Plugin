using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace DeathBox.Helpers
{
    public static class BedProximityHelper
    {
        private const float NearBedRadius = 5f;
        private const float NearBedSqrRadius = NearBedRadius * NearBedRadius;

        public static bool IsNearBed(Vector3 position)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(position, NearBedRadius, regions);

            List<Transform> barricades = new List<Transform>();
            BarricadeManager.getBarricadesInRadius(position, NearBedSqrRadius, barricades);
            BarricadeManager.getBarricadesInRadius(position, NearBedSqrRadius, regions, barricades);

            foreach (Transform transform in barricades)
            {
                if (transform != null && transform.GetComponent<InteractableBed>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
