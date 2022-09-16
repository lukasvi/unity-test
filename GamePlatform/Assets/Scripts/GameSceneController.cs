using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour {

	public Text messageText;

	private float resetTimer = 3f;

	public Player player;
	// Use this for initialization
	void Start () {
		messageText.text = "";
	}
	
	// Update is called once per frame
	void Update () {
		if (player == null) {
			messageText.text = "Game Over!";
			resetTimer -= Time.deltaTime;

			if (resetTimer <= 0f)
				SceneManager.LoadScene ("GamePlatform");
		}
	}
}
