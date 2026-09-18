using UnityEngine;

// sensor obstacle pake spherecast
// spherecast itu kayak raycast tapi tebel, jadi obstacle yg agak di samping tetep kena
public class SteeringSensor : MonoBehaviour
{
    [Header("Sensor")]
    public float sensorDistance = 3f;     // seberapa jauh ngeliat ke depan
    public float sensorRadius = 0.5f;     // samain sama radius badan npc
    public LayerMask obstacleMask;        // layer yg dianggap obstacle
    public float castHeight = 0.5f;       // offset tinggi titik awal cast dari pivot

    // hasil deteksi frame terakhir, dipake agent sama gizmos
    public bool HasObstacle { get; private set; }
    public RaycastHit LastHit { get; private set; }
    public Vector3 LastDirection { get; private set; } = Vector3.forward;

    Vector3 Origin => transform.position + Vector3.up * castHeight;

    // tembak spherecast ke arah direction
    // balikin vektor hindaran, arahnya menjauh dari obstacle, besarnya 0..1 (makin deket makin gede)
    public Vector3 Sense(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        direction.Normalize();
        LastDirection = direction;

        if (Physics.SphereCast(Origin, sensorRadius, direction, out RaycastHit hit,
                               sensorDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            HasObstacle = true;
            LastHit = hit;

            // arah dari tengah obstacle ke titik tabrak, diratain ke xz
            Vector3 away = hit.point - hit.collider.bounds.center;
            away.y = 0f;

            // ambil yg tegak lurus arah gerak aja biar npc belok, bukan mundur
            Vector3 lateral = Vector3.ProjectOnPlane(away, direction);

            // kalo kena pas di tengah lateralnya nol, ya udah belok kanan aja
            if (lateral.sqrMagnitude < 0.0001f)
                lateral = Vector3.Cross(Vector3.up, direction);

            // tambahin dikit normal permukaan biar ga nempel di tembok
            Vector3 normal = hit.normal; normal.y = 0f;
            Vector3 avoidDir = (lateral.normalized + normal.normalized * 0.5f).normalized;

            // makin deket makin kuat, 1 pas nempel, 0 pas di ujung sensor
            float urgency = 1f - Mathf.Clamp01(hit.distance / sensorDistance);
            return avoidDir * urgency;
        }

        HasObstacle = false;
        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        Vector3 origin = Origin;
        Vector3 dir = Application.isPlaying ? LastDirection : transform.forward;
        float len = (Application.isPlaying && HasObstacle) ? LastHit.distance : sensorDistance;

        // oranye = aman, merah = ada obstacle
        Gizmos.color = HasObstacle ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(origin, sensorRadius);
        Gizmos.DrawLine(origin, origin + dir * len);
        Gizmos.DrawWireSphere(origin + dir * len, sensorRadius);

        if (Application.isPlaying && HasObstacle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(LastHit.point, 0.1f);
        }
    }
}
