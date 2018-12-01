using UnityEngine;

public class ShadowOverlay : MonoBehaviour
{
	void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("ShadowOverlay");

        Vector3 parentPos = transform.parent.position;
        transform.parent.position = Vector3.zero;

        Vector3 destPos = Camera.main.transform.position;
        destPos.y -= 10.0f;
        transform.position = destPos;

        transform.parent.position = parentPos;
    }
	
	void Update()
    {
        ScaleToAspect();
	}

    private void ScaleToAspect()
    {
        Vector3 localScale = transform.localScale;
        localScale.x = Camera.main.aspect * -1;
        transform.localScale = localScale;
    }
}
