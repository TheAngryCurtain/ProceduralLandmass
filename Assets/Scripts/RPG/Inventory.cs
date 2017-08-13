using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public event System.Action OnInventoryChanged;

    [SerializeField] private int m_NumSlots = 10;

    private List<ItemData> m_Inventory = new List<ItemData>();

    private void Awake()
    {
        Instance = this;
    }

    public bool Add(ItemData item)
    {
        if (!item.IsDefault)
        {
            if (m_Inventory.Count < m_NumSlots)
            {
                m_Inventory.Add(item);

                if (OnInventoryChanged != null)
                {
                    OnInventoryChanged();
                }

                return true;
            }
        }

        return false;
    }

    public void Remove(ItemData item)
    {
        m_Inventory.Remove(item);

        if (OnInventoryChanged != null)
        {
            OnInventoryChanged();
        }
    }
}
