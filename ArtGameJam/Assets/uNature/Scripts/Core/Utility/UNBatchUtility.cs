using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using uNature.Core.FoliageClasses;

namespace uNature.Core.Utility
{
    /// <summary>
    /// An utility class for batching items.
    /// </summary>
    public static class UNBatchUtility
    {
        static Vector3 upNormal = new Vector3(0, 1, 0);
        public static int lastID;

        static Mesh _mesh;

        public static void CombineMeshes(List<UNCombineInstance> FoliageInstances, Material _mat, Mesh mesh, FoliagePrototype prototype, bool multiThread, int sortingID, bool applyInstantly, System.Action<UNBatchTask> onDone)
        {
            if (FoliageInstances.Count == 0)
            {
                mesh.Clear(); // clear mesh as its null at the moment.
                return;
            }

            var processTask = new Threading.ThreadTask<UNBatcMeshhProcessingTask>((UNBatcMeshhProcessingTask process) =>
            {
                if (process.instances.Count <= 0) return;

                int instancesCount = process.instances.Count;

                UNMeshData meshData = process.meshData;

                Vector3[] vertices = new Vector3[meshData.verticesLength * instancesCount];
                Vector3[] normals = new Vector3[meshData.normalsLength * instancesCount];
                Vector2[] uv1s = new Vector2[meshData.uv1sLength * instancesCount];
                Vector2[] uv2s = new Vector2[meshData.verticesLength * instancesCount];
                Vector2[] uv3s = new Vector2[meshData.verticesLength * instancesCount];
                Vector2[] uv4s = new Vector2[meshData.verticesLength * instancesCount];
                int[] subMeshes = new int[meshData.trianglesLength * instancesCount];

                int verticesOffset = 0;
                int normalsOffset = 0;
                int uv1sOffset = 0;
                int uv2sOffset = 0;
                int subMeshesOffset = 0;

                for (int i = 0; i < process.instances.Count; i++)
                {
                    MergeMesh(process.instances[i], i, process.material, vertices, normals, uv1s, uv2s, uv3s, uv4s, subMeshes, meshData, verticesOffset, normalsOffset, uv1sOffset, uv2sOffset, subMeshesOffset);

                    verticesOffset += meshData.verticesLength;
                    normalsOffset += meshData.normalsLength;
                    uv1sOffset += meshData.uv1sLength;
                    uv2sOffset += meshData.verticesLength;
                    subMeshesOffset += meshData.trianglesLength;
                }

                UNBatchTask batchTask = new UNBatchTask(vertices, normals, uv1s, uv2s, uv3s, uv4s, subMeshes, process.mesh, process.lastID);
                Threading.ThreadTask<UNBatchTask, bool, System.Action<UNBatchTask>> batckTaskInstance = new Threading.ThreadTask<UNBatchTask, bool, System.Action<UNBatchTask>>((UNBatchTask _batchTask, bool _applyInstantely, System.Action<UNBatchTask> _action) =>
                {
                    _batchTask.Apply();

                    if (_action != null)
                    {
                        _action(_batchTask);
                    }
                }, batchTask, process.applyInstantly, onDone);

                if (process.multiThread)
                {
                    Threading.ThreadManager.instance.RunOnUnityThread(batckTaskInstance);
                }
                else
                {
                    batckTaskInstance.Invoke();
                }

            }, new UNBatcMeshhProcessingTask(FoliageInstances, _mat, mesh, prototype, multiThread, applyInstantly, sortingID));

            if (multiThread) Threading.ThreadManager.instance.RunOnThread(processTask);
            else processTask.Invoke();
        }

        private static void MergeMesh(UNCombineInstance batchInstance, int id, Material material, Vector3[] vertices, Vector3[] normals, Vector2[] uv1s, Vector2[] uv2s, Vector2[] uv3s, Vector2[] uv4s, int[] subMeshes, UNMeshData meshData, int verticesOffset, int normalsOffset, int uv1sOffset, int uv2sOffset, int subMeshesOffset)
        {
            Vector3 centerMesh = Vector3.zero;

            for (int i = 0; i < meshData.verticesLength; i++)
            {
                centerMesh = batchInstance.transform.MultiplyPoint3x4(Vector3.zero);
            }

            for (int i = 0; i < meshData.verticesLength; i++)
            {
                vertices[verticesOffset + i] = centerMesh;
            }

            for (int i = 0; i < meshData.normalsLength; i++)
            {
                normals[normalsOffset + i] = upNormal;
            }
                
            for (int i = 0; i < meshData.uv1sLength; i++)
            {
                uv1s[uv1sOffset + i] = meshData.uv1s[i];
            }

            //add centers
            for (int i = 0; i < meshData.verticesLength; i++)
            {
                uv2s[uv2sOffset + i] = new Vector2(meshData.vertices[i].x, meshData.vertices[i].y);
                uv3s[uv2sOffset + i] = batchInstance.densityOfffset;
                uv4s[uv2sOffset + i] = new Vector2(meshData.vertices[i].z, batchInstance.density);
                //uv4s[uv2sOffset + i] = new Vector2(transformedVertices[i].z - centerMesh.z, batchInstance.density);
            }

            for (int t = 0; t < meshData.trianglesLength; t++)
            {
                subMeshes[subMeshesOffset + t] = meshData.triangles[t] + verticesOffset;
            }
        }
    }

    /// <summary>
    /// An task 
    /// </summary>
    public class UNBatchTask
    {
        public int ID;
        public bool initialized;

        Vector3[] vertices;
        Vector3[] normals;

        Vector2[] uv1s;
        Vector2[] uv2s;
        Vector2[] uv3s;
        Vector2[] uv4s;

        int[] triangles;
        public Mesh mesh;

        public UNBatchTask(Vector3[] vertices, Vector3[] normals, Vector2[] uv1s, Vector2[] uv2s, Vector2[] uv3s, Vector2[] uv4s, int[] triangles, Mesh _mesh, int id)
        {
            this.vertices = vertices;
            this.normals = normals;

            this.uv1s = uv1s;
            this.uv2s = uv2s;
            this.uv3s = uv3s;
            this.uv4s = uv4s;

            this.triangles = triangles;
            this.mesh = _mesh;

            this.ID = id;
            this.initialized = true;
        }

        public void Apply()
        {
            mesh.Clear();

            mesh.vertices = vertices;

            mesh.normals = normals;

            mesh.uv = uv1s;
            mesh.uv2 = uv2s;
            mesh.uv3 = uv3s;
            mesh.uv4 = uv4s;

            mesh.SetTriangles(triangles, 0);
        }
    }

    class UNBatcMeshhProcessingTask
    {
        public List<UNCombineInstance> instances;
        public Mesh mesh;
        public Material material;
        public bool multiThread;
        public bool applyInstantly;

        public FoliagePrototype prototype;

        public UNMeshData meshData;

        public int lastID;

        public UNBatcMeshhProcessingTask(List<UNCombineInstance> _instances, Material _material, Mesh _mesh, FoliagePrototype _prototype, bool _multiThread, bool _applyInstantly, int _lastID)
        {
            instances = _instances;
            material = _material;
            mesh = _mesh;
            multiThread = _multiThread;
            applyInstantly = _applyInstantly;

            prototype = _prototype;

            lastID = _lastID;

            meshData = prototype.FoliageInstancedMeshData.meshData;
        }
    }

    public struct UNCombineInstance
    {
        public Matrix4x4 transform;
        public Mesh mesh;

        public Vector2 densityOfffset;

        public int density;
    }
}
