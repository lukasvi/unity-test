using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour {

	public float lifetime = 2f;
	public float speed = 13f;

	private Rigidbody2D missileRigidBody;

	// Use this for initialization
	void Start () {
		Destroy (gameObject, lifetime);
		missileRigidBody = gameObject.GetComponent<Rigidbody2D> ();
	}
	
	// Update is called once per frame
	void Update () {
		missileRigidBody.velocity = transform.right * speed;
	}

	void OnTriggerEnter2D (Collider2D otherColider){
		if (otherColider.gameObject.tag == "Enemy") {
			Destroy (gameObject);  // destroy missile
			Destroy (otherColider.gameObject); // destroy the enemy
		}
	}
}
