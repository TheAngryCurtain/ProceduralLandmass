using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/New Item")]
public class ItemData : ScriptableObject
{
    public string Name = "New Item";
    public Sprite Icon = null;
    public bool IsDefault = false;
}
