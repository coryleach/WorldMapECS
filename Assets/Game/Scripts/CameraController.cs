using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
  [SerializeField] private Camera _camera;


  [SerializeField] private Vector3 velocity;
  
  [SerializeField] private float dampTime = 2f;
  
  private bool dragging = false;
  private Plane ground = new Plane(Vector3.up, Vector3.zero);
  private float time = 0;
  private Vector3 previousPosition;

  private void Update()
  {
    if (Input.GetMouseButtonDown(0))
    {
      dragging = true;
      previousPosition = Input.mousePosition;
    }

    if (Input.GetMouseButtonUp(0))
    {
      dragging = false;
    }
  }

  private void LateUpdate()
  {
    if (dragging)
    {
      time = 0;

      var previousRay = _camera.ScreenPointToRay(previousPosition);
      var newRay = _camera.ScreenPointToRay(Input.mousePosition);

      if (ground.Raycast(previousRay, out var prevDistance) && ground.Raycast(newRay, out var newDistance))
      {
        var prevPt = previousRay.GetPoint(prevDistance);
        var newPt = newRay.GetPoint(newDistance);
        var delta = prevPt - newPt;
        delta.y = 0;
        transform.position += delta;
        velocity = delta;
      }

      previousPosition = Input.mousePosition;
    }
    else if ( time < dampTime )
    {
      time += Time.smoothDeltaTime;
      velocity = Vector3.Lerp(velocity, Vector3.zero, Mathf.InverseLerp(0,dampTime,time));
      transform.position += velocity;
    }
  }

  [Serializable]
  public class Momentum3
  {
    [SerializeField] 
    private Vector3 velocity;
  
    [SerializeField] 
    private float dampTime = 2f;

    private float time = 0;

    public Action<Vector3> OnValueChanged = null;
    
    public void SetVelocity(Vector3 velocity)
    {
      this.velocity = velocity;
      time = 0;
    }

    public void Update(float deltaTime)
    {
      if ( time > dampTime )
      {
        return;
      }
      time += deltaTime;
      velocity = Vector3.Lerp(velocity, Vector3.zero, Mathf.InverseLerp(0,dampTime,time));
      OnValueChanged?.Invoke(velocity);
    }

    public void Stop()
    {
      time = dampTime;
      velocity = Vector3.zero;
    }
  }
  
}