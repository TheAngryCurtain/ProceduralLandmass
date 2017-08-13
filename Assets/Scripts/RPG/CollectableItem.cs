using UnityEngine;

public class CollectableItem : Interactable
{
    [SerializeField] private ItemData m_ItemData;

    public override void Interact()
    {
        base.Interact();

        Collect();
    }

    protected virtual void Collect()
    {
        bool collected = Inventory.Instance.Add(m_ItemData);
        if (collected)
        {
            Destroy(this.gameObject);
        }
    }
}
