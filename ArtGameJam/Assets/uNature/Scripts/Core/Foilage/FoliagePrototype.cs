using UnityEngine;
using uNature.Core.Utility;
using uNature.Core.Settings;

using System.Collections.Generic;

namespace uNature.Core.FoliageClasses
{
    public delegate void OnFoliageEnableChanged(FoliagePrototype changedPrototype, bool value);

    [System.Serializable]
    public sealed class FoliagePrototype : BasePrototypeItem
    {
        #region Static Variables
        private const int VERTEX_MAX_CAPABILITY = 65534;

        public const float SIZE_MIN_VALUE = 0.1f;
        public const float SIZE_MAX_VALUE = 5.0f;

        public static Color DEFAULT_HEALTHY_COLOR = new Color(33f / 255, 129f / 255, 25f / 255, 1);
        public static Color DEFAULT_DRY_COLOR = new Color(205f / 255, 188f / 255, 26f / 255, 1);

        static GameObject _FoliageTexGameObject;
        public static GameObject FoliageTexGameObject
        {
            get
            {
                return Terrains.UNTerrainData.FoliageGameObject;
            }
        }

        public static event OnFoliageEnableChanged OnFoliageEnabledStateChangedEvent;
        #endregion

        #region FoliageData
        [SerializeField]
        FoliageType _FoliageType;
        public FoliageType FoliageType
        {
            get { return _FoliageType; }
            set { _FoliageType = value; }
        }

        [SerializeField]
        GameObject _FoliageMesh;
        public GameObject FoliageMesh
        {
            get { return _FoliageMesh; }
            set { _FoliageMesh = value; }
        }

        [SerializeField]
        Texture2D _FoliageTexture;
        public Texture2D FoliageTexture
        {
            get { return _FoliageTexture; }
            set { _FoliageTexture = value; }
        }

        [SerializeField]
        private int foliageID;
        public int id
        {
            get
            {
                return foliageID;
            }
            private set
            {
                foliageID = value;
            }
        }

        public int maxFoliageCapability = 0;

        [SerializeField]
        float _spread = 1;
        public float spread
        {
            get
            {
                return _spread;
            }
            set
            {
                value = Mathf.Clamp(value, 0, 2);

                if (value != _spread)
                {
                    _spread = value;
                    UpdateManagerInformation();
                }
            }
        }

        [SerializeField]
        Vector2 _meshInstancesGenerationOffset = Vector2.zero;
        /// <summary>
        /// The offset of which mesh instances will be generated ( so if 1 instance is supposed to be created for a chunk and the offset is 1 then 2 will be generated)
        /// Dont increase this value unless necessary!!!
        /// </summary>
        public Vector2 meshInstancesGenerationOffset
        {
            get
            {
                return _meshInstancesGenerationOffset;
            }
            set
            {
                if(_meshInstancesGenerationOffset != value)
                {
                    _meshInstancesGenerationOffset.x = value.x;
                    _meshInstancesGenerationOffset.y = value.y;

                    FoliageMeshManager.GenerateFoliageMeshInstances(id);
                }
            }
        }

        #region Size
        [SerializeField]
        float _minimumWidth = 1.5f;
        public float minimumWidth
        {
            get
            {
                return Mathf.Clamp(_minimumWidth, SIZE_MIN_VALUE, _maximumWidth);
            }
            set
            {
                value = Mathf.Clamp(value, SIZE_MIN_VALUE, _maximumWidth);

                if(value != _minimumWidth)
                {
                    _minimumWidth = value;

                    UpdateManagerInformation();
                }
            }
        }

        [SerializeField]
        float _maximumWidth = 2f;
        public float maximumWidth
        {
            get
            {
                return Mathf.Clamp(_maximumWidth, _minimumWidth, SIZE_MAX_VALUE);
            }
            set
            {
                value = Mathf.Clamp(value, _minimumWidth, SIZE_MAX_VALUE);

                if (value != _maximumWidth)
                {
                    _maximumWidth = value;

                    UpdateManagerInformation();
                }
            }
        }

