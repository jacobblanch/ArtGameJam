#if UN_MapMagic
using UnityEngine;
using System.Collections;

using MapMagic;
using uNature.Core.FoliageClasses;
using uNature.Core.Terrains;

namespace uNature.Core.Extensions.MapMagicIntegration
{
    /// <summary>
    /// An manager class for the map magic integration, attach this script once in each one of your map magic integrated scenes.
    /// </summary>
    public class UNMapMagic_Manager : MonoBehaviour
    {
        FoliageCore_MainManager _manager;
        FoliageCore_MainManager manager
        {
            get
            {
                if(_manager == null)
                {
                    _manager = FoliageCore_MainManager.instance;
                }

                return _manager;
            }
        }

        protected virtual void OnEnable()
        {
            MapMagic.MapMagic.OnGenerateCompleted += MapMagic_OnGenerateCompleted;
        }

        protected virtual void OnDisable()
        {
            MapMagic.MapMagic.OnGenerateCompleted -= MapMagic_OnGenerateCompleted;
        }

        private void MapMagic_OnGenerateCompleted(Terrain terrain)
        {
            if (manager == null) return;

            manager.InsertFoliageFromTerrain(terrain);

            //manager.worldMap.UpdateHeight_RANGE((int)(terrain.transform.position.x - FoliageManager.instance.transform.position.x), (int)(terrain.transform.position.z - FoliageManager.instance.transform.position.z), (int)terrain.terrainData.size.x, (int)terrain.terrainData.size.z);
        }
    }
}
#endif