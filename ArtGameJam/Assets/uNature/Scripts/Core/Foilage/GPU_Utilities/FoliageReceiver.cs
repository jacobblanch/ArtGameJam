using UnityEngine;
using System.Collections.Generic;

using uNature.Core.Utility;

namespace uNature.Core.FoliageClasses
{
    [ExecuteInEditMode]
    public class FoliageReceiver : Threading.ThreadItem
    {
        public readonly static List<FoliageReceiver> FReceivers = new List<FoliageReceiver>();

        #region Variables
        [System.NonSerialized]
        FoliageCore_Chunk[] _neighbors = null;
        public FoliageCore_Chunk[] neighbors
        {
            get
            {
                if(_neighbors == null)
                {
                    _neighbors = new FoliageCore_Chunk[9];

                    _neighbors = UNStandaloneUtility.GetFoliageChunksNeighbors(transform.position - FoliageCore_MainManager.instance.transform.position, _neighbors);
                }

                return _neighbors;
            }
        }

        public FoliageCore_Chunk middleFoliageChunkFromNeighbors
        {
            get
            {
                return neighbors[4];
            }
        }

        [SerializeField]
        protected float checkDistance = 5f;

        private Vector3 lastCheckedPosition;
        private bool wasPositionChecked = false;

        [System.NonSerialized]
        FoliageChunk _latestChunk;
        public FoliageChunk latestChunk
        {
            get
            {
                return _latestChunk;
            }
            private set
            {
                if(_latestChunk != value)
                {
                    var oldChunk = _latestChunk;

                    _latestChunk = value;

                    OnLastFoliageChunkChanged(oldChunk, value);
                }
            }
        }

        [SerializeField]
        Camera _playerCamera;
        public Camera playerCamera
        {
            get
            {
                if (_playerCamera == null)
                {
                    _playerCamera = GetComponentInChildren<Camera>();
                }

                return _playerCamera;
            }
        }

        public bool isGrassReceiver = true;
        #endregion

        #region Interactions
        InteractionMap _interactionMap = null;
        public InteractionMap interactionMap
        {
            get
            {
                if(_interactionMap == null)
                {
                    _interactionMap = InteractionMap.CreateMap(this);

                    for(int i = 0; i < FoliageDB.unSortedPrototypes.Count; i++)
                    {
                        FoliageDB.unSortedPrototypes[i].FoliageInstancedMeshData.mat.SetTexture("_InteractionMap", _interactionMap.map);
                    }
                }

                return _interactionMap;
            }
        }

        [SerializeField]
        private InteractionResolutions _interactionMapResolution = InteractionResolutions._128;
        public InteractionResolutions interactionMapResolution
        {
            get
            {
                return _interactionMapResolution;
            }
            set
            {
                if(_interactionMapResolution != value)
                {
                    _interactionMapResolution = value;

                    _interactionMap = null;

                    _interactionMapResolutionIntegral = -1;
                }
            }
        }

        [System.NonSerialized]
        private int _interactionMapResolutionIntegral = -1;
        public int interactionMapResolutionIntegral
        {
            get
            {
                if(_interactionMapResolutionIntegral == -1)
                {
                    _interactionMapResolutionIntegral = (int)interactionMapResolution;
                }

                return _interactionMapResolutionIntegral;
            }
        }
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            FReceivers.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            FReceivers.Remove(this);
        }

