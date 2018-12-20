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

    private const int DOWNSAMPLESIZE = 4;
    private const int MAX_DEPTH = 4500;
    private const int NUM_PIXELS = 400;

    private KinectSensor sensor;

    private DepthMesh kinectMesh;
    private DepthMesh colliderMesh;

    private Material blurMaterial;
    private Material depthCopyMaterial;
    private Material depthMeshMaterial;

    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;
    private Renderer shadowPlaneRenderer;

    private RenderTextureDescriptor depthRenderBufferDescriptor;
    public RenderTexture shadowRenderBuffer;
    public RenderTexture simulatedKinectRenderBuffer, simulatedKinectRenderBufferColor;

    public Texture2D digitalKinectDepthTexture;

    public float upwardsTranslation = 0.0f;

    private int activeWidth;
    private int activeHeight;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        shadowPlaneRenderer = FindObjectOfType<ShadowOverlay>().GetComponent<Renderer>();
        depthMeshMaterial = gameObject.GetComponent<Renderer>().material;

        blurMaterial = new Material(Resources.Load("Shaders/Blur13") as Shader);
        depthCopyMaterial = new Material(Resources.Load("Shaders/DepthCopy") as Shader);

        SimulatedKinectCamera.depthTextureMode = DepthTextureMode.Depth;
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            if (!sensor.IsOpen) sensor.Open();

            FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
            activeWidth = NUM_PIXELS;
            activeHeight = NUM_PIXELS;

            Debug.Log(string.Format("origdepth:{0}x{1}, newdepth:{2}x{3}", fd.Width, fd.Height, activeWidth, activeHeight));

            // downsample to lower resolution
            CreateMeshes((int)(activeWidth / (float)DOWNSAMPLESIZE), (int)(activeHeight / (float)DOWNSAMPLESIZE));

            // texture buffers
            simulatedKinectRenderBuffer = new RenderTexture(activeWidth, activeHeight, 16, RenderTextureFormat.Depth);
            simulatedKinectRenderBufferColor = new RenderTexture(activeWidth, activeHeight, 0, RenderTextureFormat.ARGB32);

            depthRenderBufferDescriptor = new RenderTextureDescriptor(fd.Width, fd.Height, RenderTextureFormat.ARGB32, 0);
            shadowRenderBuffer = new RenderTexture(depthRenderBufferDescriptor);

            // must use RGB24 for reading pixels
            digitalKinectDepthTexture = new Texture2D(activeWidth, activeHeight, TextureFormat.RGB24, false);
        }
    }

    void CreateMeshes(int width, int height)
    {
        kinectMesh = new DepthMesh(width, height);
        colliderMesh = new DepthMesh(width, height);
        GetComponent<MeshFilter>().gameObject.GetComponent<MeshFilter>().mesh = kinectMesh.mesh;
    }

    void Update()
    {
        if (sensor == null) return;
        if (DepthSourceManager == null) return;

        // comment when testing without kinect
        //if (!DepthSourceManager.IsNewFrameAvailable()) return;

        // orthogonal raw depth mesh from kinect
        ushort[] rawDepthData = CropRawDepth(DepthSourceManager.GetData(), activeWidth, activeHeight);
        UpdateDepthMesh(kinectMesh, rawDepthData);

        // render mesh from viewpoint of projector
        depthMeshMaterial.SetFloat("_Threshold", meshColliderThreshold);
        SimulatedKinectCamera.targetTexture = simulatedKinectRenderBuffer;
        SimulatedKinectCamera.Render();

        depthCopyMaterial.SetTexture("_DepthTex", simulatedKinectRenderBuffer);
        Graphics.Blit(null, simulatedKinectRenderBufferColor, depthCopyMaterial);

        // read from texture memory
        RenderTexture.active = simulatedKinectRenderBufferColor;
        digitalKinectDepthTexture.ReadPixels(new Rect(0, 0, activeWidth, activeHeight), 0, 0);
        RenderTexture.active = null;
        digitalKinectDepthTexture.Apply();

        // depth data from simulated kinect
        ushort[] simulatedKinectDepthData = RGBDepthToRawDepth(digitalKinectDepthTexture.GetRawTextureData());
        UpdateDepthMesh(colliderMesh, simulatedKinectDepthData);
        meshCollider.sharedMesh = colliderMesh.mesh;

        // apply blur for soft shadows
        blurMaterial.SetColor("_ShadowColor", Application.Instance.Palette.WHITESMOKE);
        Graphics.Blit(simulatedKinectRenderBufferColor, shadowRenderBuffer, blurMaterial);

        shadowPlaneRenderer.material.SetTexture("_MainTex", shadowRenderBuffer);
    }

    private void UpdateDepthMesh(DepthMesh depthMesh, ushort[] depthData)
    {
        for (int y = 0; y < activeHeight; y += DOWNSAMPLESIZE)
        {
            for (int x = 0; x < activeWidth; x += DOWNSAMPLESIZE)
            {
                int idx = x / DOWNSAMPLESIZE;
                int idy = y / DOWNSAMPLESIZE;

                int smallIndex = (idy * (activeWidth / DOWNSAMPLESIZE)) + idx;

                double avg = GetAvg(depthData, x, y);
                avg = avg * depthRescale;

                if (smallIndex >= (activeWidth / DOWNSAMPLESIZE) * (activeHeight / DOWNSAMPLESIZE))
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

        if ((x+numSamples) * (y+numSamples) >= depthData.Length)
            return depthData[(y * activeWidth) + x];
        
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
        ushort[] croppedDepth = new ushort[newWidth * newHeight];

        FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
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
            byte value = (byte)(v);

            depthBitmap[i] = value;
        }
        return depthBitmap;
    }

    ushort[] RGBDepthToRawDepth(byte[] rgbDepth)
    {
        ushort[] rawDepth = new ushort[rgbDepth.Length / 3];
        for (int i = 0; i < rawDepth.Length; i++)
        {
            rawDepth[i] = Convert.ToUInt16(rgbDepth[i * 3]);
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
