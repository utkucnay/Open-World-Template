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
        static readonly ProfilerMarker s_TickMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.Tick");
        static readonly ProfilerMarker s_CopyMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.CopyTransforms");
        static readonly ProfilerMarker s_UploadMarker  = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.UploadTransforms");
        static readonly ProfilerMarker s_DrawMarker    = new ProfilerMarker(ProfilerCategory.Scripts, "MeshRendererSystem.Draw");

        MeshRendererSystemConfig config;
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
        int packedTransformBufferId;
        int clusterSizeId;
        int boundsUpdateFrame;
        Bounds meshBounds;

        public MeshRendererSystem() : this(MeshRendererSystemConfig.Default)
        {
        }

        public MeshRendererSystem(MeshRendererSystemConfig config)
        {
            this.config = config;
        }

        public override void Start()
        {
            packedTransformBufferId = Shader.PropertyToID(config.PackedTransformBufferProperty);
            clusterSizeId = Shader.PropertyToID(config.ClusterSizeProperty);

            sourceMesh = Resources.GetBuiltinResource<Mesh>(config.MeshResourcePath);
            if (sourceMesh == null)
            {
                Debug.LogError($"Builtin mesh '{config.MeshResourcePath}' not found.");
                return;
            }

            mesh = BuildClusterMesh(sourceMesh, config.CubesPerCluster);
            meshBounds = sourceMesh.bounds;

            material = Resources.Load<Material>(config.MaterialResourcePath);
            if (material == null)
            {
                Debug.LogError($"Material '{config.MaterialResourcePath}' not found.");
                return;
            }

            material.EnableKeyword(config.YAxisRotationKeyword);

            propertyBlock = new MaterialPropertyBlock();

            EnsureCapacity(config.InitialCapacity);
            propertyBlock.SetBuffer(packedTransformBufferId, transformBuffer);
            propertyBlock.SetInteger(clusterSizeId, config.CubesPerCluster);

            renderParams = new RenderParams(material)
            {
                worldBounds       = new Bounds(Vector3.zero, Vector3.one * config.WorldBoundsSize),
                matProps          = propertyBlock,
                shadowCastingMode = config.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows    = config.ReceiveShadows,
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

            int clusterCount = GetClusterCount(count, config.CubesPerCluster);
            int paddedCount = clusterCount * config.CubesPerCluster;

            EnsureCapacity(paddedCount);
            bool updateBounds = ShouldUpdateBounds();

            using (s_CopyMarker.Auto())
                CopyTransforms(entityManager, paddedCount, updateBounds);

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

            propertyBlock?.SetBuffer(packedTransformBufferId, transformBuffer);
            capacity = next;
        }

        static int GetClusterCount(int instanceCount, int clusterSize)
        {
            return Mathf.CeilToInt(instanceCount / (float)clusterSize);
        }

        bool ShouldUpdateBounds()
        {
            if (config.BoundsUpdateIntervalFrames <= 0)
                return false;

            return boundsUpdateFrame++ % config.BoundsUpdateIntervalFrames == 0;
        }

        void CopyTransforms(EntityManager entityManager, int paddedCount, bool updateBounds)
        {
            int typeId  = Glai.Core.TypeId<PackedTransformComponent>.Id;
            var dst     = (PackedTransformComponent*)NativeArrayUnsafeUtility.GetUnsafePtr(uploadBuffer);
            int written = 0;
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);

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

                    if (updateBounds)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            float3 position = src[i].position;
                            min = math.min(min, position);
                            max = math.max(max, position);
                        }
                    }

                    written += n;
                }
            }

            if (updateBounds && written > 0)
                renderParams.worldBounds = CalculateWorldBounds(min, max, meshBounds);

            PackedTransformComponent dummy = default;
            dummy.position = new float3(0f, config.DummyHiddenY, 0f);
            dummy.rotation = quaternion.identity;

            for (int i = written; i < paddedCount; i++)
                uploadBuffer[i] = dummy;
        }

        internal static Bounds CalculateWorldBounds(float3 minPosition, float3 maxPosition, Bounds sourceBounds)
        {
            Vector3 min = minPosition + (float3)sourceBounds.min;
            Vector3 max = maxPosition + (float3)sourceBounds.max;

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
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
