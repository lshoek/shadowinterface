using UnityEngine;

public class Application : MonoBehaviour
{
    public static Application Instance { get; private set; }

    [HideInInspector] public Transform WorldParent;
    [HideInInspector] public GameManager GameManager;
    [HideInInspector] public DepthSourceManager DepthManager;
    [HideInInspector] public ColorPalette Palette;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        // scene root of world hierarchy
        WorldParent = GameObject.FindGameObjectWithTag("WorldParent").transform;

        GameManager = FindObjectOfType<GameManager>();
        DepthManager = FindObjectOfType<DepthSourceManager>();
        Palette = gameObject.AddComponent<ColorPalette>();

        UnityEngine.Application.targetFrameRate = 60;
    }

    void Start()
    {
        // remove after debug
        GameManager.StartGame();
    }
}
