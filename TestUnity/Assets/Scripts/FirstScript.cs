using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstScript : MonoBehaviour
{

    int myAge = 20;
    float myMoney = 100.25f;

    // Start is called before the first frame update
    void Start()
    {
        DisplayName();
        Debug.Log("In between");
       
    }

    // Update is called once per frame
    void Update()
    {
    }


    void DisplayName()
    {
        Debug.Log("Mike");
    }
}
