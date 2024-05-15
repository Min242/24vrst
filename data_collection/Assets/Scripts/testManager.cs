using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
//using System;


[System.Serializable]
public struct OrientationRange
{
    public string nameElement;
    public int max;
    public int min;
}
[System.Serializable]
public struct Obj
{
    public string name;
    public GameObject gameObj;
    public GameObject img;
    public int graspIndex;
    public List<Vector3> orientation;
    //public List<((int, int), (int, int), (int, int))> orientationRange;
    //public List<OrientationRange> orientationRange;
}
public class testManager : MonoBehaviour
{
    [Header("Assign")]
    public int theta;
    public GameObject camera;
    public GameObject targetPoint;
    public GameObject img;
    public TextMeshPro text;
    [Tooltip("logSavePose.cs from main camera. Gets gaze raycast")]
    public logSavePose lsp;

    public List<Obj> Objects;

    [Header("Current Progress")]
    public string currPhase = "Ready";
    public bool isCalibrated = false;
    public bool isLooking = false;
    public bool isPressing = false;
    public bool isTaskDone = false;
    public bool moveOn = false;

    public int index = 0; //trial index( 0- 11 )
    [HideInInspector] public List<Obj> Orders = new List<Obj>(); //shuffled Objects
    [HideInInspector] public GameObject currObj;

    //Events
    [HideInInspector] public UnityEvent Intro;
    [HideInInspector] public UnityEvent Task;
    private Coroutine introRoutine;
    private Coroutine taskRoutine;

    //calibrate()
    [HideInInspector] public Vector3 newOriginPosition;
    [HideInInspector] public Quaternion newOriginRotation;
    [HideInInspector] public Vector3 newImgCenter;

    




    void Start()
    {
        makeTrials();
        Intro.AddListener(delegate { currPhase = "Intro"; });
        Intro.AddListener(delegate { lsp.enabled = true; });
        Intro.AddListener(delegate { introRoutine = StartCoroutine(checkReady()); });
        Intro.AddListener(delegate { Orders[index].img.SetActive(true); });
        Intro.AddListener(delegate { taskRoutine = null; });
        Intro.AddListener(delegate { text.text = "Intro"; });

        Task.AddListener(delegate { currPhase = "Task"; });
        Task.AddListener(delegate { lsp.enabled = false; });
        Task.AddListener(delegate { resetFromLsp(); });
        Task.AddListener(delegate { introRoutine = null; });
        Task.AddListener(delegate { taskRoutine = StartCoroutine(runTrial()); });
        Task.AddListener(delegate { text.text = "Task"; });


    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currPhase == "Ready") //when first eneter, no event assigned
            calibrate(); //invoke Intro
        if (currPhase == "Intro")
        {
            getFromLsp(); //invoke Task            
        }
        if (currPhase == "Task")
        {
            checkGrasp();
        }
    }

    void calibrate()
    {
        if (!isCalibrated)
        {
            newOriginPosition = camera.transform.position + new Vector3(0.1f, 0, 0);
            newOriginRotation = camera.transform.rotation;
            newImgCenter = newOriginPosition + camera.transform.forward * 0.5f;
            img.transform.position = newImgCenter;            
            text.text = "calibrated";
            UnityEngine.Debug.Log("calibrated");
            isCalibrated = true;
        }
        else
            this.Intro.Invoke();
    }
    void getFromLsp()
    {
        isLooking = lsp.isLooking;
        isPressing = lsp.isPressing;
    }
    IEnumerator checkReady()
    {
        yield return new WaitUntil(() => isLooking && isPressing);
        text.text = "wait";
        yield return new WaitForSeconds(Random.Range(1f, 2f));
        if (isLooking && isPressing)
        {
            this.Task.Invoke();
            Orders[index].img.SetActive(false);
        }
        else
        {
            introRoutine = null;
            introRoutine = StartCoroutine(checkReady());
        }            
    }
    void resetFromLsp()
    {
        isLooking = false;
        isPressing = false;
    }

    void makeTrials()
    {
        Orders = Shuffle(Objects);
        //foreach (var ele in Orders)
        //{
        //    Debug.Log(ele.name);
        //}
    }
    List<T> Shuffle<T>(List<T> list)
    {
        System.Random random = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        return list; // Return the shuffled list
    }

    IEnumerator runTrial()
    {
        currObj = Orders[index].gameObj;
        UnityEngine.Debug.Log("Trial: " + index + ", Object: " + Orders[index].name);
        text.text = "Trial: " + index.ToString() + ", Object: " + Orders[index].name.ToString();
        img.SetActive(false);
        //obj position & rotation 조절
        Vector3 direction2Obj = Vector3.ProjectOnPlane(Random.onUnitSphere, new Vector3(0, 0, 1f)).normalized;
        float dImgObj = 0.5f * Mathf.Tan(Mathf.Deg2Rad*theta);
        currObj.transform.position = newImgCenter + direction2Obj * dImgObj;
        Vector3 objPosition = currObj.transform.position;
        Debug.Log("h: " + Vector3.Distance(newImgCenter, currObj.transform.position));
        Debug.Log("l: " + Vector3.Distance(newOriginPosition, currObj.transform.position));
        //Debug.Log("d: " + Vector3.Distance(newOriginPosition, newImgCenter));
        //obj rotation 조절

        //target Point position 조절        
        //반대쪽 원에 targetPoint 위치
        //float dTarObj = 2 * Vector3.Distance(newImgCenter, objPosition);
        //targetPoint.transform.position = objPosition + (newImgCenter - objPosition).normalized * dTarObj;
        //Debug.Log("targetpoint: " + targetPoint.transform.position);
        //Debug.Log("tarH: " + Vector3.Distance(newImgCenter, targetPoint.transform.position));
        //Debug.Log("tarL: " + Vector3.Distance(newOriginPosition, targetPoint.transform.position));
        //objPosition 기준 targetPoint 위치
        float d1 = 2 * Vector3.Distance(newImgCenter, objPosition);
        Vector3 newO = objPosition + (newImgCenter - objPosition).normalized * d1;
        Vector3 direction2tar = Vector3.ProjectOnPlane(Random.onUnitSphere, new Vector3(0, 0, 1f)).normalized;
        targetPoint.transform.position = newO + direction2tar * 0.1f;


        currObj.SetActive(true);
        targetPoint.SetActive(true);
        yield return new WaitUntil(() => isTaskDone && moveOn);
        isTaskDone = false;
        moveOn = false;
        Debug.Log("moving on");
        index++;
        if (index == 11)
        {
            currPhase = "Ready";
            text.text = "End of Order";
            yield break;
        }        
        currObj.SetActive(false);
        targetPoint.SetActive(false);
        img.SetActive(true);
        this.Intro.Invoke();
    }
    void checkGrasp()
    {
        if (Vector3.Distance(targetPoint.transform.position, currObj.transform.position) < 0.15f)
        {
            isTaskDone = true;
            UnityEngine.Debug.Log("Translated");
            text.text = "Translated";
        }
        if (isTaskDone && Input.GetKeyDown(KeyCode.Space))
            moveOn = true;
    }

}
