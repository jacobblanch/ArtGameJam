using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public GameObject endPos;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        CameraMover();
        Debug.Log(Time.timeSinceLevelLoad);
    }

    public void CameraMover()
    {
        if (Time.timeSinceLevelLoad > 15)
        transform.position = Vector3.MoveTowards(transform.position, endPos.transform.position, 0.01f);
    }
}
