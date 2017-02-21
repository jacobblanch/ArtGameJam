using UnityEngine;
using uNature.Core.Utility;

namespace uNature.Core.FoliageClasses
{
    public class WindZones : BaseInteraction
    {

    /* DISABLED -> NEEDS TO BE REWRITTEN!

    [SerializeField]
    private int radius = 10;

    [SerializeField]
    private float strength = 0.7f;

    [SerializeField]
    private bool calculateY = true;

    protected override void UpdateInteraction(FoliageReceiver receiver, Vector2 normalizedPosition)
    {
        base.UpdateInteraction(receiver, normalizedPosition);

        if (FoliageManager.instance == null) return;

        int startX = (int)normalizedPosition.x - radius;
        int startZ = (int)normalizedPosition.y - radius;

        int endX = (int)normalizedPosition.x + radius;
        int endZ = (int)normalizedPosition.y + radius;

        int mapWidth = receiver.interactionMap.mapWidth;

        int worldX = (int)receiver.latestChunk.position.x;
        int worldZ = (int)receiver.latestChunk.position.y;

        Vector3 worldPosition;

        int interpolatedX;
        int interpolatedZ;

        FoliageManager manager = FoliageManager.instance;
        var worldMap = manager.worldMap;
        var worldMapPixels = worldMap.mapPixels;
        var worldMapWidth = worldMap.mapWidth;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startZ; y < endZ; y++)
            {
                if (x < 0 || y < 0) continue;

                worldPosition.x = x + worldX;
                worldPosition.y = 10000;
                worldPosition.z = y + worldZ;

                if (calculateY)
                {
                    interpolatedX = manager.TransformCord(worldPosition.x, false);
                    interpolatedZ = manager.TransformCord(worldPosition.z, false);

                    worldPosition.y = worldMap.GetHeight(worldMapPixels[interpolatedX + interpolatedZ * worldMapWidth]);
                }
                else
                {
                    worldPosition.y = transform.position.y; // ignore Y
                }

                receiver.interactionMap.mapPixels[x + y * mapWidth].r = (byte)(strength / Vector3.Distance(worldPosition, transform.position) * 255);
            }
        }

    }
    */

    }
}
