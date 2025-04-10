using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private void OnMouseEnter()
    {
        // Add check to see if player is placing, else highlighting is not needed
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
    }

    private void OnMouseExit()
    {
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
    }
}
