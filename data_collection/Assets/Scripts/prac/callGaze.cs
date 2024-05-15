using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;
using Leap.Unity.Interaction;
using Leap;
using Leap.Unity;

public enum GazeDataSource
{
    //InputSubsystem,
    GazeAPI
}

public class callGaze : MonoBehaviour
{
    // [Header("Gaze data")]
    // public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;
    public GameObject obj;

    [Header("Default path is Logs under application data path.")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";

    [Header("Print gaze data framerate while logging.")]
    public bool printFramerate = false;

    private List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    private List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;
    private StreamWriter writer = null;
    private bool logging = false;

    private static readonly string[] ColumnNames = { "Frame", "CaptureTime", "LogTime", "HMDPosition", "HMDRotation", "GazeStatus", "CombinedGazeForward", "CombinedGazePosition",
        "InterPupillaryDistanceInMM", "LeftEyeStatus", "LeftEyeForward", "LeftEyePosition", "LeftPupilIrisDiameterRatio", "LeftPupilDiameterInMM", "LeftIrisDiameterInMM",
        "RightEyeStatus", "RightEyeForward", "RightEyePosition", "RightPupilIrisDiameterRatio", "RightPupilDiameterInMM", "RightIrisDiameterInMM", "FocusDistance", "FocusStability",
        "Hand", "Hand Status", "OBJ Position", "TAR position"};
    //[0]Frame, [1]Capture Time, [2]Log Time. [3]HMDPosition, [4]HMDRotation, [5]GazeStatus, [6]CombinedGazeForward, [7]CombinedGazePosition,
    //[8]InterPupillaryDistanceInMM, [9]LeftEyeStatus, [10]LeftEyeForward, [11]LefyEyePosition, [12]LeftPupilIrisDiameterRatio, [13]LeftPupilDiameterInMM, [14]LeftIrisDiameterInMM,
    //[15]RightEyeStatus, [16]RightEyeForward, [17]RightEyePosition, [18]RightPupilIrisDiameterRatio, [19]RightPupilDiameterInMM, [20]RightIrisDiameterInMM, [21]FocusDistance, [22]FocusStability,
    //[23]Hand, [24]Hand Status, [25]OBJ position, [26]TAR position

    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";

    int gazeDataCount = 0;
    float gazeTimer = 0f;

    void Update()
    {
        if (logging && printFramerate)
        {
            gazeTimer += Time.deltaTime;
            if (gazeTimer >= 1.0f)
            {
                Debug.Log("Gaze data rows per second: " + gazeDataCount);
                gazeDataCount = 0;
                gazeTimer = 0f;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!logging)
            {
                StartLogging();
            }
            else
            {
                StopLogging();
            }
            return;
        }

        if (logging)
        {
            int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate, out eyeMeasurementsSinceLastUpdate);
            if (printFramerate) gazeDataCount += dataCount;
            for (int i = 0; i < dataCount; i++)
            {
                LogGazeData(dataSinceLastUpdate[i], eyeMeasurementsSinceLastUpdate[i]);
            }
            Vector3 origin = transform.TransformPoint(dataSinceLastUpdate[0].gaze.origin);
            Vector3 direction = transform.TransformDirection(dataSinceLastUpdate[0].gaze.forward);
            Debug.DrawLine(origin, origin + direction * 100, Color.red);
            //Debug.Log(transform.TransformPoint(dataSinceLastUpdate[0].gaze.origin));
            //Debug.Log(transform.TransformDirection(dataSinceLastUpdate[0].gaze.forward));
        }
    }

    void LogGazeData(VarjoEyeTracking.GazeData data, VarjoEyeTracking.EyeMeasurements eyeMeasurements)
    {
        string[] logData = new string[27];

        // Gaze data frame number
        logData[0] = data.frameNumber.ToString();

        // Gaze data capture time (nanoseconds)
        logData[1] = data.captureTime.ToString();

        // Log time (milliseconds)
        //logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();
        long currentVarjoTimestamp = VarjoTime.GetVarjoTimestamp();
        logData[2] = currentVarjoTimestamp.ToString();

        // HMD
        logData[3] = transform.localPosition.ToString("F3");
        logData[4] = transform.localRotation.ToString("F3");

        // Combined gaze in World Coordinate
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        logData[5] = invalid ? InvalidString : ValidString;
        Vector3 transformGaze = transform.TransformDirection(data.gaze.forward);
        logData[6] = invalid ? "" : transformGaze.ToString("F3");
        Vector3 transformOrigin = transform.TransformPoint(data.gaze.origin);
        logData[7] = invalid ? "" : transformOrigin.ToString("F3");


        // IPD
        logData[8] = invalid ? "" : eyeMeasurements.interPupillaryDistanceInMM.ToString("F3");

        // Left eye
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[9] = leftInvalid ? InvalidString : ValidString;
        logData[10] = leftInvalid ? "" : data.left.forward.ToString("F3");
        logData[11] = leftInvalid ? "" : data.left.origin.ToString("F3");
        logData[12] = leftInvalid ? "" : eyeMeasurements.leftPupilIrisDiameterRatio.ToString("F3");
        logData[13] = leftInvalid ? "" : eyeMeasurements.leftPupilDiameterInMM.ToString("F3");
        logData[14] = leftInvalid ? "" : eyeMeasurements.leftIrisDiameterInMM.ToString("F3");

        // Right eye
        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[15] = rightInvalid ? InvalidString : ValidString;
        logData[16] = rightInvalid ? "" : data.right.forward.ToString("F3");
        logData[17] = rightInvalid ? "" : data.right.origin.ToString("F3");
        logData[18] = rightInvalid ? "" : eyeMeasurements.rightPupilIrisDiameterRatio.ToString("F3");
        logData[19] = rightInvalid ? "" : eyeMeasurements.rightPupilDiameterInMM.ToString("F3");
        logData[20] = rightInvalid ? "" : eyeMeasurements.rightIrisDiameterInMM.ToString("F3");

        // Focus
        logData[21] = invalid ? "" : data.focusDistance.ToString();
        logData[22] = invalid ? "" : data.focusStability.ToString();

        // Calculate gaze direction multiplied by focus distance
        //Vector3 gazeDirection = data.gaze.forward * data.focusDistance;
        // Log the multiplied gaze direction
        //logData[23] = invalid ? "" : gazeDirection.ToString("F3");

        // Hand
        //logData[23] = GetHandPosition(); //Hand
        //logData[23] = "not using";
        //Debug.Log(GetHandPosition());
        //logData[24] = CheckGrasp(); //Hand Status
        //logData[24] = "not using";
        logData[23] = obj.transform.position.ToString();
        logData[24] = obj.transform.rotation.ToString();

        // GameObject Positions
        //Vector3 objPosition = OBJ.transform.position;
        //logData[25] = invalid ? "" : objPosition.ToString();
        //Vector3 tarPosition = TAR.transform.position;
        //logData[26] = invalid ? "" : tarPosition.ToString();

        logData[25] = "not using";
        logData[26] = "not using";

        Log(logData);
    }

    // Write given values in the log file
    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);
    }

    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

        string logPath = useCustomLogPath ? customLogPath : Application.dataPath + "/Logs/";
        Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}-{4:00}", now.Year, now.Month, now.Day, now.Hour, now.Minute);

        string path = logPath + fileName + ".csv";
        writer = new StreamWriter(path);

        Log(ColumnNames);
        Debug.Log("Log file started at: " + path);
    }

    void StopLogging()
    {
        if (!logging)
            return;

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
        logging = false;
        Debug.Log("Logging ended");
    }

    public void OnApplicationQuit()
    {
        StopLogging();
    }
}