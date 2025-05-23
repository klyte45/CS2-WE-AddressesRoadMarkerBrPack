using Colossal.Entities;
using System;
using Unity.Entities;

namespace BridgeAdr
{
    public static class ADRRoadMarkerInfoBridge
    {
        public static (Colossal.Hash128 routeDataIndex, int routeDirection, int numericCustomParam1, int numericCustomParam2) DesconstructRoadMarkerData(Entity e)
            => throw new NotImplementedException("Stub only!");

        public static (string prefix, string suffix, string fullName) GetHighwayRouteNamings(Colossal.Hash128 routeIndex) => throw new NotImplementedException("Stub only!");
    }
}