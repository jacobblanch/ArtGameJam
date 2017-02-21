using UnityEngine;
using uNature.Core.Sectors;

using System.Collections.Generic;

namespace uNature.Core.FoliageClasses
{
    public class FoliageChunk : Chunk
    {
        private Dictionary<int, ReadDensityInformation> _maxDensities = null;
        private Dictionary<int, ReadDensityInformation> maxDensities
        {
            get
            {
                if(_maxDensities == null)
                {
                    _maxDensities = new Dictionary<int, ReadDensityInformation>();
                    
                    for(int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        _maxDensities.Add(FoliageDB.unSortedPrototypes[i].id, new ReadDensityInformation());
                    }
                }

                return _maxDensities;
            }
        }

        [System.NonSerialized]
        FoliageManagerInstance _manager;
        FoliageManagerInstance manager
        {
            get
            {
                if(_manager == null)
                {
                    _manager = GetComponentInParent<FoliageManagerInstance>();
                }

                return _manager;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        /// <summary>
        /// Reset population.
        /// </summary>
        public override void ResetChunk()
        {
            base.ResetChunk();

            for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                maxDensities[FoliageDB.unSortedPrototypes[i].id].isDirty = true;
            }
        }

        /// <summary>
        /// Called when the size of the chunk is changed.
        /// </summary>
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
        }

        /// <summary>
        /// Create colliders
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
        }

        /// <summary>
        /// Get Density Information
        /// </summary>
        /// <returns></returns>
        public ReadDensityInformation GetDensity(int prototypeIndex)
        {
            return maxDensities[prototypeIndex];
        }

        /// <summary>
        /// Get max density on area
        /// </summary>
        /// <returns></returns>
        public byte GetMaxDensityOnArea(int prototypeIndex)
        {
            ReadDensityInformation densityInformation = GetDensity(prototypeIndex);

            if (densityInformation == null) return 0;

            if (densityInformation.isDirty)
            {
                var grassMap = manager.grassMaps[FoliageDB.sortedPrototypes[prototypeIndex]];

                densityInformation.maxDensity = 0;
                byte currentDensity = 0;

                int posX = manager.TransformCord(position.x, 0);
                int posZ = manager.TransformCord(position.y, 0);

                int sizeX = manager.TransformCord(size.x, 0);
                int sizeZ = manager.TransformCord(size.y, 0);

                var mapPixels = grassMap.mapPixels;
                var mapWidth = grassMap.mapWidth;

                for (int x = posX; x < posX + sizeX; x++)
                {
                    for (int z = posZ; z < posZ + sizeZ; z++)
                    {
                        currentDensity = mapPixels[x + z * mapWidth].b;

                        if (currentDensity > densityInformation.maxDensity)
                        {
                            densityInformation.maxDensity = currentDensity;
                        }
                    }
                }

                densityInformation.isDirty = false;
            }

            return densityInformation.maxDensity;
        }

        /// <summary>
        /// Called when a new prototype has been created -> called from FoliageDB
        /// </summary>
        /// <param name="id"></param>
        internal static void OnPrototypeCreated(int id)
        {
            if (FoliageCore_MainManager.instance == null) return;

            var chunks = FoliageCore_MainManager.instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;
            ReadDensityInformation densityInformation;

            FoliageManagerInstance mInstance;
            FoliageChunk mChunk;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (chunk.isFoliageInstanceAttached)
                {
                    mInstance = chunk.GetOrCreateFoliageManagerInstance();

                    for (int b = 0; b < mInstance.sector.FoliageChunks.Count; b++)
                    {
                        mChunk = mInstance.sector.FoliageChunks[b];

                        if (mChunk.maxDensities == null) return;

                        densityInformation = new ReadDensityInformation();

                        if (!mChunk.maxDensities.ContainsKey(id))
                        {
                            mChunk.maxDensities.Add(id, densityInformation);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a prototype has been deleted -> called from FoliageDB
        /// </summary>
        /// <param name="id"></param>
        internal static void OnPrototypeDeleted(int id)
        {
            if (FoliageCore_MainManager.instance == null) return;

            var chunks = FoliageCore_MainManager.instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance mInstance;
            FoliageChunk mChunk;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (chunk.isFoliageInstanceAttached)
                {
                    mInstance = chunk.GetOrCreateFoliageManagerInstance();

                    for (int b = 0; b < mInstance.sector.FoliageChunks.Count; b++)
                    {
                        mChunk = mInstance.sector.FoliageChunks[b];

                        if(mChunk.maxDensities.ContainsKey(id))
                        {
                            mChunk.maxDensities.Remove(id);
                        }
                    }
                }
            }
        }
    }

    public class ReadDensityInformation
    {
        public byte maxDensity;
        public bool isDirty;

        public ReadDensityInformation()
        {
            maxDensity  = 0;
            isDirty     = true;
        }
    }
}
