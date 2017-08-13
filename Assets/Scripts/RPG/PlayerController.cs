using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Camera m_Camera;
    [SerializeField]
    private LayerMask m_NavigationMask;
    [SerializeField]
    private int m_RayDistance = 100;

    [SerializeField]
    private PlayerMotor m_PlayerMotor;

    private Ray m_MouseRay;
    private RaycastHit m_HitInfo;

    private Interactable m_CurrentlyFocusedInteractable;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_MouseRay = m_Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(m_MouseRay, out m_HitInfo, m_RayDistance, m_NavigationMask))
            {
                m_PlayerMotor.MoveToPoint(m_HitInfo.point);
                UnfocusInteractable();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            m_MouseRay = m_Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(m_MouseRay, out m_HitInfo, m_RayDistance))
            {
                Interactable interactable = m_HitInfo.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    FocusInteractable(interactable);
                }
            }
        }
    }

    private void FocusInteractable(Interactable i)
    {
        if (m_CurrentlyFocusedInteractable != i)
        {
            if (m_CurrentlyFocusedInteractable != null)
            {
                m_CurrentlyFocusedInteractable.OnUnfocus();
            }

            m_CurrentlyFocusedInteractable = i;
            m_PlayerMotor.FollowTarget(m_CurrentlyFocusedInteractable);
        }

        m_CurrentlyFocusedInteractable.OnFocus(transform);
    }

    private void UnfocusInteractable()
    {
        if (m_CurrentlyFocusedInteractable != null)
        {
            m_CurrentlyFocusedInteractable.OnUnfocus();
        }

        m_CurrentlyFocusedInteractable = null;
        m_PlayerMotor.StopFollowingTarget();
    }
}
