using UnityEngine;
using System.Collections;

namespace uNature.Core.Utility
{
    /// <summary>
    /// Using this class you can paint an brush on the scene.
    /// </summary>
    [ExecuteInEditMode]
    public class UNBrushUtility : MonoBehaviour
    {
        const string brushGOPath = "Brushes/Prefabs/BrushProjector";

        static UNBrushUtility _instance;
        public static UNBrushUtility instance
        {
            get
            {
                if(_instance == null)
                {
                    var instances = GameObject.FindObjectsOfType<UNBrushUtility>();
                    
                    for(int i = 0; i < instances.Length; i++)
                    {
                        DestroyImmediate(instances[i].gameObject);
                    }

                    if(_instance == null)
                    {
                        GameObject go = GameObject.Instantiate(Resources.Load<GameObject>(brushGOPath));
                        go.hideFlags = HideFlags.HideInHierarchy;

                        _instance = go.GetComponent<UNBrushUtility>();

                        if(_instance == null)
                        {
                            _instance = go.AddComponent<UNBrushUtility>();
                        }
                    }
                }

                return _instance;
            }
        }

        static Projector _projector;
        public static Projector projector
        {
            get
            {
                if(_projector == null)
                {
                    _projector = instance.GetComponent<Projector>();
                }

                return _projector;
            }
        }

        /// <summary>
        /// Draw a brush on the scene.
        /// </summary>
        /// <param name="brushTexture">The brush's texture.</param>
        /// <param name="brushColor">The brush's color.</param>
        /// <param name="position">The brush's origin position (for example the camera's position).</param>
        /// <param name="rotation">The brush's origin rotation (for example the camera's rotation).</param>
        /// <param name="brushSize">The brush's size. (Varies from 1 -> 100)</param>
        public void DrawBrush(Texture2D brushTexture, Color brushColor, Vector3 originPosition, Quaternion originRotation, float brushSize)
        {
            projector.enabled = true;

            projector.material.SetTexture("_ShadowTex", brushTexture);

            projector.transform.position = originPosition;
            projector.transform.rotation = originRotation;

            projector.orthographicSize = brushSize;
        }

        /// <summary>
        /// Resize texture by Justin Markwell and Smoke.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
        {
            source.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var nTex = new Texture2D(newWidth, newHeight);
            nTex.hideFlags = HideFlags.HideAndDontSave;
            nTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0, 0);
            nTex.Apply();
            RenderTexture.active = null;
            return nTex;

        }

        void Update()
        {
            projector.enabled = false;
        }
    }
}
