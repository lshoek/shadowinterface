using UnityEngine;
using Windows.Kinect;

public class DepthSourceManager : MonoBehaviour
{   
    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    private ushort[] _Data;

    private bool updateSuccessful = false;
    private FrameDescription frameDescription;

    public FrameDescription GetFrameDescription()
    {
        return frameDescription;
    }

    public ushort[] GetData()
    {
        return _Data;
    }

    public bool IsNewFrameAvailable()
    {
        return updateSuccessful;
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();
        frameDescription = _Sensor.DepthFrameSource.FrameDescription;

        if (_Sensor != null) 
        {
            _Reader = _Sensor.DepthFrameSource.OpenReader();
            _Data = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];
        }
    }
    
    void Update () 
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            
            if (frame != null)
            {
                updateSuccessful = true;
                frame.CopyFrameDataToArray(_Data);
                frame.Dispose();
                frame = null;
            }
            else
            {
                updateSuccessful = false;
            }
        }
    }
    
    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
}