        protected override void Update()
        {
            if (!Application.isPlaying) return;

            base.Update();

            FoliageCore_Chunk midChunk = middleFoliageChunkFromNeighbors; // middle chunk
            FoliageManagerInstance mInstance;

            if ((!wasPositionChecked || Vector3.Distance(lastCheckedPosition, transform.position) >= checkDistance) && isGrassReceiver && midChunk != null && midChunk.isFoliageInstanceAttached)
            {
                //latestChunk = FoliageManager.instance.sector.getChunk(new Vector3(transform.position.x - FoliageManager.instance.transform.position.x, 0, transform.position.z - FoliageManager.instance.transform.position.z), 0) as FoliageChunk;
                mInstance = midChunk.GetOrCreateFoliageManagerInstance();

                latestChunk = mInstance.sector.getChunk(new Vector3(transform.position.x - mInstance.transform.position.x, 0, transform.position.z - mInstance.transform.position.z), 0) as FoliageChunk;
                _neighbors = UNStandaloneUtility.GetFoliageChunksNeighbors(transform.position - FoliageCore_MainManager.instance.transform.position, _neighbors);

                wasPositionChecked = true;
                lastCheckedPosition = transform.position;
            }
        }

        protected virtual void OnLastFoliageChunkChanged(FoliageChunk oldChunk, FoliageChunk newChunk)
        {
            interactionMap.RecalculateInteractions(this);
        }

        public static void CallInteractionsRefresh()
        {
            FoliageReceiver receiver;
            for(int i = 0; i < FReceivers.Count; i++)
            {
                receiver = FReceivers[i];

                receiver.interactionMap.RecalculateInteractions(receiver);
            }
        }

        public static List<FoliageReceiver> GetRelevantReceivers(BaseInteraction interaction)
        {
            List<FoliageReceiver> relevantReceivers = new List<FoliageReceiver>();
            FoliageReceiver receiver;

            for (int i = 0; i < FReceivers.Count; i++)
            {
                receiver = FReceivers[i];

                if (Vector3.Distance(receiver.threadPositionDepth, interaction.threadPositionDepth) <= receiver.interactionMap.radius)
                {
                    relevantReceivers.Add(receiver);
                }
            }

            return relevantReceivers;
        }
    }

    /// <summary>
    /// Channels:
    /// R: Wind Direction
    /// G: Grass Offset X (Touch bending)
    /// B: Grass Offset Z (Touch bending)
    /// A: Saved for custom work.
    /// </summary>
    public class InteractionMap : UNMap
    {
        static Color32 _defaultColor;
        static Color32 defaultColor
        {
            get
            {
                if(_defaultColor == Color.black)
                {
                    _defaultColor = new Color32(1, 0, 0, 0);
                }

                return _defaultColor;
            }
        }

        public int radius;

        public int areaSize;
        public int areaResolution;

        private float transformMultiplier = -1;

        public static InteractionMap CreateMap(FoliageReceiver receiver)
        {
            if (FoliageCore_MainManager.instance == null) return null;

            int size = receiver.interactionMapResolutionIntegral;
            
            Texture2D map = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[size * size];

            for(int i = 0; i < size * size; i++)
            {
                pixels[i] = defaultColor;
            }

            map.SetPixels32(pixels);
            map.Apply();

            return new InteractionMap(map, map.GetPixels32(), (int)FoliageCore_MainManager.instance.instancesSectorChunkSize);
        }

        public float TransformCord(float cord)
        {
            return cord / transformMultiplier;
        }

        public Vector2 TransformCord(Vector2 cord)
        {
            return new Vector2(TransformCord(cord.x), TransformCord(cord.y));
        }

        public float InverseTransformCord(float cord)
        {
            return cord * transformMultiplier;
        }

        private InteractionMap(Texture2D texture, Color32[] pixels, int areaSize) : base(texture, pixels, null)
        {
            this.areaSize = areaSize;

            this.areaResolution = mapWidth;
            this.radius = areaSize / 2;

            transformMultiplier = (float)areaSize / areaResolution;
        }

        public void RecalculateInteractions(FoliageReceiver receiver)
        {
            Clear(false, defaultColor);
            var relevantInteractions = BaseInteraction.GetRelevantInteractions(receiver);

            for(int i = 0; i < relevantInteractions.Count; i++)
            {
                relevantInteractions[i].UpdateInteraction(receiver);
            }

            SetPixels32();
        }
    }

    public enum InteractionResolutions
    {
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024
    }
}
