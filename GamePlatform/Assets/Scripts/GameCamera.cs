using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour {

	public Player player;
	public float smoothness;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		if (player != null) {

			Vector3 targetPosition = new Vector3 (player.transform.position.x, transform.position.y, transform.position.z);

			transform.position =  Vector3.Lerp (transform.position, targetPosition, smoothness * Time.deltaTime);

		}
	}
}
