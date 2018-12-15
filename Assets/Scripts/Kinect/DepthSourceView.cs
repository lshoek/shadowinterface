using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;

public class DepthSourceView : MonoBehaviour
{
    public DepthSourceManager DepthSourceManager;

    [Range(-20, 10)] [SerializeField] float meshColliderThreshold = -1.0f;
    [Range(0, 1)] [SerializeField] float depthShadowThreshold = 0.33f;
    [Range(0, 0.1f)] [SerializeField] double depthRescale = 0.01;
    [Range(0, 100)] [SerializeField] int speed = 50;
    [Range(0, 1000)] [SerializeField] float contactOffset = 0.0f;

    private const int DOWNSAMPLESIZE = 8;
    private const int MAX_DEPTH = 4500;

    private KinectSensor sensor;
    private CoordinateMapper mapper;
    private Mesh mesh;
    private Vector3[] verts;
    //private Vector2[] uv;
    private int[] triangles;

    private Material blurMaterial;
    private Material depthCopyMaterial;
    private Material depthMeshMaterial;
    private MeshCollider meshCollider;
    public Texture2D depthTexture;

    public Texture2D debugTexture;

    private Renderer shadowPlaneRenderer;
    public RenderTexture copyBuffer;
    public RenderTexture depthRenderBuffer;
    private RenderTextureDescriptor depthRenderBufferDescriptor;

    private ushort[] croppedDepthData;
    private byte[] depthBitmapBuffer;

    private byte[] debugBitmapBuffer;

    private int croppedWidth;
    private int croppedHeight;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        shadowPlaneRenderer = FindObjectOfType<ShadowOverlay>().GetComponent<Renderer>();

        depthMeshMaterial = gameObject.GetComponent<Renderer>().material;
        depthCopyMaterial = Resources.Load("Materials/DepthCopyMaterial") as Material;
        blurMaterial = Resources.Load("Materials/BlurMaterial") as Material;

        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            mapper = sensor.CoordinateMapper;
            if (!sensor.IsOpen) sensor.Open();

            // use full depth resolution for shadow textures
            FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
            depthBitmapBuffer = new byte[fd.LengthInPixels];
            
            // use part of frame
            croppedWidth = (int)(fd.Width*1.0f);
            croppedHeight = (int)(fd.Height*1.0f);

            debugBitmapBuffer = new byte[croppedWidth * croppedHeight];

            Debug.Log(string.Format("origdepth:{0}x{1}, newdepth:{2}x{3}", fd.Width, fd.Height, croppedWidth, croppedHeight));

            // downsample to lower resolution
            CreateMesh(croppedWidth / DOWNSAMPLESIZE, croppedHeight / DOWNSAMPLESIZE);
            croppedDepthData = new ushort[croppedWidth * croppedHeight];

            // texture buffers
            depthRenderBufferDescriptor = new RenderTextureDescriptor(fd.Width, fd.Height, RenderTextureFormat.ARGB32, 0);
            depthRenderBuffer = new RenderTexture(depthRenderBufferDescriptor);
            copyBuffer = new RenderTexture(depthRenderBufferDescriptor);
            depthTexture = new Texture2D(fd.Width, fd.Height, TextureFormat.R8, false);
            debugTexture = new Texture2D(croppedWidth, croppedHeight, TextureFormat.R8, false);
        }
    }

    void CreateMesh(int width, int height)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().gameObject.GetComponent<MeshFilter>().mesh = mesh;

        verts = new Vector3[width * height];
        //uv = new Vector2[width * height];
        triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                verts[index] = new Vector3(x, -y, 0);
                //uv[index] = new Vector2((x/width), (y/height));

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
        //mesh.uv = uv;
        mesh.triangles = triangles;
        //mesh.RecalculateNormals();
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
            int v = (depthData[i] * 255 / MAX_DEPTH);
            v = (v < 255) ? v : 255;
            v = (v == 0) ? 255 : v;
            byte value = (byte)(v);

            depthBitmapBuffer[i] = value;
        }
        depthTexture.LoadRawTextureData(depthBitmapBuffer);
        depthTexture.Apply();

        depthCopyMaterial.SetFloat("_Threshold", depthShadowThreshold);
        depthCopyMaterial.SetColor("_ShadowColor", Application.Instance.Palette.WHITESMOKE);
        Graphics.Blit(depthTexture, copyBuffer, depthCopyMaterial);
        Graphics.Blit(copyBuffer, depthRenderBuffer, blurMaterial);
        shadowPlaneRenderer.material.mainTexture = depthRenderBuffer;

        depthMeshMaterial.SetFloat("_Threshold", meshColliderThreshold);

        FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
        int yOffset = (fd.Height - croppedHeight) / 2;
        int xOffset = (fd.Width - croppedWidth) / 2;

        //Only use center of depth data matrix
        for (int y = 0; y < croppedHeight; y++)
            for (int x = 0; x < croppedWidth; x++)
                croppedDepthData[y * croppedWidth + x] = depthData[(y * fd.Width) + (yOffset * fd.Width) + x + xOffset];

        /// debug
        //for (int i = 0; i < croppedDepthData.Length; i++)
        //{
        //    int v = (croppedDepthData[i] * 255 / MAX_DEPTH);
        //    v = (v < 255) ? v : 255;
        //    v = (v == 0) ? 255 : v;
        //    byte value = (byte)(v);

        //    debugBitmapBuffer[i] = value;
        //}
        //debugTexture.LoadRawTextureData(debugBitmapBuffer);
        //debugTexture.Apply();
        ///

        RefreshData(croppedDepthData);
        meshCollider.sharedMesh = mesh;
        meshCollider.contactOffset = contactOffset;
    }

    private void RefreshData(ushort[] depthData)
    {
        for (int y = 0; y < croppedHeight; y+=DOWNSAMPLESIZE)
        {
            for (int x = 0; x < croppedWidth; x+=DOWNSAMPLESIZE)
            {
                int idx = x / DOWNSAMPLESIZE;
                int idy = y / DOWNSAMPLESIZE;

                int smallIndex = (idy * (croppedWidth / DOWNSAMPLESIZE)) + idx;
                
                double avg = GetAvg(depthData, x, y);
                avg = avg * depthRescale;

                if (smallIndex >= (croppedWidth / DOWNSAMPLESIZE) * (croppedHeight / DOWNSAMPLESIZE))
                    continue;

                verts[smallIndex].z = (float)avg;
            }
        }
        mesh.vertices = verts;
        //mesh.uv = uv;
        mesh.triangles = triangles;
        //mesh.RecalculateNormals();
    }
    
    private double GetAvg(ushort[] depthData, int x, int y)
    {
        double sum = 0.0;
        int numSamples = 4;

        if ((x+numSamples) * (y+numSamples) >= depthData.Length)
            return depthData[(y * croppedWidth) + x];
        
        for (int y1 = y; y1 < y + numSamples; y1++)
        {
            for (int x1 = x; x1 < x + numSamples; x1++)
            {
                int fullIndex = (y1 * croppedWidth) + x1;
                if (fullIndex >= depthData.Length) return (y * croppedWidth) + x;
                sum += (depthData[fullIndex] == 0) ? MAX_DEPTH : depthData[fullIndex];
            }
        }
        return sum / numSamples*numSamples;
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
