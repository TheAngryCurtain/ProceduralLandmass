using UnityEngine;

public class Interactable : MonoBehaviour
{
    public float Radius = 2f;
    public Transform InteractionTransform;

    private bool m_IsFocused = false;
    private Transform m_TempPlayerTransform;
    private bool m_HasInteracted = false;

    public virtual void Interact()
    {

    }

    public void OnFocus(Transform player)
    {
        m_IsFocused = true;
        m_TempPlayerTransform = player;
        m_HasInteracted = false;
    }

    public void OnUnfocus()
    {
        m_IsFocused = false;
        m_TempPlayerTransform = null;
        m_HasInteracted = false;
    }

    private void Update()
    {
        if (m_IsFocused && !m_HasInteracted)
        {
            float sqrDistToPlayer = (m_TempPlayerTransform.position - InteractionTransform.position).sqrMagnitude;
            if (sqrDistToPlayer <= Radius * Radius)
            {
                Interact();
                m_HasInteracted = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // this is here, as this gets called in the editor
        if (InteractionTransform == null)
        {
            InteractionTransform = transform;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(InteractionTransform.position, Radius);
    }
}
