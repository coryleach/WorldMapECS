using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct WorldMapViewData : IComponentData
{
    public int x;
    public int y;
    public int width;
    public int height;
}
