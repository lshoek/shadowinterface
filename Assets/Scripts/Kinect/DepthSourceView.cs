using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;

public enum DepthViewMode
{
    SeparateSourceReaders,
    MultiSourceReader,
}

public class DepthSourceView : MonoBehaviour
{
    public DepthSourceManager DepthSourceManager;

    [Range(-10, 10)] [SerializeField] float meshColliderThreshold = -1.0f;
    [Range(0, 1)] [SerializeField] float depthShadowThreshold = 0.33f;
    [Range(1, 8000)] [SerializeField] int depthRange = 4000;

    private KinectSensor sensor;
    private CoordinateMapper mapper;
    private Mesh mesh;
    private Vector3[] verts;
    private Vector2[] uv;
    private int[] triangles;

    private Material depthCopyMaterial;
    private Material depthMeshMaterial;
    private MeshCollider meshCollider;
    private Texture2D depthTexture = null;

    private Renderer shadowPlaneRenderer;
    private RenderTexture depthRenderBuffer = null;
    private RenderTextureDescriptor depthRenderBufferDescriptor;

    private byte[] depthBitmapBuffer;

    private const int downSampleSize = 8;
    private const double depthScale = 0.1f;
    private const int speed = 50;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        shadowPlaneRenderer = FindObjectOfType<ShadowOverlay>().GetComponent<Renderer>();

        depthMeshMaterial = gameObject.GetComponent<Renderer>().material;
        depthCopyMaterial = Resources.Load("Materials/DepthCopyMaterial") as Material;

        sensor = KinectSensor.GetDefault();
        if (sensor != null)
        {
            mapper = sensor.CoordinateMapper;
            FrameDescription frameDesc = sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / downSampleSize, frameDesc.Height / downSampleSize);

            if (!sensor.IsOpen)
            {
                sensor.Open();
            }
            depthRenderBufferDescriptor = new RenderTextureDescriptor(frameDesc.Width, frameDesc.Height, RenderTextureFormat.ARGB32, 0);
            depthTexture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.R8, false);
            depthBitmapBuffer = new byte[frameDesc.LengthInPixels];
        }
    }

    void CreateMesh(int width, int height)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().gameObject.GetComponent<MeshFilter>().mesh = mesh;

        verts = new Vector3[width * height];
        uv = new Vector2[width * height];
        triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                verts[index] = new Vector3(x, -y, 0);
                uv[index] = new Vector2((x/width), (y/height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }
        }
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void Update()
    {
        if (sensor == null) return;
        
        float yVal = Input.GetAxis("Horizontal");
        float xVal = -Input.GetAxis("Vertical");

        transform.Rotate(xVal * Time.deltaTime * speed, yVal * Time.deltaTime * speed, 0, Space.Self);
            
        if (DepthSourceManager.gameObject == null) return; 
        if (DepthSourceManager == null) return;

        ushort[] depthData = DepthSourceManager.GetData();

        for (int i = 0; i < depthData.Length; i++)
        {
            int v = depthData[i] * 255 / depthRange;
            v = (v < 255) ? v : 255;
            v = (v == 0) ? 255 : v;
            byte value = (byte)(v);

            depthBitmapBuffer[i] = value;
        }
        depthTexture.LoadRawTextureData(depthBitmapBuffer);
        depthTexture.Apply();

        depthRenderBuffer = RenderTexture.GetTemporary(depthRenderBufferDescriptor);
        depthCopyMaterial.SetFloat("_Threshold", depthShadowThreshold);
        Graphics.Blit(depthTexture, depthRenderBuffer, depthCopyMaterial);
        shadowPlaneRenderer.material.mainTexture = depthRenderBuffer;

        depthMeshMaterial.SetFloat("_Threshold", meshColliderThreshold);

        RefreshData(depthData); 
        meshCollider.sharedMesh = mesh;
    }

    void OnPostRender()
    {
        RenderTexture.ReleaseTemporary(depthRenderBuffer);
    }

    private void RefreshData(ushort[] depthData)
    {
        FrameDescription frameDesc = sensor.DepthFrameSource.FrameDescription;
        
        for (int y = 0; y < frameDesc.Height; y += downSampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += downSampleSize)
            {
                int idx = x / downSampleSize;
                int idy = y / downSampleSize;
                int smallIndex = (idy * (frameDesc.Width / downSampleSize)) + idx;
                
                double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height, downSampleSize);
                
                avg = avg * depthScale;
                
                verts[smallIndex].z = (float)avg;
            }
        }
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
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
        if (mapper != null)
            mapper = null;

        if (sensor != null)
        {
            if (sensor.IsOpen) sensor.Close();
            sensor = null;
        }
    }
}
