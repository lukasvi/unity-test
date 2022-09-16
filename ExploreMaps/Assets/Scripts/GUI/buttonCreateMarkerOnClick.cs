using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class buttonCreateMarkerOnClick : MonoBehaviour {

    // Use this for initialization

    
    public GameObject createButton;
    

    

    public void Button_Onclick()
    {
        Debug.Log("Button Create clicked");
        Handler.markerAddable = true;
        
        createButton.SetActive(false);
        

    }
}
