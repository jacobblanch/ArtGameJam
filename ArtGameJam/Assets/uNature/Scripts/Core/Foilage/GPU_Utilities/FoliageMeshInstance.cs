using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

using uNature.Core.Utility;
using uNature.Core.Sectors;
using uNature.Core.Threading;

namespace uNature.Core.FoliageClasses
{
    public class FoliageMeshInstance
    {
        public static int GENERATION_RANGE_OFFSET(FoliagePrototype prototype)
        {
            return Mathf.Abs(1 - Mathf.CeilToInt(((float)prototype.FoliageGenerationRadius / 2))); // from 3 -> 1
        }

        private static int GENERATION_AMOUNT_PER_RADIUS(int instancesAmount)
        {
            return Mathf.FloorToInt(FoliageCore_MainManager.instance.instancesSectorChunkSize / instancesAmount);
        }

        /// <summary>
        /// Pre generate the matrix4x4 identity to optimize the code as its being generated each frame for each mesh instance.
        /// </summary>

        private readonly static Vector3 GENERATION_OPTIMIZATION_PRE_GENERATED_VECTOR3_ZERO = Vector3.zero;
        private readonly static Quaternion GENERATION_OPTIMIZATION_PRE_GENERATED_QUATERNION_IDENTITY = Quaternion.identity;

        #region Variables
        public FoliagePrototype prototype;

        public Vector3 position;

        public Vector3 GetPosition(Vector3 pos)
        {
            return pos + position;
        }

        private int _maxInstancesPerMesh = -1;
        public int maxInstancesPerMesh
        {
            get
            {
                if (_maxInstancesPerMesh == -1)
                {
                    _maxInstancesPerMesh = CalculatePerMeshInstances(prototype, prototype.maxGeneratedDensity);
                }

                return _maxInstancesPerMesh;
            }
            private set
            {
                _maxInstancesPerMesh = value;
            }
        }

        private FoliageChunk _currentChunk;
        public FoliageChunk currentChunk
        {
            get
            {
                return _currentChunk;
            }
            set
            {
                if (_currentChunk != value)
                {
                    _currentChunk = value;
                }
            }
        }
        #endregion

        public static FoliageMeshInstance CreateFoliageMesh(FoliagePrototype prototype, Vector3 position, int maxInstancesPerMesh)
        {
            FoliageMeshInstance instance = new FoliageMeshInstance();

            instance.prototype = prototype;
            instance.position = position;
            instance.maxInstancesPerMesh = maxInstancesPerMesh;

            return instance;
        }

        public static FoliageMeshInstancesGroup CreateFoliageInstances(int prototypeIndex, int density, FoliageResolutions resolution)
        {
            float resMultiplier = (float)resolution / FoliageCore_MainManager.FOLIAGE_INSTANCE_AREA_SIZE;

            FoliagePrototype prototype = FoliageDB.sortedPrototypes[prototypeIndex];

            int maxGenerationInstancesPerMesh = (int)(CalculatePerMeshInstances(prototype, density) / resMultiplier);

            int generationAmountPerRadius = GENERATION_AMOUNT_PER_RADIUS(maxGenerationInstancesPerMesh);

            FoliageMeshInstancesGroup meshGroup = new FoliageMeshInstancesGroup();

            Vector3 position;

            int gAmountX = generationAmountPerRadius - (int)prototype.meshInstancesGenerationOffset.x;
            if (gAmountX == 0) gAmountX = 1; // dont clamp, it can be minus

            int gAmountZ = generationAmountPerRadius - (int)prototype.meshInstancesGenerationOffset.y;
            if (gAmountZ == 0) gAmountZ = 1; // dont clamp, it can be minus

            for (int x = 0; x < gAmountX; x++)
            {
                for (int z = 0; z < gAmountZ; z++)
                {
                    position = new Vector3(z * maxGenerationInstancesPerMesh, 0, x * maxGenerationInstancesPerMesh);

                    meshGroup.AddMeshInstance(FoliageMeshInstance.CreateFoliageMesh(prototype, position, maxGenerationInstancesPerMesh));
                }
            }

            return meshGroup;
        }

        public static void CreateGPUMesh(FoliagePrototype prototype, Mesh mesh, int density)
        {
            int currentValues = 0;

            List<UNCombineInstance> combineInstances = new List<UNCombineInstance>();
            UNCombineInstance instance;

            int maxPerMeshInstances = CalculatePerMeshInstances(prototype, density);

            float rndX;
            float rndZ;

            Vector3 position;

            for (int x = 0; x < maxPerMeshInstances; x++)
            {
                for (int z = 0; z < maxPerMeshInstances; z++)
                {
                    currentValues++;

                    position.x = x;
                    position.y = prototype.FoliageInstancedMeshData.offset.y;
                    position.z = z;

                    for (int densityIndex = 1; densityIndex <= density; densityIndex++)
                    {
                        rndX = Random.Range(-1f, 1f);
                        rndZ = Random.Range(-1f, 1f);

                        for (int i = 0; i < prototype.FoliageInstancedMeshData.meshes.Length; i++)
                        {
                            instance = new UNCombineInstance()
                            {
                                mesh = prototype.FoliageInstancedMeshData.meshes[i],
                                transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),

                                densityOfffset = new Vector2(rndX, rndZ),

                                density = densityIndex
                            };

                            combineInstances.Add(instance);
                        }

                        //physicsObjects.Add(new UNPhysicsTemplate(position, rndX, rndZ, densityIndex, prototype));
                    }

                    if (currentValues >= prototype.maxFoliageCapability)
                        break;
                }

                if (currentValues >= prototype.maxFoliageCapability)
                    break;
            }

            Utility.UNBatchUtility.CombineMeshes(combineInstances, prototype.FoliageInstancedMeshData.mat, mesh, prototype, false, 0, true, null);

            mesh.name = string.Format("uNature Mesh ({0}) ({1})", density, mesh.vertexCount);

            mesh.bounds = FoliageCore_MainManager.FOLIAGE_MAIN_AREA_BOUNDS;
        }

        /// <summary>
        /// Calculate the amount of permitted mesh instances per mesh.
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        private static int CalculatePerMeshInstances(FoliagePrototype prototype, int generatableDensity)
        {
            int maxInstancesDensed = Mathf.FloorToInt(Mathf.Sqrt((float)prototype.maxFoliageCapability / generatableDensity));

            float flooredChunkSize = (int)FoliageCore_MainManager.instance.instancesSectorChunkSize;

            for (int i = maxInstancesDensed; i > 0; i--)
            {
                if (((flooredChunkSize / i) % 1) == 0)
                {
                    return i;
                }
            }

            return maxInstancesDensed;
        }

        internal void Destroy()
        {
            _currentChunk = null;

            prototype = null;

            System.GC.SuppressFinalize(this);
        }

        internal void DrawAndUpdate(Vector3 position, Mesh mesh, Material mat, Camera camera, Vector3 cameraPos, FoliagePrototype prototype, MaterialPropertyBlock matBlock, bool useQualitySettingsShadows, float shadowDistance)
        {
            ShadowCastingMode castMode = prototype.castShadows && (camera == null || (useQualitySettingsShadows || Vector3.Distance(position, cameraPos) < shadowDistance)) ? ShadowCastingMode.On : ShadowCastingMode.Off;

            Graphics.DrawMesh(mesh, GENERATION_OPTIMIZATION_PRE_GENERATED_VECTOR3_ZERO, GENERATION_OPTIMIZATION_PRE_GENERATED_QUATERNION_IDENTITY, mat, prototype.renderingLayer, camera, 0, matBlock, castMode, prototype.receiveShadows, null);
        }
    }
}
