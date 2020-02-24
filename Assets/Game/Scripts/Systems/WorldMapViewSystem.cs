using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct WorldMapViewSystemData : ISystemStateComponentData
{
    public int x;
    public int y;
    public int width;
    public int height;
}

public struct WorldMapTileOutOfView : IComponentData
{
}

public class WorldMapViewSystem : JobComponentSystem
{
    private EntityCommandBufferSystem _bufferSystem;
    private EntityQuery worldMapViewDataQuery;
    private EntityQuery worldMapTiles;
    private EntityQuery outOfViewQuery;
    private EntityQuery viewChangedQuery;
    private EntityArchetype _tileArchetype;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        worldMapViewDataQuery = GetEntityQuery(ComponentType.ReadOnly<WorldMapViewData>());
        worldMapTiles = GetEntityQuery(ComponentType.ReadOnly<WorldMapTileData>());
        _tileArchetype = EntityManager.CreateArchetype(typeof(LocalToWorld),typeof(Translation),typeof(WorldMapTileData),typeof(Rotation));
        outOfViewQuery = GetEntityQuery(typeof(WorldMapTileOutOfView));
        viewChangedQuery = GetEntityQuery(ComponentType.ReadOnly<WorldMapViewData>(), typeof(WorldMapViewSystemData));
        viewChangedQuery.AddChangedVersionFilter(typeof(WorldMapViewData));
    }

    private struct CleanupJob : IJobForEachWithEntity<WorldMapTileData>
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<WorldMapViewData> mapViews;
        public EntityCommandBuffer.Concurrent commandBuffer;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref WorldMapTileData tileData)
        {
            //Check each view to see if tile is in bounds
            for (int i = 0; i < mapViews.Length; i++)
            {
                int minX = mapViews[i].x;
                int minY = mapViews[i].y;
                int maxX = mapViews[i].x + mapViews[i].width;
                int maxY = mapViews[i].y + mapViews[i].height;
                
                //Cleanup tile if it is in view
                int left = tileData.x;
                int right = tileData.x + 1;
                int top = tileData.y + 1;
                int bottom = tileData.y;
            
                //If I am in a rect do nothing
                if ( left >= minX && right <= maxX && bottom >= minY && top <= maxY )
                {
                    return;
                }
            }
            
            commandBuffer.AddComponent<WorldMapTileOutOfView>(index,entity);
        }
    }

    private struct SpawnJob : IJobForEachWithEntity<WorldMapViewSystemData,WorldMapViewData>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public EntityArchetype TileArchetype;
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> OutOfViewTiles;
        
        public void Execute(Entity entity, int index, ref WorldMapViewSystemData oldView, [ReadOnly] ref WorldMapViewData view)
        {
            //Spawn New Entities
            //int minX = view.x;
            //int minY = view.y;
            int maxX = view.x + view.width;
            int maxY = view.y + view.height;

            int oldMinX = oldView.x;
            int oldMinY = oldView.y;
            int oldMaxX = oldView.x + oldView.width;
            int oldMaxY = oldView.y + oldView.height;

            int usedTileIndex = 0;
            
            //For each tile in the new view create it if it wasn't in the old view
            for (int y = view.y; y < maxY; y++ )
            {
                for (int x = view.x; x < maxX; x++)
                {
                    int left = x;
                    int right = x + 1;
                    int top = y + 1;
                    int bottom = y;
                    
                    //If I was in the old rect then continue
                    if ( left >= oldMinX && right <= oldMaxX && bottom >= oldMinY && top <= oldMaxY )
                    {
                        continue;
                    }

                    Entity tile;
                    if (usedTileIndex >= 0 && usedTileIndex < OutOfViewTiles.Length)
                    {
                        tile = OutOfViewTiles[usedTileIndex];
                        CommandBuffer.RemoveComponent<WorldMapTileOutOfView>(index,tile);
                        CommandBuffer.AddComponent<WorldMapTileDirty>(index,tile);
                        usedTileIndex++;
                    }
                    else
                    {
                        tile = CommandBuffer.CreateEntity(index,TileArchetype);
                    }
                    
                    CommandBuffer.SetComponent(index, tile, new WorldMapTileData{x = x, y = y});
                    
                    var pt = new float2(x * 0.01f, y * 0.01f);
                    var position = new float3(x,Mathf.Max(0,Mathf.RoundToInt(noise.snoise(pt) * 4f)),y);
                    CommandBuffer.SetComponent(index, tile, new Translation(){ Value = position});
                }
            }
            
            //Update the view
            oldView.x = view.x;
            oldView.y = view.y;
            oldView.width = view.width;
            oldView.height = view.height;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent();

        //Create New Maps
        var createJob = Entities.WithAll<WorldMapViewData>().WithNone<WorldMapViewSystemData>().ForEach((int nativeThreadIndex, Entity entity) =>
        {
            commandBuffer.AddComponent(nativeThreadIndex, entity, new WorldMapViewSystemData());
        }).Schedule(inputDependencies);
        _bufferSystem.AddJobHandleForProducer(createJob);
        
        //Clean Up Destroyed Maps
        var destroyJob = Entities.WithAll<WorldMapViewSystemData>().WithNone<WorldMapViewData>().ForEach((int nativeThreadIndex, Entity entity) =>
        {
            //Cleanup Destroyed Maps
            commandBuffer.RemoveComponent<WorldMapViewSystemData>(nativeThreadIndex, entity);
        }).Schedule(createJob);
        _bufferSystem.AddJobHandleForProducer(destroyJob);

        var tileArchetype = _tileArchetype;
        
        var cleanupJob = new CleanupJob
        {
            commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent(),
            mapViews = worldMapViewDataQuery.ToComponentDataArray<WorldMapViewData>(Allocator.TempJob)
        };

        var cleanupJobHandle = cleanupJob.Schedule(worldMapTiles, destroyJob);
        _bufferSystem.AddJobHandleForProducer(cleanupJobHandle);

        var spawnJob = new SpawnJob()
        {
            CommandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent(),
            OutOfViewTiles = outOfViewQuery.ToEntityArray(Allocator.TempJob),
            TileArchetype = tileArchetype
        }.Schedule(viewChangedQuery,cleanupJobHandle);

        _bufferSystem.AddJobHandleForProducer(spawnJob);
        
        return spawnJob;
    }
}