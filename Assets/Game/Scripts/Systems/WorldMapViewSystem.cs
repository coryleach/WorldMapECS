using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using static Unity.Mathematics.math;

public struct WorldMapViewSystemData : ISystemStateComponentData
{
    public int x;
    public int y;
    public int width;
    public int height;
}

public class WorldMapViewSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem _bufferSystem;
    private EntityQuery worldMapViewDataQuery;
    private EntityQuery worldMapTiles;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        worldMapViewDataQuery = GetEntityQuery(ComponentType.ReadOnly<WorldMapViewData>());
        worldMapTiles = GetEntityQuery(ComponentType.ReadOnly<WorldMapTileData>());
    }
    
    public struct CleanupJob : IJobForEachWithEntity<WorldMapTileData>
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

            commandBuffer.DestroyEntity(index,entity);
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

        //Spawn New Tiles
        var spawnJob = Entities.WithChangeFilter<WorldMapViewData>().ForEach((int nativeThreadIndex, Entity entity, ref WorldMapViewSystemData oldView, in WorldMapViewData view) =>
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
                    if (left >= oldMinX && right <= oldMaxX && bottom >= oldMinY && top <= oldMaxY )
                    {
                        continue;
                    }

                    var tile = commandBuffer.CreateEntity(nativeThreadIndex);
                    commandBuffer.AddComponent(nativeThreadIndex, tile, new LocalToWorld());
                    commandBuffer.AddComponent(nativeThreadIndex, tile, new Translation{ Value = float3(x+0.5f,0,y+0.5f)});
                    commandBuffer.AddComponent(nativeThreadIndex, tile, new WorldMapTileData{x = x, y = y});
                }
            }
            
            //Update the view
            oldView.x = view.x;
            oldView.y = view.y;
            oldView.width = view.width;
            oldView.height = view.height;

        }).Schedule(destroyJob);
        _bufferSystem.AddJobHandleForProducer(spawnJob);

        var cleanupJob = new CleanupJob
        {
            commandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent(),
            mapViews = worldMapViewDataQuery.ToComponentDataArray<WorldMapViewData>(Allocator.TempJob)
        };

        var cleanupJobHandle = cleanupJob.Schedule(worldMapTiles, spawnJob);
        
        _bufferSystem.AddJobHandleForProducer(cleanupJobHandle);
        
        return cleanupJobHandle;
    }
}