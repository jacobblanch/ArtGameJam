#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using uNature.Core.Settings;
using uNature.Core.FoliageClasses;

using System.Collections.Generic;

namespace uNature.Core.Utility
{
    public static class UNEditorUtility
    {
        #region ProgressBar
        static int _currentProgressIndex;
        public static int currentProgressIndex
        {
            get
            {
                return _currentProgressIndex;
            }
            set
            {
                _currentProgressIndex = value;

                if(_currentProgressIndex >= targetProgressIndex)
                {
                    SceneView.onSceneGUIDelegate -= OnSceneGUI; // disable the rendering
                    subscribedToSceneGUI = false;
                }
            }
        }
        public static int targetProgressIndex;

        public static string scrollbarText;
        static bool subscribedToSceneGUI;
        #endregion

        #if UNITY_EDITOR
        static GUIStyle _boldedBox;
        static GUIStyle boldedBox
        {
            get
            {
                if(_boldedBox == null)
                {
                    _boldedBox = EditorStyles.helpBox;
                    _boldedBox.fontStyle = FontStyle.Bold;
                }

                return _boldedBox;
            }
        }

        static GUISkin _customSkin;
        public static GUISkin customSkin
        {
            get
            {
                if(_customSkin == null)
                {
                    _customSkin = AssetDatabase.LoadAssetAtPath<GUISkin>(UNSettings.ProjectPath + "Editor Default Resources/uNature_EditorSkin.guiskin");
                }

                return _customSkin;
            }
        }

        static GUIStyle _flatButton;
        public static GUIStyle flatButton
        {
            get
            {
                if(_flatButton == null)
                {
                    _flatButton = new GUIStyle("Box");
                }

                return _flatButton;
            }
        }
        #endif


        /// <summary>
        /// Show a scroll bar on scene GUI that shows a progress on a certain task.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="targetProgressIndex"></param>
        public static void StartSceneScrollbar(string text, int targetProgressIndex)
        {
            currentProgressIndex = 0;
            UNEditorUtility.targetProgressIndex = targetProgressIndex;

            scrollbarText = text;

            if (!subscribedToSceneGUI)
            {
                SceneView.onSceneGUIDelegate += OnSceneGUI;
                subscribedToSceneGUI = true;
            }
        }

