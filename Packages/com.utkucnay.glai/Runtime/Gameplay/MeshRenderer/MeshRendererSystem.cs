using Glai.ECS;
using Glai.ECS.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Glai.Gameplay
{
    public unsafe class MeshRendererSystem : System
    {
        const int k_CubesPerCluster = 1024 * 3;

        static readonly int s_PackedTransformBufferId = Shader.PropertyToID("_PackedTransformBuffer");
        static readonly int s_ClusterSizeId = Shader.PropertyToID("_ClusterSize");
        static readonly ProfilerMarker s_TickMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.Tick");
        static readonly ProfilerMarker s_CopyMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.CopyTransforms");
        static readonly ProfilerMarker s_UploadMarker  = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.UploadTransforms");
        static readonly ProfilerMarker s_DrawMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.Draw");

        static readonly Bounds s_WorldBounds = new Bounds(Vector3.zero, Vector3.one * 1_000_000f);

        Mesh mesh;
        Mesh sourceMesh;
        Material material;
        MaterialPropertyBlock propertyBlock;
        RenderParams renderParams;

        // Each instance stores position plus a snorm16-packed quaternion.
        NativeArray<PackedTransformComponent> uploadBuffer;
        GraphicsBuffer transformBuffer;
        GraphicsBuffer argsBuffer;
        GraphicsBuffer.IndirectDrawIndexedArgs[] drawArgs;
        uint indexCountPerInstance;
        uint startIndex;
        uint baseVertexIndex;
        int capacity;

        public override void Start()
        {
            sourceMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (sourceMesh == null)
            {
                Debug.LogError("Builtin mesh 'Cube.fbx' not found.");
                return;
            }

            mesh = BuildClusterMesh(sourceMesh, k_CubesPerCluster);

            material = Resources.Load<Material>("Material/IndirectDefault");
            if (material == null)
            {
                Debug.LogError("Material 'Material/IndirectDefault' not found.");
                return;
            }

            propertyBlock = new MaterialPropertyBlock();

            EnsureCapacity(1);
            propertyBlock.SetBuffer(s_PackedTransformBufferId, transformBuffer);
            propertyBlock.SetInteger(s_ClusterSizeId, k_CubesPerCluster);

            renderParams = new RenderParams(material)
            {
                worldBounds       = s_WorldBounds,
                matProps          = propertyBlock,
                shadowCastingMode = ShadowCastingMode.On,
                receiveShadows    = true,
            };

            argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);

            drawArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            indexCountPerInstance = mesh.GetIndexCount(0);
            startIndex = mesh.GetIndexStart(0);
            baseVertexIndex = mesh.GetBaseVertex(0);

            drawArgs[0].indexCountPerInstance = indexCountPerInstance;
            drawArgs[0].instanceCount         = 0;
            drawArgs[0].startIndex            = startIndex;
            drawArgs[0].baseVertexIndex       = baseVertexIndex;
            drawArgs[0].startInstance         = 0;
            argsBuffer.SetData(drawArgs);
        }

        public override void Tick(float deltaTime)
        {
            using var _ = s_TickMarker.Auto();

            var entityManager = (EntityManager)IEntityManager.Instance;
            int count = CountRenderables(entityManager);
            if (count == 0)
            {
                drawArgs[0].instanceCount = 0;
                argsBuffer.SetData(drawArgs);
                return;
            }

            int clusterCount = GetClusterCount(count, k_CubesPerCluster);
            int paddedCount = clusterCount * k_CubesPerCluster;

            EnsureCapacity(paddedCount);

            using (s_CopyMarker.Auto())
                CopyTransforms(entityManager, paddedCount);

            using (s_UploadMarker.Auto())
            {
                // Packed transforms are padded to cluster size so one indirect instance expands into a larger grouped mesh.
                transformBuffer.SetData(uploadBuffer, 0, 0, paddedCount);

                drawArgs[0].indexCountPerInstance = indexCountPerInstance;
                drawArgs[0].instanceCount         = (uint)clusterCount;
                drawArgs[0].startIndex            = startIndex;
                drawArgs[0].baseVertexIndex       = baseVertexIndex;
                drawArgs[0].startInstance         = 0;
                argsBuffer.SetData(drawArgs);
            }

            using (s_DrawMarker.Auto())
                Graphics.RenderMeshIndirect(renderParams, mesh, argsBuffer);
        }

        public override void LateTick(float deltaTime) { }

        public override void Dispose()
        {
            if (Disposed)
                return;

            transformBuffer?.Release();
            argsBuffer?.Release();

            if (mesh != null)
                UnityEngine.Object.Destroy(mesh);

            if (uploadBuffer.IsCreated)
                uploadBuffer.Dispose();
            base.Dispose();
        }

        int CountRenderables(EntityManager entityManager)
        {
            int total = 0;
            for (int a = 0; a < entityManager.ArchetypeCount; a++)
            {
                ref var arch = ref entityManager.GetArchetype(a);
                if (!arch.HasAll<MeshRendererComponent, PackedTransformComponent>())
                    continue;
                for (int c = 0; c < arch.ChunkCount; c++)
                    total += arch.GetChunk(c).EntityCount;
            }
            return total;
        }

        void EnsureCapacity(int required)
        {
            if (required <= capacity)
                return;

            int next = Mathf.NextPowerOfTwo(required);

            if (uploadBuffer.IsCreated)
                uploadBuffer.Dispose();
            transformBuffer?.Release();

            uploadBuffer    = new NativeArray<PackedTransformComponent>(next, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            transformBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, next, UnsafeUtility.SizeOf<PackedTransformComponent>());

            propertyBlock?.SetBuffer(s_PackedTransformBufferId, transformBuffer);
            capacity = next;
        }

        static int GetClusterCount(int instanceCount, int clusterSize)
        {
            return Mathf.CeilToInt(instanceCount / (float)clusterSize);
        }

        void CopyTransforms(EntityManager entityManager, int paddedCount)
        {
            int typeId  = Glai.Core.TypeId<PackedTransformComponent>.Id;
            var dst     = (PackedTransformComponent*)NativeArrayUnsafeUtility.GetUnsafePtr(uploadBuffer);
            int written = 0;

            for (int a = 0; a < entityManager.ArchetypeCount; a++)
            {
                ref var arch = ref entityManager.GetArchetype(a);
                if (!arch.HasAll<MeshRendererComponent, PackedTransformComponent>())
                    continue;

                int si = arch.GetComponentStorageIndex(typeId);
                if (si == -1)
                    continue;

                for (int c = 0; c < arch.ChunkCount; c++)
                {
                    ref var chunk = ref arch.GetChunk(c);
                    int n = chunk.EntityCount;
                    if (n == 0)
                        continue;

                    var src = (PackedTransformComponent*)chunk.GetComponentPtr(si);
                    UnsafeUtility.MemCpy(dst + written, src, n * sizeof(PackedTransformComponent));
                    written += n;
                }
            }

            PackedTransformComponent dummy = default;
            dummy.position = new float3(0f, -100000f, 0f);
            dummy.rotation = quaternion.identity;

            for (int i = written; i < paddedCount; i++)
                uploadBuffer[i] = dummy;
        }

        static Mesh BuildClusterMesh(Mesh cubeMesh, int cubesPerCluster)
        {
            Vector3[] sourceVertices = cubeMesh.vertices;
            Vector3[] sourceNormals = cubeMesh.normals;
            Vector4[] sourceTangents = cubeMesh.tangents;
            Vector2[] sourceUvs = cubeMesh.uv;
            int[] sourceIndices = cubeMesh.triangles;

            int sourceVertexCount = sourceVertices.Length;
            int sourceIndexCount = sourceIndices.Length;
            var vertices = new Vector3[sourceVertexCount * cubesPerCluster];
            var normals = new Vector3[sourceNormals.Length * cubesPerCluster];
            var tangents = new Vector4[sourceTangents.Length * cubesPerCluster];
            var uvs = new Vector2[sourceUvs.Length * cubesPerCluster];
            var slotUvs = new Vector2[sourceVertexCount * cubesPerCluster];
            var indices = new int[sourceIndexCount * cubesPerCluster];

            for (int cubeIndex = 0; cubeIndex < cubesPerCluster; cubeIndex++)
            {
                int vertexOffset = cubeIndex * sourceVertexCount;
                int indexOffset = cubeIndex * sourceIndexCount;

                for (int v = 0; v < sourceVertexCount; v++)
                {
                    vertices[vertexOffset + v] = sourceVertices[v];
                    slotUvs[vertexOffset + v] = new Vector2(cubeIndex, 0f);
                }

                for (int v = 0; v < sourceNormals.Length; v++)
                    normals[vertexOffset + v] = sourceNormals[v];

                for (int v = 0; v < sourceTangents.Length; v++)
                    tangents[vertexOffset + v] = sourceTangents[v];

                for (int v = 0; v < sourceUvs.Length; v++)
                    uvs[vertexOffset + v] = sourceUvs[v];

                for (int i = 0; i < sourceIndexCount; i++)
                    indices[indexOffset + i] = sourceIndices[i] + vertexOffset;
            }

            var clusterMesh = new Mesh
            {
                name = $"CubeCluster_{cubesPerCluster}",
                indexFormat = IndexFormat.UInt32,
            };

            clusterMesh.vertices = vertices;
            if (normals.Length > 0)
                clusterMesh.normals = normals;
            if (tangents.Length > 0)
                clusterMesh.tangents = tangents;
            if (uvs.Length > 0)
                clusterMesh.uv = uvs;
            clusterMesh.uv2 = slotUvs;
            clusterMesh.triangles = indices;
            clusterMesh.bounds = cubeMesh.bounds;
            return clusterMesh;
        }
    }
}
