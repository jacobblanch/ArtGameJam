using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using uNature.Core.Utility;
using uNature.Core.Threading;

namespace uNature.Core.FoliageClasses
{
    public class UNFoliageEditor : EditorWindow
    {
        GUIStyle invisibleButtonStyle;

        #region PaintVariables
        [System.NonSerialized]
        CurrentPaintMethod paintMethod;

        int paintBrushSize;
        byte paintDensity;
        bool instaRemove;

        [System.NonSerialized]
        List<PaintBrush> chosenBrushes = new List<PaintBrush>();

        Vector2 brushesScrollPos;
        #endregion

        #region PrototypesVariables
        [System.NonSerialized]
        List<FoliagePrototype> chosenPrototypes = new List<FoliagePrototype>();

        Vector2 prototypesScrollPos;
        Vector2 prototypesEditDataPos;
        Vector2 globalSettingsPos;

        Vector3 lastBrushPosition;
        #endregion

        Vector2 globalScrollPos;

        int _sectorResolution = -1;
        int sectorResolution
        {
            get
            {
                if(_sectorResolution == -1)
                {
                    _sectorResolution = FoliageCore_MainManager.instance.instancesSectorResolution;
                }

                return _sectorResolution;
            }
            set
            {
                _sectorResolution = value;
            }
        }

        bool ctrlPressed = false;

        #region Foliage Chunks
        [System.NonSerialized]
        FoliageCore_Chunk selectedChunk = null;
        #endregion

        [MenuItem("Window/uNature/Foliage")]
        public static void OpenWindow()
        {
            GetWindow<UNFoliageEditor>("Foliage Manager");
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        private void Update()
        {
            this.Repaint();

            CreateMissingLayers();
        }

        public void OnGUI()
        {
            if (FoliageCore_MainManager.instance == null)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Foliage Manager Not Found!!");

                if(GUILayout.Button("Create Foliage Manager"))
                {
                    FoliageCore_MainManager.InitializeAndCreateIfNotFound();
                }
                GUILayout.EndVertical();

                return;
            }

            globalScrollPos = EditorGUILayout.BeginScrollView(globalScrollPos);

            ctrlPressed = Event.current == null ? false : Event.current.control;

            if (Event.current != null && Event.current.keyCode == KeyCode.Escape) // try to disable brush on GUI window
            {
                chosenBrushes.Clear();

                EditorUtility.SetDirty(FoliageDB.instance);
                EditorUtility.SetDirty(FoliageCore_MainManager.instance);
            }

            if (invisibleButtonStyle == null)
            {
                invisibleButtonStyle = new GUIStyle("Box");

                invisibleButtonStyle.normal.background = null;
                invisibleButtonStyle.focused.background = null;
                invisibleButtonStyle.hover.background = null;
                invisibleButtonStyle.active.background = null;
            }

            FoliageCore_MainManager.instance.enabled = UNEditorUtility.DrawHelpBox(string.Format("Foliage Manager: (GUID : {0})", FoliageCore_MainManager.instance.guid), "Here you can manage and paint \nFoliage all over your scene!", true, FoliageCore_MainManager.instance.enabled); // add variable to edit.

            GUI.enabled = FoliageCore_MainManager.instance.enabled;

            DrawPaintWindow();

            GUILayout.Space(5);

            DrawPrototypesWindow();

            prototypesEditDataPos = EditorGUILayout.BeginScrollView(prototypesEditDataPos);

            GUILayout.Space(2);

            DrawPrototypesEditUI();

            GUILayout.Space(5);

            DrawGlobalSettingsUI();

            GUILayout.Space(5);

            DrawFoliageInstancesEditingUI();

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(FoliageDB.instance);
                EditorUtility.SetDirty(FoliageCore_MainManager.instance);
            }

            EditorGUILayout.EndScrollView();

            GUI.enabled = true;
        }

