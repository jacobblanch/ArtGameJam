using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UniLinq;

using uNature.Core.Sectors;
using uNature.Core.Utility;
using uNature.Core.Threading;

namespace uNature.Core.FoliageClasses
{
    [ExecuteInEditMode]
    public class FoliageCore_MainManager : FoliageMeshManager
    {
        public const int FOLIAGE_MAIN_AREA_RADIUS = 10240; // -10240 -> 10240 (x & y) [20,480 * 20,000 = 419,430 units] 
        internal const int FOLIAGE_MAIN_AREA_RESOLUTION = 40; // 40 res -> 10240 * 2 = 20480 / 40 = 512. 

        public const int FOLIAGE_INSTANCE_AREA_SIZE = (FOLIAGE_MAIN_AREA_RADIUS * 2) / FOLIAGE_MAIN_AREA_RESOLUTION;
        internal const int FOLIAGE_INSTANCE_AREA_BOUNDS = FOLIAGE_INSTANCE_AREA_SIZE * FOLIAGE_INSTANCE_AREA_SIZE; // 512 * 512

        #region Static Values
        internal static Vector3 FOLIAGE_MAIN_AREA_SECTOR_SIZE = new Vector3(FOLIAGE_MAIN_AREA_RADIUS * 2, 0, FOLIAGE_MAIN_AREA_RADIUS * 2);
        internal static Vector3 FOLIAGE_MAIN_AREA_BOUNDS_MIN = new Vector3(0, 0, 0);
        internal static Vector3 FOLIAGE_MAIN_AREA_BOUNDS_MAX = new Vector3(FOLIAGE_MAIN_AREA_RADIUS, FOLIAGE_MAIN_AREA_RADIUS, FOLIAGE_MAIN_AREA_RADIUS);
        internal static Bounds FOLIAGE_MAIN_AREA_BOUNDS = new Bounds(FOLIAGE_MAIN_AREA_BOUNDS_MIN, FOLIAGE_MAIN_AREA_BOUNDS_MAX);

        internal static Vector3 FOLIAGE_INSTANCE_AREA_BOUNDS_MIN = new Vector3(0, 0, 0);
        internal static Vector3 FOLIAGE_INSTANCE_AREA_BOUNDS_MAX = new Vector3(FOLIAGE_INSTANCE_AREA_SIZE, FOLIAGE_INSTANCE_AREA_SIZE, FOLIAGE_INSTANCE_AREA_SIZE);

        /// <summary>
        /// Check if certain world cords are out of bounds.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        internal static bool CheckCordsOutOfBounds(float x, float z, float sizeX, float sizeZ)
        {
            bool outOfBounds = x < -FOLIAGE_MAIN_AREA_RADIUS || z < -FOLIAGE_MAIN_AREA_RADIUS
                || (x + sizeX) > FOLIAGE_MAIN_AREA_RADIUS || (z + sizeZ) > FOLIAGE_MAIN_AREA_RADIUS;

            return outOfBounds;
        }

        internal static List<UNMap> FOLIAGE_MAPS_WAITING_FOR_SAVE = new List<UNMap>();

        private static FoliageCore_MainManager _instance;
        public static FoliageCore_MainManager instance
        {
            get
            {
                return _instance;
            }
        }
        #endregion

        #region Global Values
        [SerializeField]
        private string _guid = null;
        public string guid
        {
            get
            {
                if (_guid == null)
                {
                    _guid = System.Guid.NewGuid().ToString();

                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    #endif
                }

                return _guid;
            }
        }

        [SerializeField]
        private bool _enabled = true;
        public new bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        [SerializeField]
        float _density = 1;
        public float density
        {
            get
            {
                return _density;
            }
            set
            {
                value = Mathf.Clamp(value, 0, 1);

                if (_density != value)
                {
                    _density = value;

                    FoliageDB.instance.UpdateShaderGeneralSettings();
                }
            }
        }

