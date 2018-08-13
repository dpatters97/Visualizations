using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.Translate (2*Input.GetAxis ("Horizontal"), 2*Input.GetAxis ("Vertical"), 2*Input.GetAxis ("Zoom"));
		////transform.localPosition += new Vector3 (0, 0, Input.GetAxis ("Zoom"));
		//transform.position += new Vector3 (Input.GetAxis ("Horizontal"), Input.GetAxis ("Vertical"), 0);
		transform.RotateAround (Vector3.zero,Vector3.up, 3*Input.GetAxis ("Rotation"));
	}
}
