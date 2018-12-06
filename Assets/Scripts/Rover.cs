﻿using UnityEngine;
using System.Collections.Generic;

public class Rover : MonoBehaviour {

    public float speed = 20f;

    Rigidbody roverRb;


    public Vector3 Position { get { return roverRb.position; } }

    void Start()
    {
        roverRb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //roverRb.AddForce(speed,0,0);


        Vector3 offset = Application.Instance.GravityBody.Position - transform.position;
        Vector3 dir = offset / offset.sqrMagnitude * roverRb.mass * 2.0f;
        Vector3 perpdir = Quaternion.AngleAxis(-90, Vector3.down) * Vector3.Normalize(dir) * speed;

        roverRb.AddForce(perpdir);

        Debug.Log(roverRb.velocity);
                 
        Debug.DrawLine(transform.position, transform.position + perpdir * 0.1f);
    }

}