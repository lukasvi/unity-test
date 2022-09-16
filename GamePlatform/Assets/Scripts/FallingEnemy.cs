using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingEnemy : MonoBehaviour {

	public float distance;

	private GameObject player;
	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player");
	}
	
	// Update is called once per frame
	void Update () {
		if (player != null) 
		  if (Mathf.Abs (transform.position.x - player.transform.position.x) < distance)
			gameObject.GetComponent<Rigidbody2D> ().gravityScale = 3f;
	}
}