        [SerializeField]
        private int _FoliageGenerationLayerMask = 1;
        public int FoliageGenerationLayerMask
        {
            get
            {
                return _FoliageGenerationLayerMask;
            }
            set
            {
                _FoliageGenerationLayerMask = value;
            }
        }

        public bool useQualitySettingsShadowDistance = false;
        public float foliageShadowDistance = 100;
        #endregion

        #region Sectors
        [SerializeField]
        private FoliageCore_Sector _sector = null;
        public FoliageCore_Sector sector
        {
            get
            {
                if (_sector == null)
                {
                    _sector = Sector.GenerateSector<FoliageCore_Sector, FoliageCore_Chunk>(transform, FOLIAGE_MAIN_AREA_SECTOR_SIZE, _sector, FOLIAGE_MAIN_AREA_RESOLUTION);
                }

                return _sector;
            }
        }

        [SerializeField]
        private int _instancesSectorResolution = 5;
        /// <summary>
        /// The sector resolution that the manager's sector will have. [default : 10]
        /// </summary>
        public int instancesSectorResolution
        {
            get
            {
                return _instancesSectorResolution;
            }
            set
            {
                if (_instancesSectorResolution != value)
                {
                    _instancesSectorResolution = value;

                    GenerateFoliageMeshInstances();

                    FoliageCore_Chunk mChunk;
                    FoliageManagerInstance mInstance;

                    for (int i = 0; i < sector.foliageChunks.Count; i++)
                    {
                        mChunk = sector.foliageChunks[i];

                        if(mChunk.isFoliageInstanceAttached)
                        {
                            mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                            DestroyImmediate(mInstance.sector.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the chunk size of the manager instances.
        /// </summary>
        public float instancesSectorChunkSize
        {
            get
            {
                return (float)FOLIAGE_INSTANCE_AREA_SIZE / instancesSectorResolution;
            }
        }

        /// <summary>
        /// Get chunk from bounds.
        /// 
        /// [REMOVE MAIN MANAGER POSITION FROM CORDS!!]
        /// for example:
        /// cordX = transform.position.x - FoliageCore_MainManager.instance.transform.position.x.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public int GetChunkID(float x, float z)
        {
            return Mathf.FloorToInt(x / FOLIAGE_INSTANCE_AREA_SIZE) + Mathf.FloorToInt(z / FOLIAGE_INSTANCE_AREA_SIZE) * FOLIAGE_MAIN_AREA_RESOLUTION;
        }

        /// <summary>
        /// Check if the chunk id is in range
        /// </summary>
        /// <param name="chunkID"></param>
        /// <returns></returns>
        public bool CheckChunkInBounds(int chunkID)
        {
            return chunkID >= 0 && chunkID < (FOLIAGE_MAIN_AREA_RESOLUTION * FOLIAGE_MAIN_AREA_RESOLUTION); 
        }
        #endregion

        #region Constructors
        protected static void CreateInstance()
        {
            GameObject gameObject = new GameObject("Foliage Static Manager");
            gameObject.AddComponent<FoliageCore_MainManager>();
        }

        protected override void Awake()
        {
            base.Awake();

            transform.position = new Vector3(-FOLIAGE_MAIN_AREA_RADIUS, 0, -FOLIAGE_MAIN_AREA_RADIUS); // put this at the negative radius to create a balance.
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _instance = this;

            if (sector == null) { } // call this to force the sector to be created if not available.

            FoliagePrototype.OnFoliageEnabledStateChangedEvent += OnFoliagePrototypeChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            FoliagePrototype.OnFoliageEnabledStateChangedEvent -= OnFoliagePrototypeChanged;
        }

        /// <summary>
        /// Called when the enable state of a prototype is changed
        /// </summary>
        /// <param name="changedPrototype"></param>
        /// <param name="value"></param>
        private void OnFoliagePrototypeChanged(FoliagePrototype changedPrototype, bool value)
        {
            if (value)
            {
                foreach (var meshInstances in prototypeMeshInstances)
                {
                    GenerateFoliageMeshInstanceForIndex(changedPrototype.id, meshInstances.Key);
                }
            }
            else
            {
                DestroyMeshInstance(changedPrototype.id);
            }
        }
        #endregion

        #region Terrain -> uNature
        /// <summary>
        /// Copy the terrain's details and use it with the custom Foliage system.
        /// </summary>
        /// <param name="terrain"></param>
        public void InsertFoliageFromTerrain(Terrain terrain)
        {
            UNStandaloneUtility.AddPrototypesIfDontExist(terrain.terrainData.detailPrototypes);

            List<int[,]> detailData = UNStandaloneUtility.GetTerrainDetails(terrain.terrainData);

            int terrainDetailWidth = terrain.terrainData.detailWidth;
            int terrainDetailHeight = terrain.terrainData.detailHeight;

            var detailPrototypes = terrain.terrainData.detailPrototypes;
            var uNaturePrototypes = FoliageDB.sortedPrototypes;

            float posX;
            float posZ;

            int chunkIndex;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance managerInstance;

            int prototype;

            float interpolatedX;
            float interpolatedZ;

            float worldPosX;
            float worldPosZ;

            FoliageGrassMap grassMap;

            for (int x = 0; x < terrainDetailWidth; x++)
            {
                for (int z = 0; z < terrainDetailHeight; z++)
                {
                    worldPosX = ((x * terrain.terrainData.size.x) / terrainDetailWidth) + terrain.transform.position.x;
                    worldPosZ = ((z * terrain.terrainData.size.z) / terrainDetailHeight) + terrain.transform.position.z;

                    posX = worldPosX - transform.position.x;
                    posZ = worldPosZ - transform.position.z; 

                    chunkIndex = GetChunkID(posX, posZ);

                    if (CheckChunkInBounds(chunkIndex)) // If isn't out of bounds
                    {
                        chunk = sector.foliageChunks[chunkIndex];

                        managerInstance = chunk.GetOrCreateFoliageManagerInstance();

                        interpolatedX = managerInstance.TransformCord(worldPosX, managerInstance.transform.position.x);
                        interpolatedZ = managerInstance.TransformCord(worldPosZ, managerInstance.transform.position.z);

                        for (int prototypeIndex = 0; prototypeIndex < detailData.Count; prototypeIndex++)
                        {
                            prototype = UNStandaloneUtility.TryGetPrototypeIndex(detailPrototypes[prototypeIndex]);

                            grassMap = managerInstance.grassMaps[uNaturePrototypes[prototype]];

                            grassMap.mapPixels[(int)interpolatedX + (int)interpolatedZ * grassMap.mapWidth].b = (byte)detailData[prototypeIndex][z, x];

                            // reset details on the terrain.
                            detailData[prototypeIndex][z, x] = 0;

                            grassMap.SetPixels32Delayed();
                            grassMap.dirty = true;
                        }
                    }
                }
            }

            for(int i = 0; i < detailData.Count; i++)
            {
                terrain.terrainData.SetDetailLayer(0, 0, i, detailData[i]);
            }

            SaveDelayedMaps(); //apply delayed maps.
            FoliageGrassMap.SaveAllMaps();

            CallInstancesChunksUpdate();

            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        #endregion

        #region Foliage Methods

        public void UpdateHeights(int x, int z, int scaleX, int scaleZ)
        {
            int mChunkID;
            FoliageCore_Chunk mChunk;
            FoliageManagerInstance mInstance;

            int interpolatedX;
            int interpolatedZ;

            int interpolatedSize;

            for (int xIndex = 0; xIndex < x + scaleX; xIndex++)
            {
                for (int zIndex = 0; zIndex < z + scaleZ; zIndex++)
                {
                    mChunkID = GetChunkID(xIndex - transform.position.x, zIndex - transform.position.x);

                    mChunk = sector.foliageChunks[mChunkID];

                    if (!mChunk.isFoliageInstanceAttached) continue;

                    mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                    interpolatedX = mInstance.TransformCord(xIndex, 0);
                    interpolatedZ = mInstance.TransformCord(zIndex, 0);

                    interpolatedSize = mInstance.TransformCord(1, 0);

                    if (interpolatedSize == 0)
                        interpolatedSize = 1;

                    mInstance.worldMap.UpdateHeight_RANGE(interpolatedX, interpolatedZ, interpolatedSize, interpolatedSize, false);
                    mInstance.worldMap.SetPixels32Delayed();
                }
            }
        }

        /// <summary>
        /// Set detail layer in world cords
        /// </summary>
        /// <param name="baseX">WORLD CORDS!!</param>
        /// <param name="baseZ">WORLD CORDS!!</param>
        /// <param name="sizeX">WORLD CORDS!!</param>
        /// <param name="sizeZ">WORLD CORDS!!</param>
        /// <param name="prototypeIndex">prototype.id</param>
        public byte[,] GetDetailLayer(int baseX, int baseZ, int sizeX, int sizeZ, int prototypeIndex)
        {
            if (sector == null) return null;

            byte[,] densities = new byte[sizeX, sizeZ];

            Vector3 pos = transform.position;

            int startX = baseX;
            int startZ = baseZ;

            int endX = startX + sizeX;
            int endZ = startZ + sizeZ;

            int interpolatedX;
            int interpolatedZ;

            int mChunkID;
            FoliageCore_Chunk mChunk;
            FoliageManagerInstance mInstance;

            FoliagePrototype prototype;
            FoliageGrassMap grassMap;

            for (int x = startX; x < endX; x++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    mChunkID = GetChunkID(x - pos.x, z - pos.z);

                    if (!CheckChunkInBounds(mChunkID)) continue; // if position is out of bounds continue to the next position [very rare]

                    mChunk = _sector.foliageChunks[mChunkID];

                    if (!mChunk.isFoliageInstanceAttached) continue; // if this chunk doesnt have an manager instance attached then there's no need to modify it.

                    mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                    interpolatedX = mInstance.TransformCord(x, mInstance.transform.position.x);
                    interpolatedZ = mInstance.TransformCord(z, mInstance.transform.position.z);

                    prototype = FoliageDB.sortedPrototypes[prototypeIndex];

                    grassMap = mInstance.grassMaps[prototype];

                    densities[x - startX, z - startZ] = grassMap.mapPixels[interpolatedX + interpolatedZ * grassMap.mapWidth].b;
                }
            }

            return densities;
        }

        /// <summary>
        /// Set detail layer in world cords
        /// </summary>
        /// <param name="baseX">WORLD CORDS!!</param>
        /// <param name="baseZ">WORLD CORDS!!</param>
        /// <param name="sizeX">WORLD CORDS!!</param>
        /// <param name="sizeZ">WORLD CORDS!!</param>
        /// <param name="prototypeIndex">prototype.id</param>
        /// <param name="densities">the density in bytes from 0 -> 15</param>
        public void SetDetailLayer(int baseX, int baseZ, int sizeX, int sizeZ, int prototypeIndex, byte[,] densities)
        {
            if (sector == null) return;

            if(densities.GetLength(0) != sizeX || densities.GetLength(1) != sizeZ)
            {
                Debug.LogError("uNature: Densities out of range!!");

                return;
            }

            Vector3 pos = transform.position;

            int startX = baseX;
            int startZ = baseZ;

            int endX = startX + sizeX;
            int endZ = startZ + sizeZ;

            int interpolatedX;
            int interpolatedZ;

            int mChunkID;
            FoliageCore_Chunk mChunk;
            FoliageManagerInstance mInstance;

            FoliagePrototype prototype;
            FoliageGrassMap grassMap;

            for (int x = startX; x < endX; x++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    mChunkID = GetChunkID(x - pos.x, z - pos.z);

                    if (!CheckChunkInBounds(mChunkID)) continue; // if position is out of bounds continue to the next position [very rare]

                    mChunk = _sector.foliageChunks[mChunkID];

                    if (!mChunk.isFoliageInstanceAttached) continue; // if this chunk doesnt have an manager instance attached then there's no need to modify it.

                    mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                    interpolatedX = mInstance.TransformCord(x, mInstance.transform.position.x);
                    interpolatedZ = mInstance.TransformCord(z, mInstance.transform.position.z);

                    prototype = FoliageDB.sortedPrototypes[prototypeIndex];

                    grassMap = mInstance.grassMaps[prototype];

                    grassMap.mapPixels[interpolatedX + interpolatedZ * grassMap.mapWidth].b = densities[x - startX, z - startZ];
                    grassMap.SetPixels32Delayed();
                    grassMap.dirty = true;
                }
            }

            FoliageCore_MainManager.SaveDelayedMaps();
        }

        #endregion

        #region Statics
        internal static Dictionary<int, GPUMesh> GetPrototypeMeshInstances(FoliageResolutions resolution)
        {
            if (!prototypeMeshInstances.ContainsKey(resolution))
            {
                GenerateFoliageMeshInstances(resolution);
            }

            return prototypeMeshInstances[resolution];
        }

        /// <summary>
        /// Save maps that have been marked as delayed (waiting for update)
        /// </summary>
        public static void SaveDelayedMaps()
        {
            for(int i = 0; i < FOLIAGE_MAPS_WAITING_FOR_SAVE.Count; i++)
            {
                FOLIAGE_MAPS_WAITING_FOR_SAVE[i].ApplySetPixelsDelayed();
            }
            FOLIAGE_MAPS_WAITING_FOR_SAVE.Clear();
        }

        /// <summary>
        /// Reset the foliage chunks
        /// </summary>
        public static void CallInstancesChunksUpdate()
        {
            if (instance == null) return;

            var chunks = instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance mInstance;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (!chunk.isFoliageInstanceAttached) continue; // if no manager instance attached then there's nothing to remove!.

                mInstance = chunk.GetOrCreateFoliageManagerInstance();

                mInstance.sector.ResetChunks();
            }
        }

        /// <summary>
        /// Remove Grass Map Globally
        /// </summary>
        /// <param name="id"></param>
        public static void RemoveGrassMap(FoliagePrototype prototype)
        {
            if (instance == null) return;

            var chunks = instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance mInstance;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (!chunk.isFoliageInstanceAttached) continue; // if no manager instance attached then there's nothing to remove!.

                mInstance = chunk.GetOrCreateFoliageManagerInstance();

                mInstance.RemoveGrassMap(prototype);
            }
        }

        /// <summary>
        /// Update the existing grass maps
        /// </summary>
        /// <param name="prototype"></param>
        public static void UpdateGrassMap()
        {
            if (instance == null) return;

            var chunks = instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance mInstance;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (!chunk.isFoliageInstanceAttached) continue; // if no manager instance attached then there's nothing to remove!.

                mInstance = chunk.GetOrCreateFoliageManagerInstance();

                mInstance.UpdateGrassMapsForMaterials();
            }
        }

        /// <summary>
        /// Reset the existing grass maps
        /// </summary>
        /// <param name="prototype"></param>
        public static void ResetGrassMap(List<FoliagePrototype> prototypes)
        {
            if (instance == null) return;

            var chunks = instance.sector.foliageChunks;
            FoliageCore_Chunk chunk;

            FoliageManagerInstance mInstance;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunk = chunks[i];

                if (!chunk.isFoliageInstanceAttached) continue; // if no manager instance attached then there's nothing to remove!.

                mInstance = chunk.GetOrCreateFoliageManagerInstance();

                for (int b = 0; b < prototypes.Count; b++)
                {
                    mInstance.grassMaps[prototypes[b]].ResetDensity();
                }
            }
        }

        /// <summary>
        /// Create an instance if not created
        /// </summary>
        public static void InitializeAndCreateIfNotFound()
        {
            if (instance != null) return;

            GameObject instanceGO = new GameObject("Foliage Main Manager");
            instanceGO.AddComponent<FoliageCore_MainManager>();
        }
        #endregion
    }

    public enum FoliageResolutions
    {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048
    }
}
