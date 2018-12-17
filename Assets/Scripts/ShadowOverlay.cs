using UnityEngine;

public class ShadowOverlay : MonoBehaviour
{
    private int frameWidth, frameHeight;

	void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("ShadowOverlay");

        Vector3 parentPos = transform.parent.position;
        transform.parent.position = Vector3.zero;

        Vector3 destPos = Camera.main.transform.position;
        destPos.y -= 10.0f;
        transform.position = destPos;

        transform.parent.position = parentPos;

        frameWidth = Application.Instance.DepthManager.GetFrameDescription().Width;
        frameHeight = Application.Instance.DepthManager.GetFrameDescription().Height;

        Vector3 localScale = transform.localScale;
        localScale.x = (frameWidth / (float)frameHeight) * -1.0f;
        transform.localScale = localScale;
    }
}
