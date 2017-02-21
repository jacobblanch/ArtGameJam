using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {
    public float thrust;
    public Rigidbody rb;
    float InvisibleTime;
    public GameObject anchor;

	// Use this for initialization
	void Start ()
    {
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        PlayerMovement();
        PlayerAway();
	}

    public void PlayerMovement()
    {
        if (Input.GetKey("w"))
        {
            rb.AddForce(transform.forward * thrust);
        }
        if (Input.GetKey("s"))
        {
            rb.AddForce(transform.forward * -thrust);
        }
        if (Input.GetKey("a"))
        {
            rb.AddForce(transform.right * -thrust);
        }
        if (Input.GetKey("d"))
        {
            rb.AddForce(transform.right * thrust);
        }
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }

    public void PlayerAway()
    {
        if (Vector3.Distance(transform.position, anchor.transform.position) > 35)
        {
            transform.position = anchor.transform.position;
        }
    }
}
