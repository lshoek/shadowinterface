using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;

public class KinectImageView : MonoBehaviour
{
    public MultiSourceManager manager;
    public RawImage image;
	
	void Update ()
    {
        //image.texture = manager.GetColorTexture();
    }
}
