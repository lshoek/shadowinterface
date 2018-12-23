using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarPlaneDistance : MonoBehaviour
{
    public Vector3 MaxFarPlaneReach;
    private Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
    }
	
    void Update ()
    {
        camera.farClipPlane = (camera.transform.position - MaxFarPlaneReach).magnitude;
	}
}
