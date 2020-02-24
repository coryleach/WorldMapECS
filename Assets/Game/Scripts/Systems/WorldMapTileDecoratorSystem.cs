using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Scripting;

public struct WorldMapTileDirty : IComponentData
{
}

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

        Entities.WithoutBurst().WithAll<WorldMapTileDirty>().ForEach((Entity entity, RenderMesh renderMesh, in Translation translation) =>
        {
            renderMesh.mesh = GameController.instance.mesh;
            renderMesh.material =
                GameController.instance.materials[
                    Mathf.Clamp((int) translation.Value.y, 0, GameController.instance.materials.Length - 1)];
            commandBuffer.RemoveComponent<WorldMapTileDirty>(entity);
            commandBuffer.SetSharedComponent(entity,renderMesh);
        }).Run();

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
        
        return default;
    }
}