using UnityEngine;
using System.Collections;
using Windows.Kinect;

public enum DepthViewMode
{
    SeparateSourceReaders,
    MultiSourceReader,
}

public class DepthSourceView : MonoBehaviour
{
    public GameObject DepthSourceManager;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;
    
    // Only works at 4 right now
    private const int _DownsampleSize = 8;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;
    
    private DepthSourceManager _DepthManager;

    private MeshCollider _Collider;

    void Start()
    {
        _Collider = GetComponent<MeshCollider>();

        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().gameObject.GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2((x/width), (y/height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }

    void Update()
    {
        if (_Sensor == null) return;
        
        float yVal = Input.GetAxis("Horizontal");
        float xVal = -Input.GetAxis("Vertical");

        transform.Rotate(xVal * Time.deltaTime * _Speed, yVal * Time.deltaTime * _Speed, 0, Space.Self);
            
        if (DepthSourceManager == null) return; 
        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();

        if (_DepthManager == null) return;
        RefreshData(_DepthManager.GetData());

        _Collider.sharedMesh = _Mesh;
    }
    
    private void RefreshData(ushort[] depthData)
    {
        FrameDescription frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        
        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                int idx = x / _DownsampleSize;
                int idy = y / _DownsampleSize;
                int smallIndex = (idy * (frameDesc.Width / _DownsampleSize)) + idx;
                
                double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height, _DownsampleSize);
                
                avg = avg * _DepthScale;
                
                _Vertices[smallIndex].z = (float)avg;
            }
        }
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }
    
    private double GetAvg(ushort[] depthData, int x, int y, int width, int height, int dss)
    {
        double sum = 0.0;
        
        for (int y1 = y; y1 < y + 4; y1++)
        {
            for (int x1 = x; x1 < x + 4; x1++)
            {
                int fullIndex = (y1 * width) + x1;
                
                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];
            }
        }
        return sum / 16;
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null)
            _Mapper = null;

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen) _Sensor.Close();
            _Sensor = null;
        }
    }
}
