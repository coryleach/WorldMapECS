using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[AlwaysSynchronizeSystem]
public class WorldMapTileDecoratorSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        
        Entities.WithoutBurst().WithAll<WorldMapTileData>().WithNone<RenderMesh>().ForEach((Entity entity, in Translation translation) =>
        {
            commandBuffer.AddSharedComponent(entity, new RenderMesh
            {
                mesh = GameController.instance.mesh,
                material = GameController.instance.materials[Mathf.Clamp((int)translation.Value.y,0,GameController.instance.materials.Length-1)],
                //castShadows = ShadowCastingMode.On,
                //receiveShadows = true
            });
        }).Run();
        
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
        
        return default;
    }
}