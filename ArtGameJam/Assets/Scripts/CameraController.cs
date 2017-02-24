using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public GameObject endPos;
    public float cameraMoveSpeed = 0.01f;
    public Canvas canvas;

	// Use this for initialization
	void Start () {
        canvas.gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
        CameraMover();
        Debug.Log(Time.timeSinceLevelLoad);
    }

    public void CameraMover()
    {
        if (Time.timeSinceLevelLoad > 15)
        transform.position = Vector3.MoveTowards(transform.position, endPos.transform.position, cameraMoveSpeed);

        if (Input.GetKeyDown("x"))
        {
            Time.timeScale = 5;
            cameraMoveSpeed = 1;
        }
            

        if (Input.GetKeyUp("x"))
        {
            Time.timeScale = 1;
            cameraMoveSpeed = 0.01f;
        }

        if (Time.timeSinceLevelLoad > 25)
            canvas.gameObject.SetActive(true);
            
    }
}