        [SerializeField]
        float _minimumHeight = 0.7f;
        public float minimumHeight
        {
            get
            {
                return Mathf.Clamp(_minimumHeight, SIZE_MIN_VALUE, _maximumHeight);
            }
            set
            {
                value = Mathf.Clamp(value, SIZE_MIN_VALUE, _maximumHeight);

                if (value != _minimumHeight)
                {
                    _minimumHeight = value;

                    UpdateManagerInformation();
                }
            }
        }

        [SerializeField]
        float _maximumHeight = 1f;
        public float maximumHeight
        {
            get
            {
                return Mathf.Clamp(_maximumHeight, _minimumHeight, SIZE_MAX_VALUE);
            }
            set
            {
                value = Mathf.Clamp(value, _minimumHeight, SIZE_MAX_VALUE);

                if (value != _maximumHeight)
                {
                    _maximumHeight = value;

                    UpdateManagerInformation();
                }
            }
        }
        #endregion

        [SerializeField]
        bool _receiveShadows = true;
        public bool receiveShadows
        {
            get
            {
                return _receiveShadows;
            }
            set
            {
                _receiveShadows = value;
            }
        }

        [SerializeField]
        Color _dryColor;
        public Color dryColor
        {
            get
            {
                return _dryColor;
            }
            set
            {
                if(_dryColor != value)
                {
                    _dryColor = value;

                    FoliageInstancedMeshData.mat.SetColor("_dryColor", _dryColor);
                }
            }
        }

        [SerializeField]
        Color _healthyColor;
        public Color healthyColor
        {
            get
            {
                return _healthyColor;
            }
            set
            {
                if (_healthyColor != value)
                {
                    _healthyColor = value;

                    FoliageInstancedMeshData.mat.SetColor("_healthyColor", _healthyColor);
                }
            }
        }

        [SerializeField]
        bool _castShadows = false;
        public bool castShadows
        {
            get
            {
                return _castShadows;
            }
            set
            {
                _castShadows = value;
            }
        }

        [SerializeField]
        float _fadeDistance = 100;
        public float fadeDistance
        {
            get
            {
                return _fadeDistance;
            }
            set
            {
                if(_fadeDistance != value)
                {
                    _fadeDistance = value;
                    FoliageInstancedMeshData.mat.SetFloat("fadeDistance", value);
                }
            }
        }

        [SerializeField]
        int _maxGeneratedDensity = 10;
        public int maxGeneratedDensity
        {
            get
            {
                return Mathf.Clamp(_maxGeneratedDensity, 1, FoliageInstancedMeshData.MeshInstancesLimiter_Optimization_Clamp);
            }
            set
            {
                value = Mathf.Clamp(value, 1, FoliageInstancedMeshData.MeshInstancesLimiter_Optimization_Clamp);

                if (_maxGeneratedDensity != value)
                {
                    _maxGeneratedDensity = value;

                    if (enabled)
                    {
                        FoliageCore_MainManager.GenerateFoliageMeshInstances(id);
                    }
                }
            }
        }

        [SerializeField]
        bool _useColorMap;
        public bool useColorMap
        {
            get
            {
                return _useColorMap;
            }
            set
            {
                if(_useColorMap != value)
                {
                    _useColorMap = value;

                    FoliageInstancedMeshData.mat.SetFloat("_UseColorMap", value ? 1 : 0);
                }
            }
        }

        public string name
        {
            get
            {
                return FoliageType == FoliageType.Prefab ? FoliageMesh == null ? "None" : FoliageMesh.name : FoliageTexture == null ? "None" : FoliageTexture.name;
            }
        }

