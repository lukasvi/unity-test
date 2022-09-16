using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class buttonSubmitOnClick : MonoBehaviour {

    public GameObject submitButton;
    public GameObject createButton;
    public GameObject poiData;

    public GameObject nameInput;
    public GameObject radius;

    public void Button_Onclick()
    {

        string poiName = nameInput.GetComponentInChildren<Text>().text;

        if (poiName == "")
        {
            return;
        }


        
        Handler.markerAddable = false;
        submitButton.SetActive(false);
        createButton.SetActive(true);
        poiData.SetActive(false);

        //save marker data
        //todo

        

        float poiRadius = radius.GetComponent<Slider>().value;
        float lng = Handler.markerPosition.x;
        float lat = Handler.markerPosition.y;
        Debug.Log("POI added:" + poiName + ", long: " + lng + ", lat: " + lat + ", radius: " + poiRadius);


        //delete marker
        OnlineMaps.instance.RemoveAllMarkers();
    }
}
