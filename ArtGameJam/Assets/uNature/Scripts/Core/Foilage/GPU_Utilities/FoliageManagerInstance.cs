using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UniLinq;

using uNature.Core.Sectors;
using uNature.Core.Utility;
using uNature.Core.Settings;

namespace uNature.Core.FoliageClasses
{
    public class FoliageManagerInstance : Threading.ThreadItem
    {
        private const int AREA_SIZE = FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE;

        static List<FoliageManagerInstance> _instances = null;
        public static List<FoliageManagerInstance> instances
        {
            get
            {
                if (_instances == null)
                {
                    _instances = GameObject.FindObjectsOfType<FoliageManagerInstance>().ToList();
                }

                return _instances;
            }
        }

        #region Variables
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

        [System.NonSerialized]
        private Dictionary<int, GPUMesh> _meshInstances = null;
        public Dictionary<int, GPUMesh> meshInstances
        {
            get
            {
                if(_meshInstances == null)
                {
                    PoolMeshInstances();
                }

                return _meshInstances;
            }
        }

        [SerializeField]
        private Vector3 _pos = -Vector3.one;
        public Vector3 pos
        {
            get
            {
                if(_pos == -Vector3.one)
                {
                    _pos = transform.position;
                }

                return _pos;
            }
        }
        #endregion

        #region Maps_Sizes & Resolutions
        [SerializeField]
        FoliageResolutions _foliageAreaResolution = FoliageResolutions._512;
        public FoliageResolutions foliageAreaResolution
        {
            get
            {
                return _foliageAreaResolution;
            }
            set
            {
                if (_foliageAreaResolution != value)
                {
                    _foliageAreaResolution = value;
                    _foliageAreaResolutionIntegral = (int)value;

                    _transformCordsMultiplier = -1;

                    UpdateResolutionChange();
                }
            }
        }

        int _foliageAreaResolutionIntegral = -1;
        public int foliageAreaResolutionIntegral
        {
            get
            {
                if (_foliageAreaResolutionIntegral == -1)
                {
                    _foliageAreaResolutionIntegral = (int)foliageAreaResolution;
                }

                return _foliageAreaResolutionIntegral;
            }
        }

        [SerializeField]
        float _transformCordsMultiplier = -1;
        public float transformCordsMultiplier
        {
            get
            {
                if (_transformCordsMultiplier == -1)
                {
                    _transformCordsMultiplier = (float)AREA_SIZE / foliageAreaResolutionIntegral;
                }

                return _transformCordsMultiplier;
            }
            set
            {
                _transformCordsMultiplier = value;
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
        #endregion

        #region Maps_Textures
        [SerializeField]
        Texture2D _colorMap = null;
        public Texture2D colorMap
        {
            get
            {
                if (_colorMap == null)
                {
                    colorMap = UNMapGenerators.GenerateColorMap(transform.position.x, transform.position.z, AREA_SIZE, guid);
                }

                return _colorMap;
            }
            set
            {
                if (_colorMap != value)
                {
                    _colorMap = value;
                }
            }
        }

        [SerializeField]
        FoliageWorldMap _worldMap = null;
        public FoliageWorldMap worldMap
        {
            get
            {
                if (_worldMap == null)
                {
                    _worldMap = UNMapGenerators.GenerateWorldMap(this);
                }

                return _worldMap;
            }

            set
            {
                _worldMap = value;
            }
        }

        [System.NonSerialized]
        private Dictionary<FoliagePrototype, FoliageGrassMap> _grassMaps = null;
        public Dictionary<FoliagePrototype, FoliageGrassMap> grassMaps
        {
            get
            {
                if (_grassMaps == null || _grassMaps.Count == 0)
                {
                    _grassMaps = new Dictionary<FoliagePrototype, FoliageGrassMap>();

                    Texture2D loadedTexture;

                    for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        loadedTexture = Resources.Load<Texture2D>(UNMapGenerators.GetGrassMapPath(FoliageDB.unSortedPrototypes[i].id, this));

                        _grassMaps.Add(FoliageDB.unSortedPrototypes[i], loadedTexture == null ? UNMapGenerators.CreateGrassMap(FoliageDB.unSortedPrototypes[i].id, this) : new FoliageGrassMap(loadedTexture, FoliageDB.unSortedPrototypes[i], this));
                    }
                }
                else
                {
                    for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        if (!_grassMaps.ContainsKey(FoliageDB.unSortedPrototypes[i]))
                        {
                            _grassMaps.Add(FoliageDB.unSortedPrototypes[i], UNMapGenerators.CreateGrassMap(FoliageDB.unSortedPrototypes[i].id, this));
                        }
                    }
                }

                return _grassMaps;
            }
            set
            {
                _grassMaps = value;

                UpdateGrassMapsForMaterials();
            }
        }
        #endregion

        #region Grid-Management
        [SerializeField]
        FoliageCore_Chunk _attachedTo;
        public FoliageCore_Chunk attachedTo
        {
            get
            {
                return _attachedTo;
            }
        }

