using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemy : MonoBehaviour {

	public float runningSpeed;
	public float runningDuration;

	private Rigidbody2D enemyRigidbody;

	private bool lookingRight = true;

	// Use this for initialization
	void Start () {
		enemyRigidbody = gameObject.GetComponent<Rigidbody2D> ();
		StartCoroutine (MovementRoutine ());

	}

	IEnumerator MovementRoutine(){
		lookingRight = true;
		yield return new WaitForSeconds (runningDuration / 2);

		lookingRight = false;
		yield return new WaitForSeconds (runningDuration / 2);

		StartCoroutine (MovementRoutine ());

	}

	// Update is called once per frame
	void Update () {
		enemyRigidbody.velocity = new Vector2 (runningSpeed   * (lookingRight ? 1 : -1), enemyRigidbody.velocity.y);
		
	}


}
