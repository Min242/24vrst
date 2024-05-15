using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Varjo.XR;
using TMPro;

public class logSavePose : MonoBehaviour
{
    public GameObject obj;
    public TextMeshPro text;

    public bool isLooking = false;
    public bool isPressing = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        checkGaze();
        checkHand();
        text.text = isLooking.ToString();
        //if (isLooking && isPressing)
        //    text.text = "Both";
    }
    void OnDisable()
    {
        isLooking = false;
        isPressing = false;
    }
    void checkGaze()
    {
        var gaze = VarjoEyeTracking.GetGaze();
        if (gaze.status == VarjoEyeTracking.GazeStatus.Valid)
        {
            Vector3 gazeDirection = transform.TransformDirection(gaze.gaze.forward);
            Vector3 gazeOrigin = transform.TransformPoint(gaze.gaze.origin);
            Debug.DrawLine(gazeOrigin, gazeOrigin + gazeDirection * 100, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(gazeOrigin, gazeDirection, out hit))
                if (hit.transform.gameObject == obj)
                    isLooking = true;
                else
                    isLooking = false;
        }
    }
    void checkHand()
    {
        if (Input.GetKey(KeyCode.Space))
            isPressing = true;
        else
            isPressing = false;
    }
}
