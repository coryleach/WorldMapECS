using System;
using Unity.Entities;
using UnityEngine;

public class GameController : MonoBehaviour
{
  public Mesh mesh;
  public Material material;
  public Material[] materials;

  public int _x = 0;
  public int _y = 0;
  public int _width = 10;
  public int _height = 10;
  
  public static GameController instance;

  public Camera _camera;
  
  private Plane ground = new Plane(Vector3.up, Vector3.zero);

  [SerializeField] private int padding = 5;
  
  private void Awake()
  {
    instance = this;
  }

  private EntityManager _entityManager;
  private Entity worldViewEntity;
  private Vector3[] worldCorners = new Vector3[4];
  
  private void Start()
  {
    _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    worldViewEntity = _entityManager.CreateEntity();
    _entityManager.AddComponentData(worldViewEntity, new WorldMapViewData { x = _x, y = _y, width = _width, height = _height});
  }
  
  private void Update()
  {
    if (!GetCorners(worldCorners))
    {
      return;
    }

    var widthBottom = Mathf.CeilToInt(Mathf.Abs(worldCorners[3].x - worldCorners[0].x)) + 1;
    var widthTop = Mathf.CeilToInt(Mathf.Abs(worldCorners[2].x - worldCorners[1].x)) + 1;

    var heightLeft = Mathf.CeilToInt(Mathf.Abs(worldCorners[0].z - worldCorners[1].z)) + 1;
    var heightRight = Mathf.CeilToInt(Mathf.Abs(worldCorners[3].z - worldCorners[2].z)) + 1;

    var x = Mathf.FloorToInt(Mathf.Min(worldCorners[0].x, worldCorners[1].x));
    var y = Mathf.FloorToInt(Mathf.Min(worldCorners[0].z, worldCorners[3].z));

    _x = x - 5;
    _y = y - 5;
    _width = Mathf.Min(Mathf.Max(widthBottom,widthTop) + 5, 1000); //Cap at 100
    _height = Mathf.Min(Mathf.Max(heightLeft,heightRight) + 5, 1000); //Cap at 100
    
    _entityManager.SetComponentData(worldViewEntity, new WorldMapViewData { x = _x, y = _y, width = _width, height = _height});
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawSphere(worldCorners[0],0.25f);
    Gizmos.DrawSphere(worldCorners[1],0.25f);
    Gizmos.DrawSphere(worldCorners[2],0.25f);
    Gizmos.DrawSphere(worldCorners[3],0.25f);
  }

  private bool GetCorners(Vector3[] corners)
  {
    var bottomLeftRay = _camera.ViewportPointToRay(new Vector3(0,0,_camera.nearClipPlane));
    var topLeftRay = _camera.ViewportPointToRay(new Vector3(0,1,_camera.nearClipPlane));

    var topRightRay = _camera.ViewportPointToRay(new Vector3(1,1,_camera.nearClipPlane));
    var bottomRightRay = _camera.ViewportPointToRay(new Vector3(1,0,_camera.nearClipPlane));

    float distance = 0; 
    
    if (!ground.Raycast(bottomLeftRay, out distance))
    {
      return false;
    }
    corners[0] = bottomLeftRay.GetPoint(distance);

    if (!ground.Raycast(topLeftRay, out distance))
    {
      return false;
    }
    corners[1] = topLeftRay.GetPoint(distance);
    
    if (!ground.Raycast(topRightRay, out distance))
    {
      return false;
    }
    corners[2] = topRightRay.GetPoint(distance);
    
    if (!ground.Raycast(bottomRightRay, out distance))
    {
      return false;
    }
    corners[3] = bottomRightRay.GetPoint(distance);

    return true;
  }
  
}
