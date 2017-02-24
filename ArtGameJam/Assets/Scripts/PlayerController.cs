using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {

	}
	
	// Update is called once per frame
	void Update ()
    {
        PlayerMovement();   
	}

    public void PlayerMovement()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo))
        {
            transform.position = hitInfo.point + new Vector3(0,1,0);
        }
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }
}
