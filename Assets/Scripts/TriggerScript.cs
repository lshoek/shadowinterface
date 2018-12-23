﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.name == "DeadZone")
        {
            Debug.Log("Deadzone!!!!");
            Application.Instance.StopGame();
        }

        if (col.gameObject.name == "SpikeRing")
        {
            Debug.Log("Spike!!!!");
            Application.Instance.StopGame();
        }
    }
}
