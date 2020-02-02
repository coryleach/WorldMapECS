using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class WorldMapSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = Entities.ForEach((Entity entity, ref Translation translation, in WorldMapTileData tileData) =>
        {
            var pt = new float2(tileData.x * 0.01f, tileData.y * 0.01f);
            translation.Value.y = Mathf.Max(0,Mathf.RoundToInt(noise.snoise(pt) * 4f));
        }).Schedule(inputDependencies);
        
        return job;
    }
}