        /// <summary>
        /// Called when rendering scene GUI.
        /// </summary>
        private static void OnSceneGUI(SceneView sceneview)
        {
            if (currentProgressIndex != targetProgressIndex)
            {
                Handles.BeginGUI();
                EditorGUI.ProgressBar(new Rect(Screen.width - 250, Screen.height - 75, 200, 20), (float)currentProgressIndex / targetProgressIndex, scrollbarText);
                Handles.EndGUI();
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Draw a help box
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        public static bool DrawHelpBox(string title, string description, bool drawEnabled, bool enabledVariable)
        {
            GUILayout.Space(10);

            GUILayout.BeginVertical(title, boldedBox);

            GUILayout.Space(15);

            GUILayout.Label(description);

            if (drawEnabled)
            {
                GUILayout.Space(15);

                float tempFieldWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 150;
                enabledVariable = EditorGUILayout.Toggle("Enabled : ", enabledVariable);
                EditorGUIUtility.labelWidth = tempFieldWidth;
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            return enabledVariable;
        }
        #endif

#if UNITY_EDITOR

        #region CustomTypeSelector

        public static T DrawSelectionBox<T>(GUIContent content, List<SelectionBoxItems<T>> values, SelectionBoxItems<T> value)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(content);

            if (GUILayout.Button("Selected Item : " + value.itemName, EditorStyles.objectField))
            {
                value = SelectionBoxWindow.instance.Draw(values, value);
                GUI.changed = true;
            }
            else
            {
                if (SelectionBoxWindow.item != null)
                {
                    value = SelectionBoxWindow.item as SelectionBoxItems<T>;
                    GUI.changed = true;
                }
            }

            GUILayout.EndHorizontal();

            return value == null ? default(T) : value.item;
        }

        #endregion

            #region PrototypesSelector
        public static List<T> DrawPrototypesSelector<T>(List<T> items, List<T> selectedItems, bool controlClicked, float areaWidth, ref Vector2 scrollPos) where T : BasePrototypeItem
        {
            bool oldEnabled = GUI.enabled;

            scrollPos = GUILayout.BeginScrollView(scrollPos, "Box", GUILayout.Width(areaWidth), GUILayout.Height(items.Count > 0 ? 80 : 25));

            GUILayout.BeginHorizontal();

            if (items.Count > 0)
            {
                GUISkin defaultSkin = GUI.skin;

                GUI.skin = customSkin;

                for (int i = 0; i < items.Count; i++)
                {
                    if(!items[i].chooseableOnDisabled)
                    {
                        GUI.enabled = items[i].isEnabled;
                    }

                    if (DrawHighlitableButton(items[i].preview, selectedItems.Contains(items[i]), GUILayout.Width(50), GUILayout.Height(50)))
                    {
                        if (selectedItems.Contains(items[i]))
                        {
                            if (selectedItems.Count > 1 && !controlClicked)
                            {
                                selectedItems.Clear();
                                selectedItems.Add(items[i]);
                            }
                            else
                            {
                                selectedItems.Remove(items[i]);
                            }
                        }
                        else
                        {
                            if (controlClicked)
                            {
                                selectedItems.Add(items[i]);
                            }
                            else
                            {
                                selectedItems.Clear();
                                selectedItems.Add(items[i]);
                            }
                        }
                    }

                    if (!items[i].chooseableOnDisabled)
                    {
                        GUI.enabled = oldEnabled;
                    }
                    else
                    {
                        if (!items[i].isEnabled)
                        {
                            Rect rect = GUILayoutUtility.GetLastRect();

                            GUI.color = Color.gray;
                            GUI.Label(new Rect(rect.x + 30, rect.y + 4, 16, 16), UNStandaloneUtility.GetUIIcon("Disabled"));
                            GUI.color = Color.white;
                        }
                    }
                }

                GUI.skin = defaultSkin;
            }
            else
            {
                GUILayout.Label("No Items Found!", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            return selectedItems;
        }
        public static bool DrawHighlitableButton(Texture2D texture, bool highlighted, params GUILayoutOption[] options)
        {
            bool pressed = false;

            GUISkin defaultSkin = GUI.skin;

            GUI.skin = customSkin;

            if(GUILayout.Button(texture, highlighted ? "Highlight" : "Box", options))
            {
                pressed = true;
            }

            GUI.skin = defaultSkin;

            return pressed;
        }
#endregion

        public static Vector2 MinMaxSlider(GUIContent content, float minValue, float maxValue, float minLimit, float maxLimit)
        {
            Vector2 value = new Vector2(minValue, maxValue);

            GUILayout.BeginHorizontal();
            EditorGUILayout.MinMaxSlider(content, ref value.x, ref value.y, minLimit, maxLimit);

            GUILayout.Space(2);

            value.x = (float)System.Math.Round(EditorGUILayout.FloatField("", value.x, boldedBox, GUILayout.MaxWidth(45)), 2); // minimum value

            GUILayout.Space(2);

            value.y = (float)System.Math.Round(EditorGUILayout.FloatField("", value.y, boldedBox, GUILayout.MaxWidth(45)), 2); // maximum value

            GUILayout.EndHorizontal();

            return value;
        }

        /// <summary>
        /// Draw an GUI preview of a grid.
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="square"></param>
        public static void DrawGridPreview(int resolution, Texture2D square, int width, int height)
        {
            GUILayout.BeginVertical();
            for (int z = 0; z < resolution; z++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < resolution; x++)
                {
                    GUILayout.Label(square, GUILayout.Width(width), GUILayout.Height(height));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        #region DrawLODsUI
        const int baseLODButtonWidth = 165;
        
        public static FoliageLODLevel[] DrawLODsEditor(FoliageLODLevel[] lods, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical("Box");

            for(int i = 0; i < lods.Length; i++)
            {
                GUILayout.Label("LOD " + i, EditorStyles.boldLabel);

                GUILayout.Space(5);

                lods[i].lodDistance = EditorGUILayout.Slider("LOD Distance:", lods[i].lodDistance, 0, FoliageLODLevel.LOD_MAX_DISTANCE);
                lods[i].lodValue = EditorGUILayout.Slider("LOD Value:", lods[i].lodValue, 0, 1);
                
                GUILayout.Space(10);
            }

            GUILayout.EndVertical();

            return lods;
        }
        #endregion

        #endif
    }

    #if UNITY_EDITOR
    public class SelectionBoxWindow : EditorWindow
    {
        static SelectionBoxWindow _instance;
        public static SelectionBoxWindow instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = GetWindow<SelectionBoxWindow>();
                    _instance.ShowPopup();

                    _instance.maxSize = new Vector2(300, 300);
                    _instance.minSize = _instance.maxSize;
                }

                return _instance;
            }
        }

        static List<BaseSelectionBoxItem> items;
        public static BaseSelectionBoxItem item = null;

        System.DateTime lastTime = System.DateTime.Now;
        bool clickDone
        {
            get
            {
                return (System.DateTime.Now - lastTime).TotalMilliseconds < 200;
            }
        }

        string searchBox = "";
        Vector2 scrollPos;

        public SelectionBoxItems<T> Draw<T>(List<SelectionBoxItems<T>> values, SelectionBoxItems<T> value)
        {
            items = new List<BaseSelectionBoxItem>();

            for(int i = 0; i < values.Count; i++)
            {
                items.Add(values[i]);
            }

            item = value;

            return item as SelectionBoxItems<T>;
        }

        void OnEnable()
        {
            scrollPos = Vector2.zero;
        }

        void OnDisable()
        {
            if(!Application.isPlaying)
            {
                #if UNITY_EDITOR
                #if UNITY_5_3_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                #else
                UnityEditor.EditorApplication.SaveScene();
                #endif
                #endif
            }
        }

        void OnGUI()
        {
            if (items == null)
            {
                Close();
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "Box");
            GUILayout.BeginVertical();

            searchBox = EditorGUILayout.TextField("", searchBox, GUI.skin.FindStyle("ToolbarSeachTextField"));

            GUILayout.Space(5);

            BaseSelectionBoxItem currentItem;
            for(int i = 0; i < items.Count; i++)
            {
                currentItem = items[i];

                if (searchBox == "" || currentItem.itemName == "None" || currentItem.itemName.ToLower().Contains(searchBox.ToLower()))
                {
                    GUI.backgroundColor = item == currentItem ? Color.gray : Color.white;
                    if (GUILayout.Button(currentItem.itemName, EditorStyles.objectFieldThumb))
                    {
                        if (clickDone)
                        {
                            Close();
                        }

                        lastTime = System.DateTime.Now;
                        item = currentItem;
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
#endif

                /// <summary>
                /// Selection box items class which is used when openning the custom selection box.
                /// </summary>
        public class SelectionBoxItems<T> : BaseSelectionBoxItem
    {
        public T item;

        public SelectionBoxItems(string _name, T _item)
        {
            base.itemName = _name;
            this.item = _item;
        }
    }

    public class BaseSelectionBoxItem
    {
        public string itemName;
    }
}

#endif