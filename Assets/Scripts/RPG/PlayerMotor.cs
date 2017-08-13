using UnityEngine;
using UnityEngine.AI;

public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private NavMeshAgent m_Agent;
    [SerializeField] private float m_InsideStopRadiusPercent = 0.75f;
    [SerializeField] private float m_RotateDampeningSpeed = 5f;

    private Transform m_Target;

    private void Update()
    {
        if (m_Target != null)
        {
            MoveToPoint(m_Target.position);
            FaceTarget();
        }
    }

    public void MoveToPoint(Vector3 point)
    {
        m_Agent.SetDestination(point);

#if UNITY_EDITOR
        Debug.DrawLine(point + Vector3.up, point - Vector3.up, Color.red, 10f);
#endif
    }

    public void FollowTarget(Interactable target)
    {
        m_Agent.stoppingDistance = target.Radius * m_InsideStopRadiusPercent;
        m_Agent.updateRotation = false;

        m_Target = target.InteractionTransform;

#if UNITY_EDITOR
        Vector3 stoppingPointToObject = (m_Target.position - transform.position).normalized;
        Debug.DrawLine(m_Target.position - stoppingPointToObject + Vector3.up, m_Target.position - stoppingPointToObject - Vector3.up, Color.green, 10f);
#endif
    }

    public void StopFollowingTarget()
    {
        m_Agent.stoppingDistance = 0f;
        m_Agent.updateRotation = true;

        m_Target = null;
    }

    private void FaceTarget()
    {
        Vector3 direction = (m_Target.position - transform.position).normalized;
        direction.y = 0f;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * m_RotateDampeningSpeed);
    }
}
