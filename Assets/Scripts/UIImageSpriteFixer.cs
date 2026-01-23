using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageSpriteFixer : MonoBehaviour
{
    private static Sprite whiteSprite;

    private void Awake()
    {
        if (whiteSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }

        Image image = GetComponent<Image>();
        if (image.sprite == null)
        {
            image.sprite = whiteSprite;
        }
    }
}
