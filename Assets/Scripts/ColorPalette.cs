using UnityEngine;

public class ColorPalette : MonoBehaviour
{
    public Color GUNMETAL = Color.white;
    public Color LIGHTSALMONPINK = Color.white;
    public Color MELON = Color.white;
    public Color RACKLEY = Color.white;
    public Color WHITESMOKE = Color.white;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#2C2B3C", out GUNMETAL);
        ColorUtility.TryParseHtmlString("#FF9499", out LIGHTSALMONPINK);
        ColorUtility.TryParseHtmlString("#F7BfB4", out MELON);
        ColorUtility.TryParseHtmlString("#568EA3", out RACKLEY);
        ColorUtility.TryParseHtmlString("#F7F0F5", out WHITESMOKE);
    }
}
