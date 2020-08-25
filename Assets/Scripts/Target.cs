using UnityEngine;

public class Target : MonoBehaviour, ITarget {
    [SerializeField] private float gizmoSize = 3f;
    [SerializeField] private Vector3 velocity = Vector3.zero;

    public GameObject GameObject => gameObject;
    public Vector3 WorldPos => transform.position;
    public Vector3 Velocity => velocity;

    private void FixedUpdate() {
        transform.Translate(velocity);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
    }
}