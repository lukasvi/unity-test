using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public float runningSpeed;
	public float jumpingSpeed;
	public GameObject missilePrefab;
	public float shootingCoolDown;


	private Rigidbody2D playerRigidbody;
	private float height;
	private bool lookingRight = true;
	private bool shooting = false;

	// Use this for initialization
	void Start () {
		playerRigidbody = gameObject.GetComponent<Rigidbody2D> ();

		height = gameObject.GetComponent<CapsuleCollider2D> ().size.y;



	}

	// add co-routine
	IEnumerator ShootRoutine (){
		if (!shooting) {
			shooting = true;

			Instantiate (missilePrefab, transform.position, Quaternion.Euler (0, 0, lookingRight ? 0 : 180), transform.parent);

			yield return new WaitForSeconds (shootingCoolDown);

			shooting = false;
		} else
			yield return null;
	}

	
	// Update is called once per frame
	void Update () {
		// move the player horizontally
		playerRigidbody.velocity = new Vector2 (Input.GetAxis("Horizontal") * runningSpeed , playerRigidbody.velocity.y);

		if (playerRigidbody.velocity.x > 0)
			lookingRight = true;
		else if (playerRigidbody.velocity.x < 0)
			lookingRight = false;

		//move the player vertically
		RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y -height / 2 - 0.01f), Vector2.down, 0.02f);

		if (Input.GetAxis ("Jump") > 0 && hit.collider != null) {
			playerRigidbody.velocity = new Vector2 (playerRigidbody.velocity.x, jumpingSpeed);
		}

		if (Input.GetKeyDown (KeyCode.LeftControl)) {
			// shooting Missiles
			StartCoroutine (ShootRoutine());
		}

	}

	void OnCollisionEnter2D (Collision2D Collision){
		if (Collision.gameObject.tag == "Enemy")
			Destroy(gameObject);

	}
}
