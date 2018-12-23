using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;
using System;

public class DepthSourceView : MonoBehaviour
{
    [HideInInspector] public DepthSourceManager DepthSourceManager;

    [Range(-100, 100)] [SerializeField] float meshColliderThreshold = -1.0f;
    [Range(0, 1)] [SerializeField] float depthShadowThreshold = 0.33f;
    [Range(0, 0.1f)] [SerializeField] double depthRescale = 0.01;

    public Camera SimulatedKinectCamera;

    public Transform ColliderMesh;

    private const int KINECTMESH_DOWNSAMPLING = 2;
    private const int COLLIDERMESH_DOWNSAMPLING = 4;
    private const int MAX_DEPTH = 4500;
    private const int NUM_PIXELS = 400;

    private KinectSensor sensor;

    private DepthMesh kinectDepthMesh;
    private DepthMesh colliderDepthMesh;

    private Material blurMaterial;
    private Material depthCopyMaterial;

    private MeshCollider meshCollider;
    private MeshFilter colliderMeshFilter;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Renderer shadowPlaneRenderer;

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

        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        shadowPlaneRenderer = FindObjectOfType<ShadowOverlay>().GetComponent<Renderer>();

        blurMaterial = new Material(Resources.Load("Shaders/Blur13") as Shader);
        depthCopyMaterial = new Material(Resources.Load("Shaders/DepthCopy") as Shader);

        SimulatedKinectCamera.depthTextureMode = DepthTextureMode.Depth;
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            if (!sensor.IsOpen) sensor.Open();

            FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
            activeWidth = fd.Width;
            activeHeight = fd.Height;

            // downsample to lower resolution
            kinectDepthMesh = new DepthMesh(activeWidth / KINECTMESH_DOWNSAMPLING, activeHeight / KINECTMESH_DOWNSAMPLING);
            colliderDepthMesh = new DepthMesh(activeWidth / COLLIDERMESH_DOWNSAMPLING, activeHeight / COLLIDERMESH_DOWNSAMPLING);

            // texture buffers
            simulatedKinectRenderBuffer = new RenderTexture(activeWidth, activeHeight, 16, RenderTextureFormat.Depth);
            simulatedKinectRenderBufferColor = new RenderTexture(activeWidth, activeHeight, 0, RenderTextureFormat.ARGB32);

            digitalKinectDepthTexture = new Texture2D(activeWidth, activeHeight, TextureFormat.ARGB32, false);

