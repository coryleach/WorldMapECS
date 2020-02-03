using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class WorldMapSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = Entities.WithChangeFilter<WorldMapTileData>().ForEach((Entity entity, ref Translation translation, ref Rotation rotation, in WorldMapTileData tileData) =>
        {
            var pt = new float2(tileData.x * 0.01f, tileData.y * 0.01f);
            translation.Value.x = tileData.x;
            translation.Value.z = tileData.y;
            translation.Value.y = Mathf.Max(0,Mathf.RoundToInt(noise.snoise(pt) * 4f));

            rotation.Value = quaternion.Euler(Mathf.Deg2Rad * 90, 0, 0);

        }).Schedule(inputDependencies);
        
        return job;
    }
}