﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "TestMesh")
        {
            Debug.Log("Ship dead!!!");
        }


        if (collision.gameObject.name == "Planet")
        {
            Debug.Log("Ship landed!!!");
        }

    }
}
