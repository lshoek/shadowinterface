using UnityEngine;
using UnityEngine.UI;

public class KinectViewer : MonoBehaviour
{
    public MultiSourceManager manager;
    public RawImage image;
	
	void Update ()
    {
        image.texture = manager.GetColorTexture();
	}
}