            shadowRenderBuffer = new RenderTexture(fd.Width, fd.Height, 0, RenderTextureFormat.ARGB32);
        }
    }

    void Update()
    {
        if (sensor == null) return;
        if (DepthSourceManager == null) return;

        // comment when testing without kinect
        if (!DepthSourceManager.IsNewFrameAvailable()) return;

        // orthogonal raw depth mesh from kinect
        ushort[] rawDepthData = CropRawDepth(DepthSourceManager.GetData(), activeWidth, activeHeight);
        UpdateDepthMesh(kinectDepthMesh, rawDepthData, KINECTMESH_DOWNSAMPLING);

        meshFilter.mesh = kinectDepthMesh.mesh;

        // render mesh from viewpoint of projector
        SimulatedKinectCamera.targetTexture = simulatedKinectRenderBuffer;
        SimulatedKinectCamera.Render();

        // copy depth to color buffer
        depthCopyMaterial.SetTexture("_DepthTex", simulatedKinectRenderBuffer);
        Graphics.Blit(null, simulatedKinectRenderBufferColor, depthCopyMaterial);

        // update collider mesh
        //Graphics.CopyTexture(simulatedKinectRenderBufferColor, digitalKinectDepthTexture); // -> doesnt work????
        RenderTexture.active = simulatedKinectRenderBufferColor;
        digitalKinectDepthTexture.ReadPixels(new Rect(0, 0, activeWidth, activeHeight), 0, 0);
        digitalKinectDepthTexture.Apply();
        RenderTexture.active = null;

        UpdateDepthMeshFromTexture(colliderDepthMesh, digitalKinectDepthTexture, COLLIDERMESH_DOWNSAMPLING);

        colliderMeshFilter.mesh = colliderDepthMesh.mesh;
        meshCollider.sharedMesh = colliderDepthMesh.mesh;

        // apply blur for soft shadows
        blurMaterial.SetColor("_ShadowColor", Application.Instance.Palette.WHITESMOKE);
        Graphics.Blit(simulatedKinectRenderBufferColor, shadowRenderBuffer, blurMaterial);

        shadowPlaneRenderer.material.SetTexture("_MainTex", shadowRenderBuffer);
    }

    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), shadowRenderBuffer);
    }

    private void UpdateDepthMeshFromTexture(DepthMesh depthMesh, Texture2D depthTexture, int downSampleSize)
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

                if (smallIndex >= (activeWidth / downSampleSize) * (activeHeight / downSampleSize)) continue;

                float depth = depthData[fullIndex];
                depthMesh.verts[smallIndex].z = depth;
            }
        }
        depthMesh.Apply();
    }

    private void UpdateDepthMesh(DepthMesh depthMesh, ushort[] depthData, int downSampleSize)
    {
        for (int y = 0; y < activeHeight; y += downSampleSize)
        {
            for (int x = 0; x < activeWidth; x += downSampleSize)
            {
                int idx = x / downSampleSize;
                int idy = y / downSampleSize;

                int smallIndex = (idy * (activeWidth / downSampleSize)) + idx;

                double avg = GetAvg(depthData, x, y);
                avg = avg * depthRescale;

                if (smallIndex >= depthMesh.verts.Length)
                    continue;

                depthMesh.verts[smallIndex].z = (float)avg;
            }
        }

        // fixes camera clipping problems
        Matrix4x4 translateUpwards = Matrix4x4.Translate(new Vector3(0.0f, 0.0f, upwardsTranslation));

        for (int i = 0; i < depthMesh.verts.Length; i++)
            depthMesh.verts[i] = translateUpwards.MultiplyPoint3x4(depthMesh.verts[i]);

        depthMesh.Apply();
    }
    
    private double GetAvg(ushort[] depthData, int x, int y)
    {
        double sum = 0.0;
        int numSamples = 4;

        //if ((x+numSamples) * (y+numSamples) >= depthData.Length)
        //    return depthData[(y * activeWidth) + x];
        
        for (int y1 = y; y1 < y + numSamples; y1++)
        {
            for (int x1 = x; x1 < x + numSamples; x1++)
            {
                int fullIndex = (y1 * activeWidth) + x1;
                if (fullIndex >= depthData.Length) return (y * activeWidth) + x;
                sum += (depthData[fullIndex] == 0) ? MAX_DEPTH : depthData[fullIndex];
            }
        }
        return sum / numSamples*numSamples;
    }

    #region "Helper Methods"
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

    byte[] RawDepthToByteBuffer(ushort[] rawDepth)
    {
        byte[] depthBitmap = new byte[rawDepth.Length];
        for (int i = 0; i < rawDepth.Length; i++)
        {
            int v = rawDepth[i] * 255 / MAX_DEPTH;
            v = (v < 255) ? v : 255;
            v = (v == 0) ? 255 : v;
            depthBitmap[i] = (byte)v;
        }
        return depthBitmap;
    }

    ushort[] RGBDepthToRawDepth(byte[] rgbDepth)
    {
        ushort[] rawDepth = new ushort[rgbDepth.Length / 3];
        for (int i = 0; i < rawDepth.Length; i++)
        {
            int v = rgbDepth[i * 3] * 255 * 255;
            v = (v < 255) ? v : 255;
            v = (v == 0) ? 255 : v;
            rawDepth[i] = (ushort)v;
        }
        return rawDepth;
    }
    #endregion

    void OnApplicationQuit()
    {
        if (sensor != null)
        {
            if (sensor.IsOpen) sensor.Close();
            sensor = null;
        }
    }
}