        void DrawPaintWindow()
        {
            GUILayout.BeginVertical("Box", GUILayout.Width(450));

            GUILayout.BeginHorizontal();

            GUILayout.Label("Paint Tools:", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reload Brushes"))
            {
                FoliageDB.instance.brushes = null;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("Box");

            string[] paintMethodsNames = Enum.GetNames(typeof(CurrentPaintMethod));
            CurrentPaintMethod currentType;

            for (int i = 0; i < paintMethodsNames.Length; i++)
            {
                currentType = (Enum.GetValues(typeof(CurrentPaintMethod)) as CurrentPaintMethod[])[i];

                if (UNEditorUtility.DrawHighlitableButton(UNStandaloneUtility.GetUIIcon(paintMethodsNames[i]), paintMethod == currentType, GUILayout.Width(40), GUILayout.Height(40)))
                {
                    paintMethod = currentType; // select the paint method.
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(3);

            GUILayout.BeginVertical("Box", GUILayout.Height(100));

            GUILayout.Label("Paint Settings:", EditorStyles.boldLabel);

            GUILayout.Space(5);

            switch (paintMethod)
            {
                case CurrentPaintMethod.Normal_Paint:
                    DrawNormalBrush();
                    break;

                case CurrentPaintMethod.Spline_Paint:
                    DrawSplineBrush();
                    break;
            }

            GUILayout.Space(5);

            paintBrushSize = EditorGUILayout.IntSlider(new GUIContent("Paint Brush Size:", "The percentage from the brush that will be drawn"), paintBrushSize, 1, 30);
            paintDensity = (byte)EditorGUILayout.IntSlider(new GUIContent("Paint Brush Density:", "The percentage from the brush that will be drawn"), paintDensity, 0, 15);
            instaRemove = EditorGUILayout.Toggle(new GUIContent("Instant Remove On Shift:", "Instantely remove the grass when shift is pressed (instead of needing to put density to 0"), instaRemove);

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (FoliageGrassMap.globalDirty)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Foliage hasn't been saved after painting on terrain. \nPlease save the data by clicking the save button.", MessageType.Warning);

                if(GUILayout.Button("Save Grass Maps"))
                {
                    FoliageGrassMap.SaveAllMaps();

                    EditorUtility.SetDirty(FoliageCore_MainManager.instance);

                    if (!Application.isPlaying)
                    {
                        #if UNITY_5_3_OR_NEWER
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                        #else
                        EditorApplication.SaveScene();
                        #endif
                    }

                    AssetDatabase.SaveAssets();
                }
            }
            

            if (FoliageWorldMap.globalDirty)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("World information isn't been saved. \nPlease save the data by clicking the save button or changes wont be applied.", MessageType.Warning);

                if (GUILayout.Button("Save World Map"))
                {
                    FoliageWorldMap.SaveAllMaps();

                    EditorUtility.SetDirty(FoliageCore_MainManager.instance);

                    if (!Application.isPlaying)
                    {
                        #if UNITY_5_3_OR_NEWER
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                        #else
                        EditorApplication.SaveScene();
                        #endif
                    }

                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.EndVertical();
        }
    
        void DrawNormalBrush()
        {
            chosenBrushes = UNEditorUtility.DrawPrototypesSelector(FoliageDB.instance.brushes, chosenBrushes, false, 300, ref brushesScrollPos);
        }

        void DrawSplineBrush()
        {
            GUILayout.Label("Spline Painting Is Not Yet Implemented!", EditorStyles.boldLabel);
        }
        
        void DrawPrototypesWindow()
        {
            GUILayout.BeginVertical("Box", GUILayout.Width(450));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Prototypes Management:", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (chosenPrototypes.Count > 0)
            {
                if (GUILayout.Button("-", GUILayout.Width(15), GUILayout.Width(15)))
                {
                    if (EditorUtility.DisplayDialog("uNature", "Are you sure you want to delete this prototype ? \nThis cannot be undone!", "Yes", "No"))
                    {
                        for (int i = 0; i < chosenPrototypes.Count; i++)
                        {
                            FoliageDB.instance.RemovePrototype(chosenPrototypes[i]);
                        }
                        chosenPrototypes.Clear();

                        return;
                    }
                }
                if (GUILayout.Button(new GUIContent("R", "Remove the prototype density from this foliage manager.")))
                {
                    if (EditorUtility.DisplayDialog("uNature", "Are you sure you want to remove this prototype's density ? \nThis cannot be undone!", "Yes", "No"))
                    {
                        for (int i = 0; i < chosenPrototypes.Count; i++)
                        {
                            FoliageCore_MainManager.ResetGrassMap(chosenPrototypes);
                        }
                    }
                }

                if(GUILayout.Button(new GUIContent("L", "Locate the material instance of this prototype")))
                {
                    UnityEngine.Object[] selectionTargets = new UnityEngine.Object[chosenPrototypes.Count];

                    for(int i = 0; i < selectionTargets.Length; i++)
                    {
                        selectionTargets[i] = chosenPrototypes[i].FoliageInstancedMeshData.mat;
                    }

                    Selection.objects = selectionTargets;
                }

            }

            GUILayout.EndHorizontal();

            chosenPrototypes = UNEditorUtility.DrawPrototypesSelector(FoliageDB.unSortedPrototypes, chosenPrototypes, ctrlPressed, 440, ref prototypesScrollPos);

            #region Drag and drop
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type == EventType.DragExited)
            {
                GameObject targetFoliagePrefab;
                Texture2D targetFoliageTexture;
                UnityEngine.Object targetGeneric;

                bool exists = false;

                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    targetFoliagePrefab = DragAndDrop.objectReferences[i] as GameObject;
                    targetFoliageTexture = DragAndDrop.objectReferences[i] as Texture2D;

                    targetGeneric = targetFoliagePrefab == null ? (UnityEngine.Object)targetFoliageTexture : targetFoliagePrefab;

                    if (targetGeneric != null)
                    {
                        for (int b = 0; b < FoliageDB.unSortedPrototypes.Count; b++)
                        {
                            if ((targetFoliagePrefab != null && targetFoliagePrefab == FoliageDB.unSortedPrototypes[b].FoliageMesh) || (targetFoliageTexture != null && targetFoliageTexture == FoliageDB.unSortedPrototypes[b].FoliageTexture))
                            {
                                Debug.LogWarning("Foliage : " + targetGeneric.name + " Already exists! Ignored!");
                                exists = true;
                                break;
                            }
                        }
                    }

                    if (exists)
                        continue;

                    if (targetFoliagePrefab != null)
                    {
                        FoliageDB.instance.AddPrototype(targetFoliagePrefab);
                    }
                    else if(targetFoliageTexture != null)
                    {
                        FoliageDB.instance.AddPrototype(targetFoliageTexture);
                    }
                }
            }
            #endregion

            GUILayout.EndVertical();
        }

        void DrawPrototypesEditUI()
        {
            bool canEdit = chosenPrototypes.Count == 1;

            EditorGUILayout.BeginVertical("Box", GUILayout.Width(450), GUILayout.MaxHeight(canEdit ? 215 : 35));

            FoliagePrototype prototype;

            if (chosenPrototypes.Count == 1) // render settings
            {
                prototype = chosenPrototypes[0];

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Prototype: {0}({1})", prototype.name, prototype.id), EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                prototype.enabled = GUILayout.Toggle(prototype.enabled, "") && GUI.enabled;

                GUILayout.EndHorizontal();

                GUI.enabled = prototype.enabled;

                GUILayout.Space(10);

                GUILayout.Label("Generation Settings:", EditorStyles.boldLabel);

                prototype.spread = EditorGUILayout.Slider(new GUIContent("Spread Noise:", "The randomized space between each Foliage"), prototype.spread, 0, 2);
                prototype.FoliageGenerationRadius = (FoliageGenerationRadius)EditorGUILayout.EnumPopup("Generation Radius:", prototype.FoliageGenerationRadius);

                Vector2 widthValues = UNEditorUtility.MinMaxSlider(new GUIContent("Width Noise:", ""), prototype.minimumWidth, prototype.maximumWidth, FoliagePrototype.SIZE_MIN_VALUE, FoliagePrototype.SIZE_MAX_VALUE);
                Vector2 heightValues = UNEditorUtility.MinMaxSlider(new GUIContent("Height Noise:", ""), prototype.minimumHeight, prototype.maximumHeight, FoliagePrototype.SIZE_MIN_VALUE, FoliagePrototype.SIZE_MAX_VALUE);

                prototype.minimumWidth = widthValues.x;
                prototype.maximumWidth = widthValues.y;

                prototype.minimumHeight = heightValues.x;
                prototype.maximumHeight = heightValues.y;

                GUILayout.Space(10);

                prototype.renderingLayer = EditorGUILayout.LayerField(new GUIContent("Rendering Layer: ", "The rendering layer"), prototype.renderingLayer);
                prototype.fadeDistance = EditorGUILayout.Slider("Fade Distance: ", prototype.fadeDistance, 0, 1000);

                GUILayout.Space(10);

                prototype.receiveShadows = EditorGUILayout.Toggle("Receive Shadows:", prototype.receiveShadows);
                prototype.castShadows = EditorGUILayout.Toggle("Cast Shadows:", prototype.castShadows);
                prototype.useColorMap = EditorGUILayout.Toggle("Use Color Map:", prototype.useColorMap);
                prototype.maxGeneratedDensity = EditorGUILayout.IntSlider("Max Generatable Density:", prototype.maxGeneratedDensity, 1, prototype.FoliageInstancedMeshData.MeshInstancesLimiter_Optimization_Clamp);
                prototype.meshInstancesGenerationOffset = EditorGUILayout.Vector2Field(new GUIContent("Instances Generating Offset: ", "Offset for mesh instances generation, don't change unless necessary!!"), prototype.meshInstancesGenerationOffset);

                GUILayout.Space(10);

                GUILayout.Label("Touch Bending Settings :", EditorStyles.boldLabel);

                prototype.touchBendingEnabled = GUILayout.Toggle(prototype.touchBendingEnabled, "Touch Bending Enabled");
                prototype.touchBendingStrength = EditorGUILayout.Slider("Touch Bending Strength", prototype.touchBendingStrength, 0.01f, 5f);

                GUILayout.Space(10);

                GUILayout.Label("Individual Wind Settings :", EditorStyles.boldLabel);

                prototype.useCustomWind = EditorGUILayout.BeginToggleGroup(new GUIContent("Individual Wind", "Use Individual wind for this specific prototype (dont use the global settings)"), prototype.useCustomWind);
                prototype.customWindSettings.windBending = EditorGUILayout.Slider("Wind Bending:", prototype.customWindSettings.windBending, 0, 1);
                prototype.customWindSettings.windSpeed = EditorGUILayout.Slider("Wind Speed:", prototype.customWindSettings.windSpeed, 0, 1);
                EditorGUILayout.EndToggleGroup();

                GUILayout.Space(10);

                prototype.useLODs = EditorGUILayout.BeginToggleGroup(new GUIContent("Use LODs", "Use Level Of Detail on the Foliage."), prototype.useLODs);
                prototype.lods = UNEditorUtility.DrawLODsEditor(prototype.lods);
                EditorGUILayout.EndToggleGroup();

                GUILayout.Space(10);

                prototype.healthyColor = EditorGUILayout.ColorField("Healthy Color", prototype.healthyColor);
                prototype.dryColor = EditorGUILayout.ColorField("Dry Color", prototype.dryColor);

                GUI.enabled = true;
            }
            else if (chosenPrototypes.Count > 1) // if bigger than one, lets disable editing (not supporting multi-editing)
            {
                GUILayout.Label("Multi-editing is not supported!, Please note \nthat you can still draw while multi-selecting prototypes.", EditorStyles.centeredGreyMiniLabel);
            }
            else // if zero, just write that no item is selected.
            {
                GUILayout.Label("No prototype is selected, please choose one to continue!", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndVertical();
        }

        void DrawGlobalSettingsUI()
        {
            EditorGUILayout.BeginVertical("Box", GUILayout.Width(450), GUILayout.MaxHeight(200));

            GUILayout.Label("Global Settings:", EditorStyles.boldLabel);

            GUILayout.Space(10);

            /*
            FoliageManager.instance.FoliageAreaSize = (FoliageResolutions)EditorGUILayout.EnumPopup("Foliage Area Size:", FoliageManager.instance.FoliageAreaSize);
            FoliageManager.instance.FoliageAreaResolution = (FoliageResolutions)EditorGUILayout.EnumPopup("Foliage Area Resolution:", FoliageManager.instance.FoliageAreaResolution);
            */

            bool sectorResolutionDirty = FoliageCore_MainManager.instance.instancesSectorResolution != sectorResolution;
            bool defaultGUIEnabled = GUI.enabled;

            GUILayout.BeginHorizontal();
            sectorResolution = EditorGUILayout.IntSlider("Sector Resolution:", sectorResolution, 2, 10);

            GUI.enabled = sectorResolutionDirty;
            if (GUILayout.Button("Update", GUILayout.MaxWidth(60), GUILayout.MaxHeight(15)))
            {
                FoliageCore_MainManager.instance.instancesSectorResolution = sectorResolution;
            }
            GUI.enabled = defaultGUIEnabled;
            GUILayout.EndHorizontal();

            FoliageCore_MainManager.instance.useQualitySettingsShadowDistance = EditorGUILayout.Toggle("Use Quality Settings Shadow Distance:", FoliageCore_MainManager.instance.useQualitySettingsShadowDistance);

            #region Shadows Settings
            bool tempEnabled = GUI.enabled;

            GUI.enabled = FoliageCore_MainManager.instance.useQualitySettingsShadowDistance == false && tempEnabled;

            FoliageCore_MainManager.instance.foliageShadowDistance = EditorGUILayout.Slider("Foliage Shadow Distance :", FoliageCore_MainManager.instance.foliageShadowDistance, 0, QualitySettings.shadowDistance);

            GUI.enabled = tempEnabled;
            #endregion

            GUILayout.Space(5);

            FoliageCore_MainManager.instance.density = EditorGUILayout.Slider("Foliage Density:", FoliageCore_MainManager.instance.density, 0, 1);
            FoliageCore_MainManager.instance.FoliageGenerationLayerMask = EditorGUILayout.MaskField("Maps Generation Mask:", FoliageCore_MainManager.instance.FoliageGenerationLayerMask, GetLayerNames());

            GUILayout.Label("Wind Settings:", EditorStyles.boldLabel);

            FoliageDB.instance.globalWindSettings.windBending = EditorGUILayout.Slider("Wind Bending:", FoliageDB.instance.globalWindSettings.windBending, 0, 1);
            FoliageDB.instance.globalWindSettings.windSpeed = EditorGUILayout.Slider("Wind Speed:", FoliageDB.instance.globalWindSettings.windSpeed, 0, 1);

            GUILayout.Space(10);

            /* color maps
            GUILayout.BeginHorizontal();
            GUILayout.Label("Color Maps Settings", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate"))
            {
                FoliageManager.instance.colorMap = FoliageManager.instance.GenerateColorMapFromArea();
            }

            GUILayout.EndHorizontal();

            FoliageManager.instance.colorMap = (Texture2D)EditorGUILayout.ObjectField(string.Format("Color Map: ({0})", FoliageManager.instance.FoliageAreaSizeIntegral), FoliageManager.instance.colorMap, typeof(Texture2D), false);

            // normal maps

            GUILayout.BeginHorizontal();
            GUILayout.Label("World Maps Settings", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate"))
            {
                FoliageManager.instance.worldMap = FoliageManager.instance.GenerateWorldMapFromArea();
            }

            GUILayout.EndHorizontal();

            FoliageManager.instance.worldMap.map = (Texture2D)EditorGUILayout.ObjectField(string.Format("World Map: ({0})", FoliageManager.instance.worldMapResolutionIntegral), FoliageManager.instance.worldMap.map, typeof(Texture2D), false);

            FoliageManager.instance.worldMapResolution = (FoliageResolutions)EditorGUILayout.EnumPopup ("World Map Resolution", FoliageManager.instance.worldMapResolution);
            */

            GUILayout.EndVertical();
        }

        void DrawFoliageInstancesEditingUI()
        {
            EditorGUILayout.BeginVertical("Box", GUILayout.Width(450), GUILayout.MaxHeight(200));

            GUILayout.Label("Chunks Settings:", EditorStyles.boldLabel);

            GUILayout.Space(10);

            if (selectedChunk == null)
            {
                GUILayout.Label("Chunk cannot be found!");
            }
            else
            {
                if (!selectedChunk.isFoliageInstanceAttached)
                {
                    GUILayout.Label("Chunk doesn't have a manager attached!");
                }
                else
                {
                    FoliageManagerInstance mInstance = selectedChunk.GetOrCreateFoliageManagerInstance();

                    mInstance.foliageAreaResolution = (FoliageResolutions)EditorGUILayout.EnumPopup("Foliage Area Resolution", mInstance.foliageAreaResolution);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("World Maps Settings", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Generate"))
                    {
                        mInstance.worldMap = UNMapGenerators.GenerateWorldMap(mInstance);
                    }

                    GUILayout.EndHorizontal();

                    mInstance.worldMap.map = (Texture2D)EditorGUILayout.ObjectField(string.Format("World Map: ({0})", mInstance.foliageAreaResolutionIntegral), mInstance.worldMap.map, typeof(Texture2D), false);

                    GUILayout.Space(5);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Color Maps Settings", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Generate"))
                    {
                        mInstance.colorMap = UNMapGenerators.GenerateColorMap(0, 0, FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE, mInstance.guid);
                    }

                    GUILayout.EndHorizontal();

                    mInstance.colorMap = (Texture2D)EditorGUILayout.ObjectField(string.Format("Color Map: ({0})", FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE), mInstance.colorMap, typeof(Texture2D), false);

                }
            }

            EditorGUILayout.EndVertical();
        }

        void OnSceneGUI(SceneView sView)
        {
            if (FoliageCore_MainManager.instance == null) return;

            var current = Event.current;

            if (current != null)
            {
                if (Event.current.keyCode == KeyCode.Escape) // try to disable brush on Scene window
                {
                    chosenBrushes.Clear();

                    EditorUtility.SetDirty(FoliageDB.instance);
                    EditorUtility.SetDirty(FoliageCore_MainManager.instance);
                }

                EventType type = current.type;

                var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                RaycastHit hit;

                #region Brushes
                if (chosenBrushes.Count > 0 && chosenPrototypes.Count > 0)
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));    //Disable scene view mouse selection

                    UNBrushUtility.instance.DrawBrush(chosenBrushes[0].brushTexture, Color.cyan, ray.origin, Quaternion.FromToRotation(Vector3.forward, ray.direction), paintBrushSize);

                    bool hitTarget = Physics.Raycast(ray, out hit, Mathf.Infinity);
                    if (type == EventType.MouseDrag && current.button == 0 && hitTarget)
                    {
                        if (Vector3.Distance(lastBrushPosition, hit.point) > 1)
                        {
                            lastBrushPosition = hit.point;

                            PaintBrush(current.shift, new Vector2(hit.point.x, hit.point.z), chosenBrushes[0]);

                            Repaint();
                        }

                        if (type == EventType.KeyUp)
                        {
                            HandleUtility.Repaint();    //Enable scene view mouse selection
                        }
                    }
                }
                #endregion

                #region Chunks Selection
                if (type == EventType.mouseDown && current.button == 0)
                {
                    RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
                    FoliageCore_Chunk mChunk;

                    for (int i = 0; i < hits.Length; i++)
                    {
                        hit = hits[i];

                        mChunk = hit.transform.GetComponent<FoliageCore_Chunk>();

                        if (mChunk != null)
                        {
                            if (selectedChunk == mChunk) // if not changed
                            {
                                selectedChunk = null;
                            }   
                            else
                            {
                                selectedChunk = mChunk;
                            }

                            return;
                        }
                    }
                }
            }
            #endregion

            #region DebugMode
            if (FoliageCore_MainManager.instance != null)
            {
                FoliageCore_MainManager.instance.DEBUG_DrawUI();
            }
            #endregion
        }

