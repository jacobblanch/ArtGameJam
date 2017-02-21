using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UniLinq;

using uNature.Core.FoliageClasses;
using uNature.Core.Settings;

namespace uNature.Core.Utility
{
    public static class UNMapGenerators
    {
        public static Texture2D GenerateColorMap(float x, float z, int size, string guid)
        {
            bool brushProjectorEnabled = UNBrushUtility.projector.enabled;

            UNBrushUtility.projector.enabled = false;

            #if UNITY_EDITOR
            UNEditorUtility.StartSceneScrollbar("Calculating Color Map", 1);
            #endif

            GameObject go = new GameObject("Temp_uNature_ColorMap_Generator");
            Camera camera = go.AddComponent<Camera>();

            float shadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = 0;

            string path = "";

            List<Terrain> dirtyTerrains = new List<Terrain>();
            List<Terrain.MaterialType> materialTypes = new List<Terrain.MaterialType>();

            var terrains = GameObject.FindObjectsOfType<Terrain>();
            Terrain terrain;

            for (int i = 0; i < terrains.Length; i++)
            {
                terrain = terrains[i];

                materialTypes.Add(terrain.materialType);
                terrain.materialType = Terrain.MaterialType.BuiltInLegacyDiffuse;

                if (terrain.drawTreesAndFoliage)
                {
                    terrain.drawTreesAndFoliage = false;
                    dirtyTerrains.Add(terrain);
                }
            }

            try
            {
                go.transform.position = new Vector3(x + size / 2, size, z + size / 2);

                camera.aspect = 1;
                camera.farClipPlane = size + 500;
                camera.fieldOfView = 53;

                go.transform.LookAt(new Vector3(x + size / 2, 0, z + size / 2));

                RenderTexture rt = new RenderTexture(size, size, 24);
                camera.targetTexture = rt;
                Texture2D screenShot = new Texture2D(size, size, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                camera.targetTexture = null;
                RenderTexture.active = null; // JC: added to avoid errors

                GameObject.DestroyImmediate(rt);
                GameObject.DestroyImmediate(camera);

                byte[] bytes = screenShot.EncodeToPNG();

                GameObject.DestroyImmediate(screenShot);

                path = "ColorMaps/" + string.Format("ColorMap_{0}_{1}", guid, size);

                CreateAndSaveTexture(path, bytes);

                GameObject.DestroyImmediate(go);

                #if UNITY_EDITOR
                UNEditorUtility.currentProgressIndex = 1;
                #endif
            }
            catch
            {
                QualitySettings.shadowDistance = shadowDistance;

                UNBrushUtility.projector.enabled = brushProjectorEnabled; // restore brush settings

                for (int i = 0; i < materialTypes.Count; i++)
                {
                    terrains[i].materialType = materialTypes[i];
                }

                for (int i = 0; i < dirtyTerrains.Count; i++)
                {
                    dirtyTerrains[i].drawTreesAndFoliage = true;
                }

                return null;
            }

            QualitySettings.shadowDistance = shadowDistance; // restore shadows
            UNBrushUtility.projector.enabled = brushProjectorEnabled; // restore brush settings

            for (int i = 0; i < materialTypes.Count; i++)
            {
                terrains[i].materialType = materialTypes[i];
            }

            for (int i = 0; i < dirtyTerrains.Count; i++)
            {
                dirtyTerrains[i].drawTreesAndFoliage = true;
            }

            return path == "" ? null : Resources.Load<Texture2D>(path);
        }

        /// <summary>
        /// This will generate the texture itself instead of the whole FoliageGrassMap like the "CreateGrassMap" method.
        /// </summary>
        /// <param name="prototypeIndex"></param>
        /// <param name="mInstance"></param>
        /// <returns></returns>
        public static Texture2D GenerateGrassMap(int prototypeIndex, FoliageManagerInstance mInstance)
        {
            int size = mInstance.foliageAreaResolutionIntegral;

            Texture2D map;

            string path;

            try
            {
                map = new Texture2D(size, size, TextureFormat.ARGB32, false, true);
                Color32[] colors = new Color32[size * size];

                map.SetPixels32(colors); // set pixels to black so it wont be a white texture.
                map.filterMode = FilterMode.Point;

                path = GetGrassMapPath(prototypeIndex, mInstance);

                if (!Application.isPlaying)
                {
                    CreateAndSaveTexture(path, map);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(mInstance);

                #if UNITY_5_3_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                #else
                UnityEditor.EditorApplication.MarkSceneDirty();
                #endif
            }
            #endif

            return map;
        }

        public static FoliageGrassMap CreateGrassMap(int prototypeIndex, FoliageManagerInstance mInstance)
        {
            FoliagePrototype prototype = FoliageDB.sortedPrototypes[prototypeIndex];

            FoliageGrassMap grassMap = new FoliageGrassMap(GenerateGrassMap(prototypeIndex, mInstance), prototype, mInstance);
            grassMap.UpdateMap();

            return grassMap;
        }

        public static void UpdateGrassMapPerlin(FoliageGrassMap grassMap)
        {
            int size = grassMap.mapWidth;

            for (int xIndex = 0; xIndex < size; xIndex++)
            {
                for (int zIndex = 0; zIndex < size; zIndex++)
                {
                    grassMap.mapPixels[xIndex + zIndex * size].a = (byte)(Mathf.PerlinNoise(xIndex / (size * grassMap.perlinScale), zIndex / (size * grassMap.perlinScale)) * 255);
                }
            }

            grassMap.SetPixels32();
        }

        public static void SaveGrassMaps(FoliageManagerInstance mInstance)
        {
            FoliagePrototype prototype;
            for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                prototype = FoliageDB.unSortedPrototypes[i];

                if (mInstance.grassMaps.ContainsKey(prototype))
                {
                    SaveMap(mInstance.grassMaps[prototype]);
                }
            }
        }

        public static void SaveMap(UNMap mapData)
        {
            #if UNITY_EDITOR
            if (mapData.map == null) return;

            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        public static string GetGrassMapPath(int prototypeIndex, FoliageManagerInstance mInstance)
        {
            return "GrassMaps/" + string.Format("GrassMap_{0}_{1}_Prototype_{2}", mInstance.guid, mInstance.foliageAreaResolutionIntegral, prototypeIndex);
        }

        public static string GetWorldMapPath(int size, FoliageManagerInstance mInstance)
        {
            return "GrassMaps/" + string.Format("WorldMap_{0}_{1}", mInstance.guid, size);
        }

        public static FoliageWorldMap GenerateWorldMap(FoliageManagerInstance mInstance)
        {
            List<Terrain> dirtyTerrains = new List<Terrain>();

            var terrains = GameObject.FindObjectsOfType<Terrain>();
            Terrain terrain;

            for (int i = 0; i < terrains.Length; i++)
            {
                terrain = terrains[i];

                if (terrain.drawTreesAndFoliage)
                {
                    terrain.drawTreesAndFoliage = false;
                    dirtyTerrains.Add(terrain);
                }
            }

            int mapResolution = mInstance.foliageAreaResolutionIntegral;

            string path = GetWorldMapPath(mapResolution, mInstance);

            FoliageWorldMap worldMap;

            try
            {
                Texture2D map = new Texture2D(mapResolution, mapResolution, TextureFormat.ARGB32, false, true);
                Color32[] colors = new Color32[mapResolution * mapResolution];

                map.filterMode = FilterMode.Point;

                map.SetPixels32(colors); // set pixels to black so it wont be a white texture.

                if (!Application.isPlaying)
                {
                    /*
                    byte[] bytes = map.EncodeToPNG();

                    GameObject.DestroyImmediate(map);

                    CreateAndSaveTexture(path, bytes);

                    map = Resources.Load<Texture2D>(path);
                    */

                    CreateAndSaveTexture(path, map);
                }

                worldMap = new FoliageWorldMap(map, mInstance);
                worldMap.UpdateHeight_RANGE(0, 0, mapResolution, mapResolution, true);

                worldMap.Save();
            }
            catch (System.Exception ex)
            {
                for (int i = 0; i < dirtyTerrains.Count; i++)
                {
                    terrain = dirtyTerrains[i];

                    terrain.drawTreesAndFoliage = true;
                }

                throw ex;
            }

            for (int i = 0; i < dirtyTerrains.Count; i++)
            {
                terrain = dirtyTerrains[i];

                terrain.drawTreesAndFoliage = true;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(mInstance);

                #if UNITY_5_3_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                #else
                UnityEditor.EditorApplication.MarkSceneDirty();
                #endif
            }
            #endif

            return worldMap;
        }

        public static Vector2 TransformNormals(Vector3 normal)
        {
            return new Vector3(ClampNegativeIntoPositive(normal.y), ClampNegativeIntoPositive(normal.z));
        }

        /// <summary>
        /// Get a hit for the maps (included with the map mask which is specified on the Foliage manager)
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public static bool GetMapHit(Ray ray, out RaycastHit hit, float maxDistance)
        {
            var hits = Physics.RaycastAll(ray, maxDistance, FoliageCore_MainManager.instance.FoliageGenerationLayerMask);

            System.Array.Sort<RaycastHit>(hits, delegate(RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            });

            bool lengthBiggerThan0 = hits.Length > 0;

            hit = lengthBiggerThan0 ? hits[0] : new RaycastHit();

            return lengthBiggerThan0;
        }

        /// <summary>
        /// Get a hit for the maps (included with the map mask which is specified on the Foliage manager)
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public static bool GetMapHit(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance)
        {
            return GetMapHit(new Ray(origin, direction), out hit, maxDistance);
        }

        /// <summary>
        /// Create and save a map texture.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        public static void CreateAndSaveTexture(string path, byte[] bytes)
        {
            #if UNITY_EDITOR
            System.IO.File.WriteAllBytes(UNSettings.ProjectPath + "Resources/" + path + ".png", bytes);

            if (!Application.isPlaying)
            {
                UnityEditor.AssetDatabase.Refresh();
            }

            var textureImporter = UnityEditor.AssetImporter.GetAtPath(UNSettings.ProjectPath + "Resources/" + path + ".png") as UnityEditor.TextureImporter;
            textureImporter.textureType = UnityEditor.TextureImporterType.Default;
            textureImporter.textureFormat = UnityEditor.TextureImporterFormat.AutomaticTruecolor;
            textureImporter.linearTexture = true;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.npotScale = UnityEditor.TextureImporterNPOTScale.None;
            textureImporter.isReadable = true;
            textureImporter.maxTextureSize = 4096;

            textureImporter.SaveAndReimport();

            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        /// <summary>
        /// Create and save a map texture.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        public static void CreateAndSaveTexture(string path, Texture2D map)
        {
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(map, UNSettings.ProjectPath + "Resources/" + path + ".asset");
            UnityEditor.AssetDatabase.SaveAssets();

            //System.IO.File.WriteAllBytes(UNSettings.ProjectPath + "Resources/" + path + ".png", bytes);

            if (!Application.isPlaying)
            {
                UnityEditor.AssetDatabase.Refresh();
            }
            #endif
        }

        /// <summary>
        /// Convert point from negative to positive.
        /// 
        /// -1 = 0;
        /// 0 = 0.5;
        /// 1 = 1;
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float ClampNegativeIntoPositive(float value)
        {
            return (value + 1) / 2;
        }
    }

    /// <summary>
    /// The abstract Map class.
    /// </summary>
    public abstract class UNMap
    {
        [SerializeField]
        private Texture2D _map;
        public Texture2D map
        {
            get
            {
                return _map;
            }
            set
            {
                if (_map != value)
                {
                    _map = value;

                    _mapPixels = map == null ? null : map.GetPixels32();
                }
            }
        }

        [SerializeField]
        protected Color32[] _mapPixels;
        public Color32[] mapPixels
        {
            get
            {
                return _mapPixels;
            }
            internal set
            {
                _mapPixels = value;
            }
        }

        [System.NonSerialized]
        private Color32[] originalMapPixels = null; // used for restoring runtime changes.

        [SerializeField]
        private int _mapWidth;
        public int mapWidth
        {
            get
            {
                return _mapWidth;
            }
        }

        [System.NonSerialized]
        bool _dirty = false;
        public bool dirty
        {
            get
            {
                return _dirty;
            }
            set
            {
                if (_dirty != value && (!Application.isPlaying || UNSettings.instance.UN_Foliage_RUNTIME_SAVECHANGES))
                {
                    _dirty = value;

                    #if UNITY_EDITOR
                    if(!Application.isPlaying && map != null && value)
                    {
                        UnityEditor.EditorUtility.SetDirty(map);
                    }
                    #endif

                    OnDirty(value);
                }
            }
        }

        [SerializeField]
        bool _saveDelayed = false;
        private bool saveDelayed
        {
            get
            {
                return _saveDelayed;
            }
            set
            {
                if(_saveDelayed != value)
                {
                    _saveDelayed = value;

                    if(value && !FoliageCore_MainManager.FOLIAGE_MAPS_WAITING_FOR_SAVE.Contains(this))
                    {
                        FoliageCore_MainManager.FOLIAGE_MAPS_WAITING_FOR_SAVE.Add(this);
                    }
                }
            }
        }

        [SerializeField]
        FoliageManagerInstance _mInstance;
        public FoliageManagerInstance mInstance
        {
            get
            {
                return _mInstance;
            }
        }

        protected UNMap()
        {
        }

        protected UNMap(Texture2D texture, Color32[] pixels, FoliageManagerInstance mInstance)
        {
            _map = texture;
            _mInstance = mInstance;

            Apply(pixels);
        }

        public void RestoreChanges()
        {
            if(!UNSettings.instance.UN_Foliage_RUNTIME_SAVECHANGES && originalMapPixels != null)
            {
                SetPixels32(originalMapPixels);
            }
        }

        public void Apply(Color32[] pixels)
        {
            _mapPixels = pixels;
            _mapWidth = _map.width;

            map.Apply();

            #if UNITY_EDITOR
            if (!Application.isPlaying || UNSettings.instance.UN_Foliage_RUNTIME_SAVECHANGES)
            {
                UnityEditor.EditorUtility.SetDirty(map);
            }
            #endif
        }

        protected virtual void OnDirty(bool value)
        {

        }

        public void SetPixels32()
        {
            SetPixels32(mapPixels);
        }

        public void SetPixels32(Color32[] pixels)
        {
            SetPixels32(pixels, true);
        }

        public void SetPixelsNoApply()
        {
            SetPixels32(mapPixels, false);
        }

        public void SetPixels32Delayed()
        {
            saveDelayed = true;
        }

        internal void ApplySetPixelsDelayed()
        {
            if(saveDelayed)
            {
                saveDelayed = false;

                SetPixels32();
            }
        }

        private void SetPixels32(Color32[] pixels, bool apply)
        {
            if (map == null) return;

            if (originalMapPixels == null)
            {
                originalMapPixels = map.GetPixels32();
            }

            map.SetPixels32(pixels);

            if (apply)
            {
                Apply(pixels); // apply changes
            }
        }

        public byte[] EncodeToPNG()
        {
            if (map == null) return null;

            return map.EncodeToPNG();
        }

        public void Resize(int size)
        {
            map.Resize(size, size);
            _mapWidth = size;
            _mapPixels = map.GetPixels32();

            originalMapPixels = mapPixels;

            Clear(true, Color.black);
        }

        public void Clear(bool autoApply, Color32 defaultColor)
        {
            int mapWidth = this.mapWidth;
            Color32[] mapPixels = this.mapPixels;

            int length = mapWidth * mapWidth;

            for (int count = 0; count < length; count++)
            {
                mapPixels[count] = defaultColor;
            }

            this._mapPixels = mapPixels;

            if (autoApply)
            {
                SetPixels32();
            }
        }
    }

    /// <summary>
    /// Channels:
    ///
    /// R: Free
    /// G: Free
    /// B: Density
    /// A: Perlin Noise
    /// </summary>
    public class FoliageGrassMap : UNMap
    {
        [SerializeField]
        private float _perlinScale = 0.02f;
        public float perlinScale
        {
            get
            {
                return _perlinScale;
            }
            set
            {
                if (_perlinScale != value)
                {
                    _perlinScale = value;

                    UNMapGenerators.UpdateGrassMapPerlin(this);
                }
            }
        }

        [SerializeField]
        int _prototypeID;
        public int prototypeID
        {
            get
            {
                return _prototypeID;
            }
        }

        public FoliageGrassMap(Texture2D texture, FoliagePrototype prototype, FoliageManagerInstance mInstance) : this(texture, texture.GetPixels32(), prototype, mInstance)
        {
        }

        public FoliageGrassMap(Texture2D texture, Color32[] pixels, FoliagePrototype prototype, FoliageManagerInstance mInstance) : base(texture, pixels, mInstance)
        {
            _prototypeID = prototype.id;
        }

        public void UpdateMap()
        {
            if (FoliageDB.unSortedPrototypes.Count == 0 || mInstance == null) return;

            if (map == null)
            {
                mInstance.grassMaps.Remove(FoliageDB.sortedPrototypes[prototypeID]);
                return;
            }

            int size = FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE;

            int index;

            for (int xIndex = 0; xIndex < size; xIndex++)
            {
                for (int zIndex = 0; zIndex < size; zIndex++)
                {
                    index = xIndex + zIndex * size;

                    mapPixels[index].a = (byte)(Mathf.PerlinNoise(xIndex / (size * perlinScale), zIndex / (size * perlinScale)) * 255);
                }
            }

            SetPixels32();

            Save();
        }

        public void ResetDensity()
        {
            for (int xIndex = 0; xIndex < mapWidth; xIndex++)
            {
                for (int zIndex = 0; zIndex < mapWidth; zIndex++)
                {
                    mapPixels[xIndex + zIndex * mapWidth].b = 0;
                }
            }

            SetPixels32();
            Save();
        }

        /// <summary>
        /// This method will make sure that the map exists and if it doesnt it will create it.
        /// 
        /// Used mainly for the rendering so if the user accidently/ purposely removed the grass map it will automatically generate a new one so it wont affect the system.
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        public Texture2D SafeGetMap(FoliagePrototype prototype, FoliageManagerInstance instance)
        {
            if(map == null)
            {
                map = UNMapGenerators.GenerateGrassMap(prototype.id, instance);
                UpdateMap();
            }

            return map;
        }

        /// <summary>
        /// Destroy this current grass map.
        /// </summary>
        public void Dispose()
        {
            if (map == null) return;

            #if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(map);
            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        /// <summary>
        /// Get density at normalized x & z
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public byte GetDensity(int x, int z)
        {
            return mapPixels[x + z * mapWidth].b;
        }

        /// <summary>
        /// Set density at normalized x & z
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public void SetDensity(int x, int z, byte density)
        {
            mapPixels[x + z * mapWidth].b = density;
        }

        public void CallChunksUpdate()
        {
            for (int i = 0; i < mInstance.sector.FoliageChunks.Count; i++)
            {
                mInstance.sector.FoliageChunks[i].GetDensity(prototypeID).isDirty = true;
            }
        }

        public void Save()
        {
            if (dirty && (!Application.isPlaying || UNSettings.instance.UN_Foliage_RUNTIME_SAVECHANGES))
            {
                #if UNITY_EDITOR
                UNMapGenerators.SaveMap(this);
                #endif

                dirty = false;
            }
        }

        protected override void OnDirty(bool value)
        {
            base.OnDirty(value);

            CallChunksUpdate();
        }

        #region Static
        public static bool globalDirty
        {
            get
            {
                FoliageCore_MainManager mManager = FoliageCore_MainManager.instance;
                FoliageCore_Chunk mChunk;
                FoliageManagerInstance mInstance;

                for (int j = 0; j < mManager.sector.foliageChunks.Count; j++)
                {
                    mChunk = mManager.sector.foliageChunks[j];

                    if (!mChunk.isFoliageInstanceAttached) continue;

                    mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                    for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        if (mInstance.grassMaps[FoliageDB.unSortedPrototypes[i]].dirty) return true;
                    }
                }

                return false;
            }
        }

        public static void SaveAllMaps()
        {
            FoliageGrassMap grassMap;
            FoliageCore_MainManager mManager = FoliageCore_MainManager.instance;
            FoliageCore_Chunk mChunk;
            FoliageManagerInstance mInstance;

            for (int j = 0; j < mManager.sector.foliageChunks.Count; j++)
            {
                mChunk = mManager.sector.foliageChunks[j];

                if (!mChunk.isFoliageInstanceAttached) continue;

                mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                {
                    grassMap = mInstance.grassMaps[FoliageDB.unSortedPrototypes[i]];

                    grassMap.Save();
                }
            }
        }

        public static void ApplyAreaSizeChange(FoliageManagerInstance mInstance)
        {
            FoliageGrassMap grassMap;
            for(int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                grassMap = mInstance.grassMaps[FoliageDB.unSortedPrototypes[i]];

                grassMap.Resize(mInstance.foliageAreaResolutionIntegral);
            }
            
            UNSettings.Log("Grass maps updated succesfully after modifying resolution.");
        }

        /// <summary>
        /// Update all of the availble grass maps
        /// (pixels)
        /// </summary>
        /// <param name="size"></param>
        public static void UpdateGrassMaps(FoliageManagerInstance mInstance)
        {
            if (FoliageDB.unSortedPrototypes.Count == 0) return;

            for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                mInstance.grassMaps[FoliageDB.unSortedPrototypes[i]].UpdateMap();
            }
        }
        #endregion
    }

    /// <summary>
    /// Channels:
    /// R: Normals X-Axis
    /// G: Normals Y-Axis
    /// B: Heights Channel #1
    /// A: Heights Channel #2
    /// </summary>
    [System.Serializable]
    public class FoliageWorldMap : UNMap
    {
        public FoliageWorldMap(Texture2D texture, FoliageManagerInstance mInstance) : base(texture, texture.GetPixels32(), mInstance)
        {
        }

        /// <summary>
        /// Transform height from normalized cords to world cords
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public float GetHeight(Color32 pixel)
        {
            return (((pixel.b * 256f) + pixel.a) / 65535f) * mInstance.foliageAreaResolutionIntegral;
        }

        /// <summary>
        /// Update height
        /// </summary>
        /// <param name="worldMap"></param>
        public void UpdateHeight_WorldMap(int index, float height)
        {
            Vector2 heights = NormalizeHeight(height);

            mapPixels[index].b = (byte)heights.x;
            mapPixels[index].a = (byte)heights.y;

            dirty = true;
        }

        /// <summary>
        /// Update height on a certain range.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeZ"></param>
        public void UpdateHeight_RANGE(float x, float z, int sizeX, int sizeZ, bool save)
        {
            int mapWidth = this.mapWidth;
            float resMultiplier = mInstance.transformCordsMultiplier;

            if (x < 0 || z < 0 || x + sizeX > mapWidth || z + sizeZ > mapWidth)
            {
                Debug.Log("Cant generate world map. Out Of Bounds.");
                return;
            }

            Transform managerTransform = mInstance.transform;

            RaycastHit hit;
            Vector3 rayPos = new Vector3(0, managerTransform.position.y + 10000, 0);
            Vector3 vDown = Vector3.down;

            Vector2 heights;
            Vector3 normals;

            Color32[] colors = mapPixels;

            for (float xIndex = x; xIndex < x + sizeX; xIndex++)
            {
                for (float zIndex = z; zIndex < z + sizeZ; zIndex++)
                {
                    rayPos.x = (xIndex * resMultiplier) + managerTransform.position.x; // transform to world cords
                    rayPos.z = (zIndex * resMultiplier) + managerTransform.position.z; // transform to world cords

                    if (UNMapGenerators.GetMapHit(rayPos, vDown, out hit, Mathf.Infinity))
                    {
                        heights = NormalizeHeight(hit.point.y);
                        normals = UNMapGenerators.TransformNormals(hit.normal);

                        colors[(int)xIndex + (int)zIndex * mapWidth] = new Color32((byte)(normals.x * 255), (byte)(normals.y * 255), (byte)(heights.x), (byte)(heights.y));
                    }
                }
            }

            if (save)
            {
                SetPixels32(colors);
            }
            else
            {
                mapPixels = colors;
            }

            dirty = true;
        }

        public Vector2 NormalizeHeight(float worldHeight)
        {
            Vector2 heights;

            float h = (worldHeight / mInstance.foliageAreaResolutionIntegral) * 65535;
            heights.x = (int)(h / 256);
            heights.y = h - (heights.x * 256);

            return heights;
        }

        public static void ApplyAreaSizeChange(FoliageManagerInstance mInstance)
        {
            mInstance.worldMap.Resize(mInstance.foliageAreaResolutionIntegral);

            UNSettings.Log("World map updated succesfully after modifying resolution.");
        }

        public void Save()
        {
            if (dirty)
            {
                #if UNITY_EDITOR
                UNMapGenerators.SaveMap(this);
                #endif

                dirty = false;
            }
        }

        #region Static
        public static bool globalDirty
        {
            get
            {
                FoliageCore_MainManager mManager = FoliageCore_MainManager.instance;
                FoliageCore_Chunk mChunk;
                FoliageManagerInstance mInstance;

                for (int j = 0; j < mManager.sector.foliageChunks.Count; j++)
                {
                    mChunk = mManager.sector.foliageChunks[j];

                    if (!mChunk.isFoliageInstanceAttached) continue;

                    mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                    if (mInstance.worldMap.dirty) return true;
                }

                return false;
            }
        }

        public static void SaveAllMaps()
        {
            FoliageCore_MainManager mManager = FoliageCore_MainManager.instance;
            FoliageCore_Chunk mChunk;
            FoliageManagerInstance mInstance;

            for (int j = 0; j < mManager.sector.foliageChunks.Count; j++)
            {
                mChunk = mManager.sector.foliageChunks[j];

                if (!mChunk.isFoliageInstanceAttached) continue;

                mInstance = mChunk.GetOrCreateFoliageManagerInstance();

                mInstance.worldMap.Save();
            }
        }
        #endregion
    }
}