        [SerializeField]
        FoliageSector _sector;
        public FoliageSector sector
        {
            get
            {
                if (_sector == null)
                {
                    _sector = Sector.GenerateSector<FoliageSector, FoliageChunk>(transform, new Vector3(AREA_SIZE, 0, AREA_SIZE), _sector, FoliageCore_MainManager.instance.instancesSectorResolution);
                }
                return _sector;
            }
        }
        #endregion

        #region Constructors
        internal static FoliageManagerInstance CreateInstance(FoliageCore_Chunk attachedTo)
        {
            GameObject go = new GameObject("Foliage Manager Instance");
            go.transform.SetParent(attachedTo.transform);

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            FoliageManagerInstance mInstance = go.AddComponent<FoliageManagerInstance>();
            mInstance._attachedTo = attachedTo;

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("uNature", "Creating Manager Instance - Color Map", 0f);
            #endif

            if (mInstance.colorMap == null) { };

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("uNature", "Creating Manager Instance - Grass Maps", 0.33f);
            #endif

            if (mInstance.grassMaps == null) { };

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("uNature", "Creating Manager Instance - World Map", 0.67f);
            #endif

            if (mInstance.worldMap == null) { };

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
            #endif

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                #if UNITY_5_3_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                #else
                UnityEditor.EditorApplication.MarkSceneDirty();
                #endif
            }
            #endif

            return mInstance;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!instances.Contains(this))
            {
                instances.Add(this);
            }

            if (colorMap == null) return; // force creation
            if (worldMap == null) return; // force creation
            if (grassMaps == null) return; // force creation
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (instances.Contains(this))
            {
                instances.Remove(this);
            }

            ForceMapsRestore();
        }

        /// <summary>
        /// Restores the changes on the maps as long as saving changes on runtime isnt checked on the settings.
        /// </summary>
        public void ForceMapsRestore()
        {
            for(int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                grassMaps[FoliageDB.unSortedPrototypes[i]].RestoreChanges();
            }

            worldMap.RestoreChanges();
        }
        #endregion

        #region Size And Transform Changes Updates
        protected virtual void UpdateResolutionChange()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("uNature Foliage", "uNature is recalculating the new Foliage area resolution \nThat might take awhile...", 0.2f);
            #endif

            // recreate sectors.
            DestroyImmediate(sector.gameObject);

            // update world map
            worldMap = UNMapGenerators.GenerateWorldMap(this);

            // update & clean grass maps 
            DisposeExistingGrassMaps();

            _grassMaps = null;
            UpdateGrassMapsForMaterials();

            PoolMeshInstances();

            UNSettings.Log("uNature finished updating the area resolution successfully!! New resolution : " + _foliageAreaResolutionIntegral);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
            #endif
        }

        private void DisposeExistingGrassMaps()
        {
            FoliageGrassMap grassMap;

            for(int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
            {
                grassMap = grassMaps[FoliageDB.unSortedPrototypes[i]];
                grassMap.Dispose();
            }
        }

        private void PoolMeshInstances()
        {
            if (FoliageCore_MainManager.instance == null) return;

            _meshInstances = FoliageCore_MainManager.GetPrototypeMeshInstances(foliageAreaResolution);
        }

        public void UpdateMaterialBlock(MaterialPropertyBlock mBlock)
        {
            // property blocks. (Unique to each manager instance).
            mBlock.SetTexture("_ColorMap", colorMap);

            if(worldMap.map == null)
            {
                worldMap = UNMapGenerators.GenerateWorldMap(this);
            }

            mBlock.SetTexture("_WorldMap", worldMap.map);

            mBlock.SetFloat("_FoliageAreaResolution", foliageAreaResolutionIntegral);
            mBlock.SetVector("_FoliageAreaPosition", transform.position);
        }

        internal void UpdateGrassMapsForMaterials()
        {
            if (grassMaps == null) return; // force grass maps recreation.
        }

        public void RemoveGrassMap(FoliagePrototype prototype)
        {
            if (grassMaps.ContainsKey(prototype))
            {
                FoliageGrassMap grassMap = grassMaps[prototype];

                grassMaps.Remove(prototype);

                #if UNITY_EDITOR
                grassMap.Dispose();
                #endif
            }
        }
        #endregion

        #region Foliage Modifications Methods
        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int TransformCord(float x, float removeOffset)
        {
            return (int)TransformCordFloat(x, removeOffset);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float TransformCordFloat(float x, float removeOffset)
        {
            return TransformCordCustomFloat(x, removeOffset, transformCordsMultiplier);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int TransformCordCustom(float x, float removeOffset, float multiplier)
        {
            return (int)TransformCordCustomFloat(x, removeOffset, multiplier);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float TransformCordCustomFloat(float x, float removeOffset, float multiplier)
        {
            return (x - removeOffset) / multiplier;
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int InverseCord(float x, float addOffset)
        {
            return (int)InverseCordFloat(x, addOffset);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float InverseCordFloat(float x, float addOffset)
        {
            return InverseCordCustomFloat(x, addOffset, transformCordsMultiplier);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int InverseCordCustom(float x, float addOffset, float multiplier)
        {
            return (int)InverseCordCustomFloat(x, addOffset, multiplier);
        }

        /// <summary>
        /// Transform 1 cord
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float InverseCordCustomFloat(float x, float addOffset, float multiplier)
        {
            return (x * multiplier) + addOffset;
        }
        #endregion
    }
}