        private string[] GetLayerNames()
        {
            SerializedObject layersManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = layersManager.FindProperty("layers");
            string[] layersNamesArray = new string[layers.arraySize];

            for (int i = 0; i < layersNamesArray.Length; i++)
            {
                layersNamesArray[i] = layers.GetArrayElementAtIndex(i).stringValue;
            }

            return layersNamesArray;
        }

        static void CreateMissingLayers()
        {
            SerializedObject layersManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = layersManager.FindProperty("layers");

            string layerName = "uNature_Terrain";

            //check if layers exists
            bool terrainLayerExists = CheckIfLayerExists(layers, "uNature_Terrain");

            if (terrainLayerExists)
            {
                return;
            }

            if (!terrainLayerExists)
            {
                bool success = CreateLayer(layersManager, layers, layerName);

                if (!success)
                {
                    return;
                }
            }

            Debug.Log("Layers created succesfully.");
        }

        static bool CreateLayer(SerializedObject layersManager, SerializedProperty layers, string layerName)
        {
            int emptyIndex = GetEmptyLayerIndex(layers);

            if (emptyIndex == -1)
                return false;

            SerializedProperty layer = layers.GetArrayElementAtIndex(emptyIndex);

            if (layer != null)
            {
                layer.stringValue = layerName;
                layersManager.ApplyModifiedProperties();
            }

            return true;
        }

