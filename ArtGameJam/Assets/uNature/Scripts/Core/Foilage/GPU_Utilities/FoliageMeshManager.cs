using UnityEngine;
using System.Collections.Generic;

using uNature.Core.Utility;
using uNature.Core.Targets;

using UniLinq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace uNature.Core.FoliageClasses
{
    public class FoliageMeshManager : UNTarget
    {
        public static List<Mesh> globalMeshesThreshold = new List<Mesh>();

        #region PropertyIDs
        private static int _property_id_worldposition = -1;
        private static int PROPERTY_ID_WORLDPOSITION
        {
            get
            {
                if(_property_id_worldposition == -1)
                {
                    _property_id_worldposition = Shader.PropertyToID("_WorldPosition");
                }

                return _property_id_worldposition;
            }
        }

        private static int _property_id_grassmap = -1;
        private static int PROPERTY_ID_GRASSMAP
        {
            get
            {
                if(_property_id_grassmap == -1)
                {
                    _property_id_grassmap = Shader.PropertyToID("_GrassMap");
                }

                return _property_id_grassmap;
            }
        }

        private static int _property_id_foliage_interaction_position = -1;
        private static int PROPERTY_ID_FOLIAGE_INTERACTION_POSITION
        {
            get
            {
                if(_property_id_foliage_interaction_position == -1)
                {
                    _property_id_foliage_interaction_position = Shader.PropertyToID("_FoliageInteractionPosition");
                }

                return _property_id_foliage_interaction_position;
            }
        }
        #endregion

        #if UNITY_EDITOR
        protected bool RENDERING_editorDrawCalled;
        protected Vector3 RENDERING_lastEditorCameraPosition = Vector3.zero;
        #endif

        #region Rendering Variables
        static Dictionary<FoliageResolutions, Dictionary<int, GPUMesh>> _prototypeMeshInstances = null;
        public static Dictionary<FoliageResolutions, Dictionary<int, GPUMesh>> prototypeMeshInstances
        {
            get
            {
                if (_prototypeMeshInstances == null)
                {
                    _prototypeMeshInstances = new Dictionary<FoliageResolutions, Dictionary<int, GPUMesh>>();
                }

                return _prototypeMeshInstances;
            }
        }

        [System.NonSerialized]
        MaterialPropertyBlock _propertyBlock = null;
        MaterialPropertyBlock propertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }

                return _propertyBlock;
            }
        }
        #endregion

        #region Debug Variables
        public bool DEBUG_Window_Open = true;
        public bool DEBUG_Window_Minimized = false;

        protected int _lastRenderedVertices;
        public int lastRenderedVertices
        {
            get
            {
                return _lastRenderedVertices;
            }
        }

        protected int _lastRenderedDrawCalls;
        public int lastRenderedDrawCalls
        {
            get
            {
                return _lastRenderedDrawCalls * 2;
            }
        }

        protected int _lastRenderedPrototypes;
        public int lastRenderedPrototypes
        {
            get
            {
                return _lastRenderedPrototypes; // include shadow pass
            }
        }

        Rect debugWindowRect = new Rect(Screen.width / 25, Screen.height / 15, 500, 400);

        FoliageCore_Chunk latestManagerChunk = null;

        Texture2D _closeIcon;
        Texture2D closeIcon
        {
            get
            {
                if (_closeIcon == null)
                {
                    _closeIcon = UNStandaloneUtility.GetUIIcon("Close");
                }

                return _closeIcon;
            }
        }

        Texture2D _showIcon;
        Texture2D showIcon
        {
            get
            {
                if (_showIcon == null)
                {
                    _showIcon = UNStandaloneUtility.GetUIIcon("Show");
                }

                return _showIcon;
            }
        }

        Texture2D _hideIcon;
        Texture2D hideIcon
        {
            get
            {
                if (_hideIcon == null)
                {
                    _hideIcon = UNStandaloneUtility.GetUIIcon("Hide");
                }

                return _hideIcon;
            }
        }
        #endregion

        #region Constructors
        protected override void OnEnable()
        {
            base.OnEnable();

            Camera.onPostRender += OnGlobalPostRender;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DestroyMeshInstances();

            Camera.onPostRender -= OnGlobalPostRender;
        }
        #endregion

        #region Mesh Instances Methods
        /// <summary>
        /// Generate new mesh instances
        /// </summary>
        /// <param name="areaSize"></param>
        public static void GenerateFoliageMeshInstances()
        {
            foreach (var meshInstances in prototypeMeshInstances)
            {
                if (meshInstances.Value.Count > 0)
                {
                    for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        if (meshInstances.Value.ContainsKey(FoliageDB.unSortedPrototypes[i].id))
                        {
                            meshInstances.Value[FoliageDB.unSortedPrototypes[i].id].Destroy();
                        }
                    }

                    meshInstances.Value.Clear();
                }

                if (FoliageCore_MainManager.instance.enabled)
                {
                    for (int prototypeIndex = 0; prototypeIndex < FoliageDB.unSortedPrototypes.Count; prototypeIndex++)
                    {
                        if (FoliageDB.unSortedPrototypes[prototypeIndex].enabled)
                        {
                            GenerateFoliageMeshInstanceForIndex(FoliageDB.unSortedPrototypes[prototypeIndex].id, meshInstances.Key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate new mesh instances
        /// </summary>
        /// <param name="areaSize"></param>
        public static void GenerateFoliageMeshInstances(FoliageResolutions resolution)
        {
            if (!prototypeMeshInstances.ContainsKey(resolution))
            {
                prototypeMeshInstances.Add(resolution, new Dictionary<int, GPUMesh>());
            }

            var meshInstances = prototypeMeshInstances[resolution];

            if (meshInstances.Count > 0)
            {
                for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                {
                    if (meshInstances.ContainsKey(FoliageDB.unSortedPrototypes[i].id))
                    {
                        meshInstances[FoliageDB.unSortedPrototypes[i].id].Destroy();
                    }
                }

                meshInstances.Clear();
            }

            if (FoliageCore_MainManager.instance.enabled)
            {
                for (int prototypeIndex = 0; prototypeIndex < FoliageDB.unSortedPrototypes.Count; prototypeIndex++)
                {
                    if (FoliageDB.unSortedPrototypes[prototypeIndex].enabled)
                    {
                        GenerateFoliageMeshInstanceForIndex(FoliageDB.unSortedPrototypes[prototypeIndex].id, resolution);
                    }
                }
            }
        }

        /// <summary>
        /// Generate new mesh instances
        /// </summary>
        /// <param name="areaSize"></param>
        public static void GenerateFoliageMeshInstances(int prototypeID)
        {
            foreach (var meshInstances in prototypeMeshInstances)
            {
                if (FoliageCore_MainManager.instance.enabled)
                {
                    if (FoliageDB.sortedPrototypes[prototypeID].enabled)
                    {
                        GenerateFoliageMeshInstanceForIndex(prototypeID, meshInstances.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Create Foliage mesh instances for a certain index and foliage size.
        /// </summary>
        /// <param name="meshInstances"></param>
        /// <param name="prototypeIndex"></param>
        public static void GenerateFoliageMeshInstanceForIndex(int prototypeIndex, FoliageResolutions resolution)
        {
            Dictionary<int, GPUMesh> meshInstances = prototypeMeshInstances[resolution];

            FoliagePrototype prototype = FoliageDB.sortedPrototypes[prototypeIndex];

            if (!FoliageCore_MainManager.instance.enabled) return;

            bool prototypeMeshExists = prototypeMeshInstances != null && meshInstances.ContainsKey(prototypeIndex);

            if (prototypeMeshExists)
            {
                meshInstances[prototypeIndex].Destroy();
                meshInstances.Remove(prototypeIndex);
            }

            Mesh[] meshes = new Mesh[prototype.meshLodsCount];
            int[] densities = new int[prototype.meshLodsCount];
            List<UNPhysicsTemplate>[] physicsObjects = new List<UNPhysicsTemplate>[prototype.meshLodsCount];

            for (int i = 0; i < prototype.meshLodsCount; i++)
            {
                meshes[i] = CreateNewMesh();
                densities[i] = Mathf.FloorToInt((float)prototype.maxGeneratedDensity / (i + 1));

                FoliageMeshInstance.CreateGPUMesh(prototype, meshes[i], densities[i]);
            }
            meshInstances.Add(prototypeIndex, new GPUMesh(meshes, densities, prototypeIndex, physicsObjects, resolution));
        }

        /// <summary>
        /// Destroy the current mesh instances.
        /// </summary>
        protected static void DestroyMeshInstances()
        {
            if (prototypeMeshInstances.Count == 0) return;

            FoliagePrototype prototype;

            foreach (var meshInstances in prototypeMeshInstances.Values)
            {
                for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                {
                    prototype = FoliageDB.unSortedPrototypes[i];

                    if (meshInstances != null && meshInstances.ContainsKey(prototype.id))
                    {
                        meshInstances[prototype.id].Destroy();
                    }
                }

                meshInstances.Clear();
            }
            _prototypeMeshInstances = null;
        }

        /// <summary>
        /// Destroy a mesh instance
        /// </summary>
        /// <param name="prototypeID"></param>
        public static void DestroyMeshInstance(int prototypeID)
        {
            foreach (var meshInstances in prototypeMeshInstances.Values)
            {
                if (meshInstances != null && meshInstances.ContainsKey(prototypeID))
                {
                    meshInstances[prototypeID].Destroy();
                }

                meshInstances.Remove(prototypeID);
            }
        }

        /// <summary>
        /// Create a new mesh instace
        /// </summary>
        /// <returns></returns>
        private static Mesh CreateNewMesh()
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;

            return mesh;
        }

        /// <summary>
        /// Update the mesh instances bounds
        /// </summary>
        public void UpdateMeshBounds(Vector3 centerPos)
        {
            Bounds newBounds = new Bounds(centerPos, FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_BOUNDS_MAX * 2);

            for (int i = 0; i < globalMeshesThreshold.Count; i++)
            {
                try
                {
                    globalMeshesThreshold[i].bounds = newBounds;
                }
                catch
                {
                    globalMeshesThreshold.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Rendering Methods

        public void OnGlobalPostRender(Camera camera)
        {
            bool sceneCamera = false;

            #if UNITY_EDITOR
            sceneCamera = SceneView.currentDrawingSceneView;

            // 
            // Editor camera, force scene updating.
            //
            if (!Application.isPlaying)
            {
                SceneView sView = SceneView.currentDrawingSceneView;
                if (sView != null && sView.camera == camera)
                {
                    if (Vector3.Distance(sView.camera.transform.position, RENDERING_lastEditorCameraPosition) > 5 || !RENDERING_editorDrawCalled)
                    {
                        // this method will force an update call on the editor, so the chunks can be updated on the editor.
                        EditorUtility.SetDirty(this);

                        RENDERING_lastEditorCameraPosition = sView.camera.transform.position;
                        RENDERING_editorDrawCalled = true;

                        return;
                    }
                }
            }
            #endif

            if (CheckIfCameraIsSeeker(camera) || sceneCamera) // if this camera belongs to a seeker
            {
                //UpdateMeshBounds(camera.transform.position);
            }
        }

        protected override void Update()
        {
            base.Update();

            DrawEditorCameras();

            if (Application.isPlaying)
            {
                var cameras = Camera.allCameras;

                for (int i = 0; i < cameras.Length; i++)
                {
                    OnDrawCamera(cameras[i]);
                }
            }
        }

        private void DrawEditorCameras()
        {
#if UNITY_EDITOR
            var sceneCameras = UnityEditor.SceneView.sceneViews;
            UnityEditor.SceneView sView;

            for (int i = 0; i < sceneCameras.Count; i++)
            {
                sView = sceneCameras[i] as UnityEditor.SceneView;

                OnDrawCamera(sView.camera);
            }
#endif
        }

        private void DEBUG_ResetValues()
        {
            _lastRenderedDrawCalls = 0;
            _lastRenderedVertices = 0;
            _lastRenderedPrototypes = 0;
        }

        public void OnDrawCamera(Camera camera)
        {
            if (!FoliageCore_MainManager.instance.enabled || prototypeMeshInstances == null) return;

            bool runCameraCheck = true;
            FoliageReceiver receiver = null;

            #if UNITY_EDITOR
            runCameraCheck = !UnityEditor.SceneView.GetAllSceneCameras().Contains(camera);
            #endif

            if (runCameraCheck)
            {
                if (FoliageReceiver.FReceivers.Count == 0) return;

                for (int i = 0; i < FoliageReceiver.FReceivers.Count; i++)
                {
                    if (camera == FoliageReceiver.FReceivers[i].playerCamera)
                    {
                        receiver = FoliageReceiver.FReceivers[i];

                        break;
                    }
                    if (i == FoliageReceiver.FReceivers.Count - 1)
                    {
                        return;
                    }
                }
            }

            if (receiver != null && !receiver.isGrassReceiver) return;

            int areaSizeIntegral = FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE;

            FoliageCore_Chunk[] targetedChunks = null;
            FoliageCore_Chunk currentMChunk;

            if (receiver == null)
            {
                targetedChunks = UNStandaloneUtility.GetFoliageChunksNeighbors(camera.transform.position - transform.position, targetedChunks);
                latestManagerChunk = targetedChunks[4];
            }
            else
            {
                targetedChunks = receiver.neighbors;
                latestManagerChunk = receiver.middleFoliageChunkFromNeighbors;
            }

            Vector3 normalizedCameraPosition;

            FoliageManagerInstance mInstance;
            FoliageSector sector;
            FoliageChunk chunk;

            float density = FoliageCore_MainManager.instance.density;
            int instancesResolution = FoliageCore_MainManager.instance.instancesSectorResolution;

            #region PER_INSTANCE
            GPUMesh gpuInstance = null;

            FoliageChunk currentInstanceChunk;

            Vector3 pos;
            Material mat;

            int chunkIndex;

            FoliageMeshInstancesGroup meshGroup;
            FoliagePrototype prototype;
            int maxDensity;

            FoliageMeshInstance meshInstance;

            Vector3 chunkPos;

            int gpuMeshIndex = -1;

            Camera renderCamera = Application.isPlaying ? camera : null;

            List<FoliageChunk> chunks;

            Mesh targetMesh;
            Dictionary<int, GPUMesh> prototypeInstances;

            bool useQualitySettingsShadows = FoliageCore_MainManager.instance.useQualitySettingsShadowDistance;
            float shadowsDistance = FoliageCore_MainManager.instance.foliageShadowDistance;
            #endregion

            propertyBlock.SetVector("_StreamingAdjuster", UNStandaloneUtility.GetStreamingAdjuster());

            if (receiver != null)
            {
                propertyBlock.SetFloat("_InteractionResolution", receiver.interactionMapResolutionIntegral);
            }

            for (int i = 0; i < targetedChunks.Length; i++)
            {
                currentMChunk = targetedChunks[i];

                if (currentMChunk == null) continue;

                normalizedCameraPosition = camera.transform.position;

                if (!currentMChunk.InBounds(normalizedCameraPosition, 100) || !currentMChunk.isFoliageInstanceAttached)
                {
                    continue;
                }

                mInstance = currentMChunk.GetOrCreateFoliageManagerInstance();

                normalizedCameraPosition -= mInstance.transform.position;

                normalizedCameraPosition.x = Mathf.Clamp(normalizedCameraPosition.x, 0, areaSizeIntegral - 1);
                normalizedCameraPosition.z = Mathf.Clamp(normalizedCameraPosition.z, 0, areaSizeIntegral - 1);

                sector = mInstance.sector;
                chunks = sector.FoliageChunks;

                prototypeInstances = mInstance.meshInstances;

                chunk = sector.getChunk(normalizedCameraPosition) as FoliageChunk;

                if (chunk == null)
                {
                    continue;
                }

                if (receiver != null)
                {
                    propertyBlock.SetVector(PROPERTY_ID_FOLIAGE_INTERACTION_POSITION, chunk.position3D);
                }

                DEBUG_ResetValues();

                mInstance.UpdateMaterialBlock(propertyBlock);

                for (int prototypeIndex = 0; prototypeIndex < FoliageDB.unSortedPrototypes.Count; prototypeIndex++)
                {
                    prototype = FoliageDB.unSortedPrototypes[prototypeIndex];

                    if (!prototype.enabled) continue;

                    int chunkOffset = FoliageMeshInstance.GENERATION_RANGE_OFFSET(prototype);

                    int prototypeRadius = (int)prototype.FoliageGenerationRadius;

                    propertyBlock.SetTexture(PROPERTY_ID_GRASSMAP, mInstance.grassMaps[prototype].map);

                    //DEBUG
                    _lastRenderedPrototypes++;

                    try
                    {
                        gpuInstance = prototypeInstances[prototype.id];
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError(ex.ToString());

                        return;
                    }

                    try
                    {
                        mat = prototype.FoliageInstancedMeshData.mat;
                    }
                    catch
                    {
                        // if Foliage db was deleted/ a detail was removed...

                        FoliageCore_MainManager.DestroyMeshInstance(prototype.id);

                        OnDrawCamera(camera);
                        return;
                    }

                    int xIndex;
                    int zIndex;

                    for (int x = 0; x < prototypeRadius; x++)
                    {
                        for (int z = 0; z < prototypeRadius; z++)
                        {
                            xIndex = chunk.x + (x - chunkOffset);
                            zIndex = chunk.z + (z - chunkOffset);

                            if (xIndex < 0 || zIndex < 0)
                            {
                                continue;
                            }

                            chunkIndex = xIndex + (zIndex * instancesResolution);

                            if (chunkIndex >= chunks.Count)
                            {
                                continue;
                            }

                            currentInstanceChunk = chunks[chunkIndex];

                            if (currentInstanceChunk != null)
                            {
                                chunkPos = currentInstanceChunk.position3D;

                                maxDensity = (int)(currentInstanceChunk.GetMaxDensityOnArea(prototype.id) * density);
                                gpuMeshIndex = gpuInstance.GetMesh(maxDensity);

                                if (gpuMeshIndex != -1)
                                {
                                    //meshGroup = gpuInstance.LODMeshInstances[x, z, gpuMeshIndex];
                                    meshGroup = gpuInstance.LODMeshInstances[gpuMeshIndex];

                                    targetMesh = gpuInstance.meshes[gpuMeshIndex].mesh;

                                    for (int j = 0; j < meshGroup.Count; j++)
                                    {
                                        meshInstance = meshGroup[j];

                                        pos = meshInstance.GetPosition(chunkPos);

                                        if (pos.x < 0 || pos.z < 0 || pos.x >= areaSizeIntegral || pos.z >= areaSizeIntegral) continue;

                                        propertyBlock.SetVector(PROPERTY_ID_WORLDPOSITION, pos);

                                        //DEBUG
                                        _lastRenderedVertices += targetMesh.vertexCount;

                                        //DEBUG
                                        _lastRenderedDrawCalls++;

                                        meshInstance.DrawAndUpdate(pos, targetMesh, mat, renderCamera, normalizedCameraPosition, prototype, propertyBlock, useQualitySettingsShadows, shadowsDistance);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool CheckIfCameraIsSeeker(Camera camera)
        {
            if (FoliageReceiver.FReceivers.Count == 0) return false;

            for (int i = 0; i < FoliageReceiver.FReceivers.Count; i++)
            {
                if (camera == FoliageReceiver.FReceivers[i].playerCamera)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region DEBUG_UI Methods
        private void OnGUI()
        {
            DEBUG_DrawUI();
        }

        public void DEBUG_DrawUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

            if (Settings.UNSettings.instance.UN_Console_Debugging_Enabled)
            {
#if UNITY_EDITOR
                Handles.BeginGUI();
#endif

                ClampDebugWindow();

                if (DEBUG_Window_Open)
                {
                    debugWindowRect = GUILayout.Window(3, debugWindowRect, (id) =>
                    {
                        if (GUI.Button(new Rect(580, 0.75f, 15, 15), closeIcon, "Label"))
                        {
                            DEBUG_Window_Open = false;
                        }

                        if (FoliageCore_MainManager.instance.enabled)
                        {
                            if (GUI.Button(new Rect(565, -1f, 15, 15), DEBUG_Window_Minimized ? "+" : "-", "Label"))
                            {
                                DEBUG_Window_Minimized = !DEBUG_Window_Minimized;
                            }

                            if (!DEBUG_Window_Minimized)
                            {
                                GUILayout.BeginHorizontal();

                                GUILayout.BeginVertical();

                                #region Global Settings
                                DebugGlobalSettings();
                                #endregion

                                #region Prototypes
                                GUILayout.Space(10);

                                GUILayout.Label("Prototypes Settings:", UNStandaloneUtility.boldLabel);

                                UNStandaloneUtility.BeginHorizontalOffset(25);
                                for (int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                                {
                                    GUILayout.Space(5);

                                    DebugPrototypeInformation(FoliageDB.unSortedPrototypes[i].id);
                                }
                                UNStandaloneUtility.EndHorizontalOffset();
                                #endregion

                                GUILayout.EndVertical();

                                GUILayout.Space(10);

                                DebugVisualSettings();

                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUILayout.Label("", GUILayout.Height(15));
                            }
                        }
                        else
                        {
                            GUILayout.Label("Foliage disabled. Enable it on the FoliageManager component in your scene.");
                        }

                        GUI.DragWindow();

                    }, "uNature Debug", "Window", GUILayout.Width(600), GUILayout.Height((DEBUG_Window_Minimized || !FoliageCore_MainManager.instance.enabled) ? 2 : 400));
                }

#if UNITY_EDITOR
                Handles.EndGUI();
#endif
            }

#endif
        }

        private void DebugGlobalSettings()
        {
            GUILayout.Space(10);

            GUILayout.Label(string.Format("Global Settings:    (Frame: {0})", Time.frameCount), UNStandaloneUtility.boldLabel);

            GUILayout.Space(5);

            UNStandaloneUtility.BeginHorizontalOffset(25);

            GUILayout.Label(string.Format("Current Grided-Position : {0} ({1})", latestManagerChunk == null ? "Out Of Bounds" : latestManagerChunk.transform.position.ToString(), latestManagerChunk == null ? "NaN" : latestManagerChunk.isFoliageInstanceAttached ? latestManagerChunk.GetOrCreateFoliageManagerInstance().foliageAreaResolutionIntegral.ToString() : "Not Poppulated"));
            GUILayout.Label("Foliage Shadow Distance: " + (FoliageCore_MainManager.instance.useQualitySettingsShadowDistance ? QualitySettings.shadowDistance : FoliageCore_MainManager.instance.foliageShadowDistance));
            GUILayout.Label("Foliage Foliage Density: " + FoliageCore_MainManager.instance.density);

            UNStandaloneUtility.EndHorizontalOffset();
        }

        private void DebugPrototypeInformation(int prototypeID)
        {
            FoliagePrototype prototype = FoliageDB.sortedPrototypes[prototypeID];

            GUILayout.Label(string.Format("{0} (ID: {1})", prototype.name, prototypeID), UNStandaloneUtility.boldLabel);

            GUILayout.Space(5);

            UNStandaloneUtility.BeginHorizontalOffset(25);

            GUILayout.Label("GPU Generated Density: " + prototype.maxGeneratedDensity);
            GUILayout.Label("Generation Radius: " + prototype.FoliageGenerationRadius.ToString().Replace("_", "")); //show radius and remove the "_" character.

            GUILayout.Label(string.Format("Width Noise: {0} ~ {1}", System.Math.Round(prototype.minimumWidth, 2), System.Math.Round(prototype.maximumWidth, 2)));
            GUILayout.Label(string.Format("Height Noise: {0} ~ {1}", System.Math.Round(prototype.minimumHeight, 2), System.Math.Round(prototype.maximumHeight, 2)));

            GUILayout.Space(5);

            GUILayout.Label("Wind:");

            UNStandaloneUtility.BeginHorizontalOffset(25);

            GUILayout.Label("Individual Wind: " + prototype.useCustomWind);
            GUILayout.Label("Wind Bending: " + (prototype.useCustomWind == false ? FoliageDB.instance.globalWindSettings.windBending : prototype.customWindSettings.windBending));
            GUILayout.Label("Wind Speed: " + (prototype.useCustomWind == false ? FoliageDB.instance.globalWindSettings.windSpeed : prototype.customWindSettings.windSpeed));

            UNStandaloneUtility.EndHorizontalOffset();

            UNStandaloneUtility.EndHorizontalOffset();
        }

        private void DebugVisualSettings()
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);

            GUILayout.Label("Visual Settings:", UNStandaloneUtility.boldLabel);

            UNStandaloneUtility.BeginHorizontalOffset(25);

            GUILayout.Space(5);

            GUILayout.Label("Vertices: " + string.Format("{0:n0}", lastRenderedVertices));
            GUILayout.Label("Draw Calls: " + string.Format("{0:n0}", lastRenderedDrawCalls));
            GUILayout.Label("Prototypes Drawn: " + string.Format("{0:n0}", lastRenderedPrototypes));

            UNStandaloneUtility.EndHorizontalOffset();

            GUILayout.EndVertical();
        }

        private void ClampDebugWindow()
        {
            float halfWidth = debugWindowRect.width / 2;
            float halfHeight = debugWindowRect.height / 2;

            debugWindowRect.x = Mathf.Clamp(debugWindowRect.x, -halfWidth, Screen.width - halfWidth);
            debugWindowRect.y = Mathf.Clamp(debugWindowRect.y, -halfHeight, Screen.height - halfHeight);
        }
        #endregion
    }

    public enum FoliageGenerationRadius
    {
        _1x1 = 1,
        _3x3 = 3,
        _5x5 = 5
    }

    /// <summary>
    /// A class used to hold the gpu meshes
    /// </summary>
    public class GPUMesh
    {
        public List<GPUMeshLOD> meshes = new List<GPUMeshLOD>();

        /// <summary>
        /// Dimension 1 : x chunk
        /// Dimension 2 : z chunk
        /// Dimension 3 : LOD index
        /// </summary>
        //public FoliageMeshInstancesGroup[,,] LODMeshInstances = null;
        public FoliageMeshInstancesGroup[] LODMeshInstances = null;

        public GPUMesh(Mesh[] LODMeshes, int[] LODLevels, int prototypeIndex, List<UNPhysicsTemplate>[] physicsInformation, FoliageResolutions resolution)
        {
            if (LODMeshes.Length != LODLevels.Length)
            {
                Debug.LogError("uNature Foliage : Generating LODs Failed!, Array sizes are different!");
                return;
            }

            LODMeshInstances = new FoliageMeshInstancesGroup[LODMeshes.Length];

            for (int i = 0; i < LODMeshes.Length; i++)
            {
                meshes.Add(new GPUMeshLOD(LODMeshes[i], LODLevels[i], prototypeIndex, physicsInformation[i]));

                LODMeshInstances[i] = FoliageMeshInstance.CreateFoliageInstances(prototypeIndex, LODLevels[i], resolution);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                LODMeshInstances[i].Destroy();

                Object.DestroyImmediate(meshes[i].mesh);
            }

            meshes = null;
        }
        public int GetMesh(int density)
        {
            if (density == 0) return -1;

            for (int i = meshes.Count - 1; i >= 0; i--)
            {
                if (density <= meshes[i].density)
                {
                    return i;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// GPU Mesh Lods.
    /// </summary>
    public class GPUMeshLOD
    {
        public Mesh mesh;
        public int density;
        public List<UNPhysicsTemplate> physicsTemplates = new List<UNPhysicsTemplate>();

        public GPUMeshLOD(Mesh _mesh, int _density, int _prototypeIndex, List<UNPhysicsTemplate> physicsInformation)
        {
            mesh = _mesh;
            density = _density;

            this.physicsTemplates = physicsInformation;
            FoliageMeshManager.globalMeshesThreshold.Add(mesh);
        }

        public void Destroy()
        {
            GameObject.DestroyImmediate(mesh);

            for (int i = 0; i < physicsTemplates.Count; i++)
            {
                System.GC.SuppressFinalize(physicsTemplates[i]);
            }

            FoliageMeshManager.globalMeshesThreshold.Remove(mesh);
        }
    }

    public class FoliageMeshInstancesGroup
    {
        private List<FoliageMeshInstance> meshInstances = new List<FoliageMeshInstance>();

        public FoliageMeshInstance this[int index]
        {
            get
            {
                return meshInstances[index];
            }
            set
            {
                meshInstances[index] = value;
            }
        }

        private int count;
        public int Count
        {
            get
            {
                return count;
            }
        }

        public FoliageMeshInstancesGroup()
        {
            meshInstances = new List<FoliageMeshInstance>();
            count = 0;
        }

        public void AddMeshInstance(FoliageMeshInstance instance)
        {
            meshInstances.Add(instance);
            count = meshInstances.Count;
        }

        public void Destroy()
        {
            for (int i = 0; i < meshInstances.Count; i++)
            {
                meshInstances[i].Destroy();
            }
            meshInstances.Clear();

            count = 0;
        }
    }
}