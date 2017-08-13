using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField] private Transform m_Target;
    [SerializeField] private Vector3 m_Offset;
    [SerializeField] private float m_Pitch = 2f;

    [SerializeField] private float m_ZoomSpeed = 4f;
    [SerializeField] private float m_MinZoom = 5f;
    [SerializeField] private float m_MaxZoom = 15f;

    [SerializeField] private float m_YawSpeed = 100f;

    private float m_CurrentZoom = 10f;
    private float m_CurrentYaw = 0f;

    private void Update()
    {
        m_CurrentZoom -= Input.GetAxis("Mouse ScrollWheel") * m_ZoomSpeed;
        m_CurrentZoom = Mathf.Clamp(m_CurrentZoom, m_MinZoom, m_MaxZoom);

        m_CurrentYaw -= Input.GetAxis("Horizontal") * m_YawSpeed * Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (m_Target != null)
        {
            transform.position = m_Target.position - m_Offset * m_CurrentZoom;
            transform.LookAt(m_Target.position + Vector3.up * m_Pitch);

            transform.RotateAround(m_Target.position, Vector3.up, m_CurrentYaw);
        }
    }
}