        private static int GetEmptyLayerIndex(SerializedProperty layers)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue.ToLower() == "")
                    return i;
            }

            return -1;
        }

        private static bool CheckIfLayerExists(SerializedProperty layers, string layer)
        {
            var found = false;

            for (var i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue.ToLower() == layer.ToLower())
                    found = true;
            }

            return found;
        }

        void PaintBrush(bool isErase, Vector2 position, PaintBrush brush)
        {
            FoliageCore_MainManager mManager = FoliageCore_MainManager.instance;

            int chunkID;
            FoliageManagerInstance mInstance;

            brush.TryToResize(paintBrushSize);

            Texture2D texture = brush.instancedTexture;

            int textureWidth = texture.width;
            int textureHeight = texture.height;

            Color32 pixel;
            Color32[] grassMapPixels;
            int mapWidth;

            int posX;
            int posZ;

            int transformedCordX;
            int transformedCordZ;

            FoliagePrototype prototype;
            FoliageGrassMap grassMap;

            int radius = textureWidth / 2;

            byte density;

            for (int prototypeIndex = 0; prototypeIndex < chosenPrototypes.Count; prototypeIndex++)
            {
                prototype = chosenPrototypes[prototypeIndex];

                for (int x = 0; x < textureWidth; x++)
                {
                    for (int y = 0; y < textureHeight; y++)
                    {
                        pixel = brush.pixels[x, y];

                        if (pixel.r == 255 && pixel.g == 255 && pixel.b == 255)
                        {
                            posX = x - radius + (int)position.x - (int)mManager.transform.position.x;
                            posZ = y - radius + (int)position.y - (int)mManager.transform.position.z;

                            chunkID = mManager.GetChunkID(posX, posZ);

                            mInstance = mManager.sector.foliageChunks[chunkID].GetOrCreateFoliageManagerInstance();

                            transformedCordX = mInstance.TransformCord(posX, mInstance.attachedTo.transform.localPosition.x);
                            transformedCordZ = mInstance.TransformCord(posZ, mInstance.attachedTo.transform.localPosition.z);

                            grassMap = mInstance.grassMaps[prototype];

                            grassMapPixels = grassMap.mapPixels;
                            mapWidth = grassMap.mapWidth;

                            density = grassMapPixels[transformedCordX + transformedCordZ * mapWidth].b;

                            if (paintDensity > density || (isErase && (instaRemove || paintDensity < density)))
                            {
                                grassMap.mapPixels[transformedCordX + transformedCordZ * mapWidth].b = isErase && instaRemove ? (byte)0 : paintDensity;

                                grassMap.dirty = true;
                                grassMap.SetPixels32Delayed();
                            }
                        }
                    }
                }

                FoliageCore_MainManager.SaveDelayedMaps();
                FoliageCore_MainManager.CallInstancesChunksUpdate();
            }
        }
    }

    public enum CurrentPaintMethod
    {
        Normal_Paint,
        Spline_Paint,
    }
}