        [SerializeField]
        FoliageGenerationRadius _FoliageGenerationRadius = FoliageGenerationRadius._3x3;
        public FoliageGenerationRadius FoliageGenerationRadius
        {
            get
            {
                return _FoliageGenerationRadius;
            }
            set
            {
                if (_FoliageGenerationRadius != value)
                {
                    _FoliageGenerationRadius = value;

                    if (enabled)
                    {
                        FoliageCore_MainManager.GenerateFoliageMeshInstances(id);
                    }
                }
            }
        }

        [SerializeField]
        private bool _enabled = true;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if(_enabled != value)
                {
                    _enabled = value;

                    if(OnFoliageEnabledStateChangedEvent != null)
                    {
                        OnFoliageEnabledStateChangedEvent(this, value);
                    }

                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(FoliageDB.instance);
                    }
                    #endif
                }
            }
        }

        [SerializeField]
        private int _meshLodsCount = 3;
        public int meshLodsCount
        {
            get
            {
                return Mathf.Clamp(_meshLodsCount, 1, maxGeneratedDensity);
            }
            set
            {
                value = Mathf.Clamp(value, 1, maxGeneratedDensity);

                if(_meshLodsCount != value)
                {
                    _meshLodsCount = value;
                }
            }
        }

        [SerializeField]
        private int _renderingLayer = 0;
        public int renderingLayer
        {
            get
            {
                return _renderingLayer;
            }
            set
            {
                _renderingLayer = value;
            }
        }

        [SerializeField]
        private bool _touchBendingEnabled = true;
        public bool touchBendingEnabled
        {
            get
            {
                return _touchBendingEnabled;
            }
            set
            {
                if (_touchBendingEnabled != value)
                {
                    _touchBendingEnabled = value;
                    FoliageInstancedMeshData.mat.SetFloat("touchBendingEnabled", value ? 1 : 0);
                }
            }
        }

        [SerializeField]
        private float _touchBendingStrength = 0.97f;
        public float touchBendingStrength
        {
            get
            {
                return _touchBendingStrength;
            }
            set
            {
                if (_touchBendingStrength != value)
                {
                    _touchBendingStrength = value;
                    FoliageInstancedMeshData.mat.SetFloat("touchBendingStrength", value);
                }
            }
        }

        public override bool isEnabled
        {
            get
            {
                return enabled;
            }
        }
        public override bool chooseableOnDisabled
        {
            get
            {
                return true;
            }
        }
        #endregion

        #region WindSettings
        [SerializeField]
        bool _useCustomWind = false;
        public bool useCustomWind
        {
            get
            {
                return _useCustomWind;
            }
            set
            {
                if (value != _useCustomWind)
                {
                    _useCustomWind = value;

                    FoliageDB.instance.UpdateShaderWindSettings();
                }
            }
        }

        public WindSettings customWindSettings = new WindSettings();
        #endregion

        #region LODs
        bool _useLODs = true;
        public bool useLODs
        {
            get
            {
                return _useLODs;
            }
            set
            {
                if(_useLODs != value)
                {
                    _useLODs = value;

                    UpdateLODs();
                }
            }
        }

        [SerializeField]
        FoliageLODLevel[] _lods = null;
        public FoliageLODLevel[] lods
        {
            get
            {
                if (_lods == null)
                {
                    _lods = new FoliageLODLevel[4];

                    _lods[0] = new FoliageLODLevel(75, 0.8f);
                    _lods[1] = new FoliageLODLevel(130, 0.6f);
                    _lods[2] = new FoliageLODLevel(160, 0.4f);
                    _lods[3] = new FoliageLODLevel(200, 0.2f);
                }

                return _lods;
            }
            set
            {
                _lods = value;

                bool changed = false;
                for(int i = 0; i < _lods.Length; i++)
                {
                    if (_lods[i].isDirty)
                    {
                        changed = true;
                        _lods[i].isDirty = false;
                    }
                }

                if(changed)
                {
                    UpdateLODs();
                }
            }
        }
        #endregion

        #region Instance
        [SerializeField]
        FoliageMesh _FoliageInstancedMeshData;
        public FoliageMesh FoliageInstancedMeshData
        {
            get
            {
                return _FoliageInstancedMeshData;
            }
        }

        public Vector3 instancedEuler;
        #endregion
        
        private FoliagePrototype(Texture2D texture, GameObject prefab, float minWidth, float minHeight, float maxWidth, float maxHeight, float spread, int layer, int id)
        {
            FoliageType = prefab == null ? FoliageType.Texture : FoliageType.Prefab;
            FoliageMesh = prefab;
            FoliageTexture = texture;

            this.id = id;

            _spread = spread;

            _renderingLayer = layer;

            _minimumWidth = minWidth;
            _maximumWidth = maxWidth;

            _minimumHeight = minHeight;
            _maximumHeight = maxHeight;
        }

        public bool EqualsToPrototype(DetailPrototype detail)
        {
            return detail.prototype == this.FoliageMesh && detail.prototypeTexture == FoliageTexture;
        }

        private void GenerateInstantiatedMesh(Color healthyColor, Color dryColor)
        {
            GameObject instance;
            Vector3 offset;

            if (FoliageType == FoliageType.Prefab)
            {
                instance = FoliageMesh;
                offset = Vector3.zero;
            }
            else
            {
                instance = FoliageTexGameObject;
                instance.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", FoliageTexture);

                offset = new Vector3(0, 0.5f * instance.transform.localScale.y, 0); // assign offset.
            }

            _FoliageInstancedMeshData = new FoliageMesh(instance, id, offset);

            this.healthyColor = healthyColor;
            this.dryColor = dryColor;

            // Apply lods.
            UpdateLODs();
        }

        private int CalculateMaxFoliageCapability()
        {
            return Mathf.FloorToInt(VERTEX_MAX_CAPABILITY / FoliageInstancedMeshData.vertexCount);
        }

        private void UpdateLODs()
        {
            FoliageInstancedMeshData.mat.SetFloat("lods_Enabled", useLODs ? 1 : 0);

            for(int i = 0; i < lods.Length; i++)
            {
                FoliageInstancedMeshData.mat.SetFloat("lod" + i + "_Distance", lods[i].lodDistance);
                FoliageInstancedMeshData.mat.SetFloat("lod" + i + "_Value", lods[i].lodValue);
            }
        }

        /// <summary>
        /// Get Preview
        /// </summary>
        /// <returns></returns>
        protected override Texture2D GetPreview()
        {
            #if UNITY_EDITOR
            if(FoliageType == FoliageType.Prefab)
            {
                return FoliageMesh == null ? null : UnityEditor.AssetPreview.GetAssetPreview(FoliageMesh);
            }
            else if(FoliageType == FoliageType.Texture)
            {
                return FoliageTexture;
            }
            #endif

            return null;
        }

        /// <summary>
        /// Apply the wind parameters to this Foliage prototype.
        /// </summary>
        public void ApplyWind()
        {
            WindSettings targetedSettings = useCustomWind ? customWindSettings : FoliageDB.instance.globalWindSettings;

            FoliageInstancedMeshData.mat.SetFloat("_WindSpeed", targetedSettings.windSpeed);
            FoliageInstancedMeshData.mat.SetFloat("_WindBending", targetedSettings.windBending);
        }

        /// <summary>
        /// Apply color map
        /// 
        /// Res = area size.
        /// </summary>
        public void ApplyColorMap(Texture2D map, Texture2D normalMap)
        {
            FoliageInstancedMeshData.mat.SetTexture("_ColorMap", map);
            FoliageInstancedMeshData.mat.SetTexture("_WorldMap", normalMap);
        }

        /// <summary>
        /// Apply color map
        /// 
        /// Res = area size.
        /// </summary>
        public void ApplyGrassMap(Texture2D map)
        {
            FoliageInstancedMeshData.mat.SetTexture("_GrassMap", map);
        }

        /// <summary>
        /// Update the global spread noise.
        /// </summary>
        public void UpdateManagerInformation()
        {
            FoliageInstancedMeshData.mat.SetFloat("_DensityMultiplier", FoliageCore_MainManager.instance.density);
            FoliageInstancedMeshData.mat.SetFloat("_NoiseMultiplier", spread);

            FoliageInstancedMeshData.mat.SetFloat("_MinimumWidth", minimumWidth);
            FoliageInstancedMeshData.mat.SetFloat("_MaximumWidth", maximumWidth);

            FoliageInstancedMeshData.mat.SetFloat("_MinimumHeight", minimumHeight);
            FoliageInstancedMeshData.mat.SetFloat("_MaximumHeight", maximumHeight);

            FoliageInstancedMeshData.mat.SetFloat("_FoliageAreaSize", FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE);
        }

        /// <summary>
        /// Update the touch bending
        /// </summary>
        public void UpdateTouchBending()
        {
            #if UNITY_5_4_OR_NEWER
            FoliageInstancedMeshData.mat.SetVectorArray("_InteractionTouchBendedInstances", TouchBending.bendingTargets);
            #endif
        }

        /// <summary>
        /// Create a prototype.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="prefab"></param>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
        /// <param name="spread"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static FoliagePrototype CreatePrototype(Texture2D texture, GameObject prefab, float minWidth, float minHeight, float maxWidth, float maxHeight, float spread, int layer, int id, Color healthyColor, Color dryColor)
        {
            FoliagePrototype prototype = new FoliagePrototype(texture, prefab, minWidth, minHeight, maxWidth, maxHeight, spread, layer, id);

            FoliageDB.instance.RegisterNewPrototype(prototype);

            prototype.GenerateInstantiatedMesh(healthyColor, dryColor);

            prototype.instancedEuler = prototype.FoliageInstancedMeshData.eulerAngles;
            prototype.maxFoliageCapability = prototype.CalculateMaxFoliageCapability();
            prototype.UpdateManagerInformation();

            FoliageCore_MainManager.UpdateGrassMap();

            return prototype;
        }
    }

    public enum FoliageType
    {
        Prefab,
        Texture
    }

    [System.Serializable]
    public class FoliageMesh
    {
        public const int OPTIMIZATION_MESH_INSTANCES_DENSITIES_LIMITER = 12;
        public static string materialsCachePath = Settings.UNSettings.ProjectPath + "Resources/Foliage/Materials/";

        public Vector3[] positions;
        public Mesh[] meshes;

        public Material mat;

        public Vector3 eulerAngles;

        [SerializeField]
        private Vector3 _rendererScale;
        public Vector3 rendererScale
        {
            get
            {
                return _rendererScale;
            }
        }

        [SerializeField]
        private Vector3 _worldScale = Vector3.zero;
        public Vector3 worldScale
        {
            get
            {
                if(_worldScale == Vector3.zero)
                {
                    _worldScale = rendererScale;

                    _worldScale.Scale(scale);
                }

                return _worldScale;
            }
        }

        [SerializeField]
        private UNMeshData _meshData = null;
        public UNMeshData meshData
        {
            get
            {
                if(_meshData == null)
                {
                    _meshData = new UNMeshData(meshes);
                }

                return _meshData;
            }
        }

        public Vector3 scale = Vector3.one;

        public Vector3 offset;

        public int vertexCount;

        [SerializeField]
        private int meshInstancesLimiter_Optimization_Clamp = 15;
        public int MeshInstancesLimiter_Optimization_Clamp
        {
            get
            {
                return meshInstancesLimiter_Optimization_Clamp;
            }
        }

        public FoliageMesh(GameObject go, int layer, Vector3 offset)
        {
            var filters = go.GetComponentsInChildren<MeshFilter>(true);

            if (filters.Length == 0) return;

            this.offset = offset;

            go.transform.position = Vector3.zero + offset;
            eulerAngles = go.transform.eulerAngles;
            scale = go.transform.localScale;

            CalculateRendererScale(go);

            MeshFilter filter;

            positions = new Vector3[filters.Length];
            meshes = new Mesh[filters.Length];

            for(int i = 0; i < filters.Length; i++)
            {
                filter = filters[i];

                meshes[i] = filter.sharedMesh;
                positions[i] = filter.transform.position;

                vertexCount += filter.sharedMesh.vertexCount;
            }

            Material matInstance = GameObject.Instantiate<Material>(go.GetComponentsInChildren<MeshRenderer>(true)[0].sharedMaterial);
            matInstance.name = "Foliage Material";

            mat = matInstance;

            _meshData = new UNMeshData(meshes);

            if(vertexCount > OPTIMIZATION_MESH_INSTANCES_DENSITIES_LIMITER)
            {
                int differences = Mathf.Clamp((int)((float)vertexCount / OPTIMIZATION_MESH_INSTANCES_DENSITIES_LIMITER), 0, 14);

                meshInstancesLimiter_Optimization_Clamp = 15 - differences;
            }

            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(mat, string.Format(materialsCachePath + "{0}_{1}.mat", mat.name, layer));
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        private void CalculateRendererScale(GameObject obj)
        {
            Bounds bounds = new Bounds();

            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);

            for(int i = 0; i < renderers.Length; i++)
            {
                if(i == 0)
                {
                    bounds = renderers[i].bounds;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            _rendererScale = bounds.size;
        }
    }

    [System.Serializable]
    public struct FoliageLODLevel
    {
        public const int LOD_MAX_DISTANCE = 500;

        [SerializeField]
        Vector2 _vectorRepresentation;
        public Vector2 vectorRepresentation
        {
            get
            {
                return _vectorRepresentation;
            }
        }

        public float lodDistance
        {
            get
            {
                return _vectorRepresentation.x;
            }
            set
            {
                value = Mathf.Clamp(value, 0, LOD_MAX_DISTANCE);

                if (_vectorRepresentation.x != value)
                {
                    _vectorRepresentation.x = value;
                    isDirty = true;
                }
            }
        }
        public float lodValue
        {
            get
            {
                return _vectorRepresentation.y;
            }
            set
            {
                value = Mathf.Clamp(value, 0, 1);

                if (_vectorRepresentation.y != value)
                {
                    _vectorRepresentation.y = value;
                    isDirty = true;
                }
            }
        }

        bool _isDirty;
        internal bool isDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
            }
        }

        public FoliageLODLevel(float lodDistance, float lodValue)
        {
            _vectorRepresentation.x = lodDistance;
            _vectorRepresentation.y = lodValue;
            _isDirty = true;
        }
    }

    [System.Serializable]
    public class UNMeshData
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<int> triangles;

        public List<Vector2> uv1s;

        public int verticesLength;
        public int normalsLength;
        public int uv1sLength;
        public int trianglesLength;

        public UNMeshData(Mesh[] meshes)
        {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            triangles = new List<int>();
            uv1s = new List<Vector2>();

            verticesLength = 0;
            normalsLength = 0;
            uv1sLength = 0;
            trianglesLength = 0;

            for(int i = 0; i < meshes.Length; i++)
            {
                FetchMesh(meshes[i]);
            }
        }

        private void FetchMesh(Mesh mesh)
        {
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                vertices.Add(mesh.vertices[i]);
            }
            verticesLength += mesh.vertexCount;

            for (int i = 0; i < mesh.normals.Length; i++)
            {
                normals.Add(mesh.normals[i]);
            }
            normalsLength += mesh.normals.Length;

            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                triangles.Add(mesh.triangles[i]);
            }
            trianglesLength += mesh.triangles.Length;

            for (int i = 0; i < mesh.uv.Length; i++)
            {
                uv1s.Add(mesh.uv[i]);
            }
            uv1sLength += mesh.uv.Length;
        }
    }
}
