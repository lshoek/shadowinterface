using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class DepthSourceView : MonoBehaviour
{
    [HideInInspector] public DepthSourceManager DepthSourceManager;

    [Range(0, 0.1f)] [SerializeField] float depthRescale = 0.01f;

    public Camera SimulatedKinectCamera;

    public Transform ColliderMesh;

    private const int KINECTMESH_DOWNSAMPLING = 2;
    private const int COLLIDERMESH_DOWNSAMPLING = 8;
    private const int MAX_DEPTH = 4500;
    private const int NUM_PIXELS = 400;

    private KinectSensor sensor;

    private DepthMesh kinectDepthMesh;
    private DepthMesh colliderDepthMesh;

    private Material blurMaterial;
    private Material depthCopyMaterial;

    private MeshCollider meshCollider;
    private MeshFilter colliderMeshFilter;
    private MeshFilter meshFilter;

    private RenderTexture simulatedKinectRenderBuffer;
    public RenderTexture simulatedKinectRenderBufferColor;
    public RenderTexture shadowRenderBuffer;

    public Texture2D digitalKinectDepthTexture;

    public float upwardsTranslation = 0.0f;

    private int activeWidth;
    private int activeHeight;

    void Start()
    {
        meshCollider = ColliderMesh.GetComponent<MeshCollider>();
        colliderMeshFilter = ColliderMesh.GetComponent<MeshFilter>();

        meshFilter = GetComponent<MeshFilter>();

        blurMaterial = new Material(Resources.Load("Shaders/Blur5") as Shader);
        depthCopyMaterial = new Material(Resources.Load("Shaders/DepthCopy") as Shader);

        SimulatedKinectCamera.depthTextureMode = DepthTextureMode.Depth;
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            if (!sensor.IsOpen) sensor.Open();

            FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
            activeWidth = NUM_PIXELS;
            activeHeight = NUM_PIXELS;

            // downsample to lower resolution
            kinectDepthMesh = new DepthMesh(activeWidth / KINECTMESH_DOWNSAMPLING, activeHeight / KINECTMESH_DOWNSAMPLING);
            colliderDepthMesh = new DepthMesh(activeWidth / COLLIDERMESH_DOWNSAMPLING, activeHeight / COLLIDERMESH_DOWNSAMPLING);

            // texture buffers
            simulatedKinectRenderBuffer = new RenderTexture(activeWidth, activeHeight, 16, RenderTextureFormat.Depth);
            simulatedKinectRenderBufferColor = new RenderTexture(activeWidth, activeHeight, 0, RenderTextureFormat.ARGB32);
            shadowRenderBuffer = new RenderTexture(fd.Width, fd.Height, 0, RenderTextureFormat.ARGB32);

            digitalKinectDepthTexture = new Texture2D(activeWidth, activeHeight, TextureFormat.ARGB32, false);
        }
        Camera.main.GetComponent<CameraRenderHooks>().OnPreRenderEvent += ProcessDepth;
        Camera.main.GetComponent<CameraRenderHooks>().OnPostRenderEvent += RenderShadowQuad;
    }

    void Update()
    {
        if (!IsFrameValid()) return;

        ushort[] rawDepthData = DepthSourceManager.GetData(); //CropRawDepth(DepthSourceManager.GetData(), activeWidth, activeHeight);
        UpdateDepthMesh(kinectDepthMesh, rawDepthData, depthRescale, KINECTMESH_DOWNSAMPLING);

        meshFilter.mesh = kinectDepthMesh.mesh;
    }

    private void ProcessDepth()
    {
        if (!IsFrameValid()) return;

        // render mesh from viewpoint of projector
        SimulatedKinectCamera.targetTexture = simulatedKinectRenderBuffer;
        SimulatedKinectCamera.Render();

        // copy depth to color buffer
        depthCopyMaterial.SetTexture("_DepthTex", simulatedKinectRenderBuffer);
        Graphics.Blit(null, simulatedKinectRenderBufferColor, depthCopyMaterial); // blit directly to texture2d in unity 2019.1
         
        // apply blur for soft shadows
        blurMaterial.SetColor("_ShadowColor", Application.Instance.Palette.WHITESMOKE);
        Graphics.Blit(simulatedKinectRenderBufferColor, shadowRenderBuffer, blurMaterial);

        // would rather place this in updatecollider task -> does not run on the main thread
        RenderTexture.active = simulatedKinectRenderBufferColor;
        digitalKinectDepthTexture.ReadPixels(new Rect(0, 0, activeWidth, activeHeight), 0, 0);
        digitalKinectDepthTexture.Apply();
        RenderTexture.active = null;

        UpdateDepthMesh(colliderDepthMesh, digitalKinectDepthTexture, 1.0f, COLLIDERMESH_DOWNSAMPLING);

        colliderMeshFilter.mesh = colliderDepthMesh.mesh;
        meshCollider.sharedMesh = colliderDepthMesh.mesh;
    }

    private void RenderShadowQuad()
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix();
        Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), shadowRenderBuffer);
        GL.PopMatrix();
    }

    private void UpdateDepthMesh(DepthMesh depthMesh, ushort[] depthData, float scale, int downSampleSize)
    {
        for (int y = 0; y < activeHeight; y += downSampleSize)
        {
            for (int x = 0; x < activeWidth; x += downSampleSize)
            {
                int idx = x / downSampleSize;
                int idy = y / downSampleSize;

                int fullIndex = y * activeWidth + x;
                int smallIndex = (idy * (activeWidth / downSampleSize)) + idx;

                float depth = depthData[fullIndex] * scale;
                depthMesh.verts[smallIndex].z = depth;
            }
        }
        depthMesh.Apply();
    }

    private void UpdateDepthMesh(DepthMesh depthMesh, Texture2D depthTexture, float scale, int downSampleSize)
    {
        byte[] depthData = depthTexture.GetRawTextureData();
        
        for (int y = 0; y < activeHeight; y += downSampleSize)
        {
            for (int x = 0; x < activeWidth; x += downSampleSize)
            {
                int idx = x / downSampleSize;
                int idy = y / downSampleSize;

                int fullIndex = y * activeWidth*4 + x*4; // ARGB
                int smallIndex = idy * (activeWidth / downSampleSize) + idx;

                float depth = depthData[fullIndex] * scale;
                depthMesh.verts[smallIndex].z = depth;
            }
        }
        depthMesh.Apply();
    }

    ushort[] CropRawDepth(ushort[] orig, int newWidth, int newHeight)
    {
        FrameDescription fd = sensor.DepthFrameSource.FrameDescription;

        if (fd.Width == newWidth && fd.Height == newHeight)
            return orig;

        ushort[] croppedDepth = new ushort[newWidth * newHeight];
        int yOffset = (fd.Height - newHeight) / 2;
        int xOffset = (fd.Width - newWidth) / 2;

        //Only use center of depth data matrix
        for (int y = 0; y < activeHeight; y++)
            for (int x = 0; x < activeWidth; x++)
                croppedDepth[y * activeWidth + x] = orig[(y * fd.Width) + (yOffset * fd.Width) + x + xOffset];

        return croppedDepth;
    }

    private bool IsFrameValid()
    {
        if (sensor == null) return false;
        if (DepthSourceManager == null) return false;
        if (sensor.IsAvailable)
        {
            if (!DepthSourceManager.IsNewFrameAvailable())
                return false;
        }
        return true;
    }

    void OnApplicationQuit()
    {
        if (sensor != null)
        {
            if (sensor.IsOpen) sensor.Close();
            sensor = null;
        }
    }
}
