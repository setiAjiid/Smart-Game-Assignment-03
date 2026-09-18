using UnityEngine;

/// <summary>
/// Sensor obstacle berbasis SphereCast.
/// SphereCast = Raycast yang "tebal": sebuah bola dengan radius tertentu
/// ditembakkan ke depan, sehingga obstacle yang sedikit di samping garis
/// tengah pun tetap terdeteksi (sesuai lebar badan NPC).
/// </summary>
public class SteeringSensor : MonoBehaviour
{
    [Header("Sensor")]
    [Tooltip("Jarak maksimum deteksi obstacle di depan NPC")]
    public float sensorDistance = 3f;

    [Tooltip("Radius bola SphereCast (samakan dengan ukuran badan NPC)")]
    public float sensorRadius = 0.5f;

    [Tooltip("Layer yang dianggap obstacle")]
    public LayerMask obstacleMask;

    [Tooltip("Tinggi titik awal cast dari pivot NPC")]
    public float castHeight = 0.5f;

    // Hasil deteksi frame terakhir (dibaca oleh SteeringAgent & Gizmos)
    public bool HasObstacle { get; private set; }
    public RaycastHit LastHit { get; private set; }
    public Vector3 LastDirection { get; private set; } = Vector3.forward;

    Vector3 Origin => transform.position + Vector3.up * castHeight;

    /// <summary>
    /// Menembakkan SphereCast searah <paramref name="direction"/>.
    /// Mengembalikan gaya hindaran (sudah dinormalisasi dan diberi bobot jarak):
    /// - arah: menjauhi pusat obstacle, tegak lurus arah gerak
    /// - besar: 0..1, makin dekat obstacle makin besar
    /// </summary>
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

            // Arah "menjauh": dari pusat obstacle ke titik tabrak, diratakan ke bidang XZ.
            Vector3 away = hit.point - hit.collider.bounds.center;
            away.y = 0f;

            // Ambil komponen yang tegak lurus arah gerak supaya NPC belok, bukan mundur.
            Vector3 lateral = Vector3.ProjectOnPlane(away, direction);

            // Kalau tabrak tepat di tengah (lateral ~ 0), pilih belok kanan.
            if (lateral.sqrMagnitude < 0.0001f)
                lateral = Vector3.Cross(Vector3.up, direction);

            // Tambahkan sedikit komponen normal permukaan agar tidak menempel dinding.
            Vector3 normal = hit.normal; normal.y = 0f;
            Vector3 avoidDir = (lateral.normalized + normal.normalized * 0.5f).normalized;

            // Makin dekat obstacle, makin kuat (1 saat menempel, 0 di ujung sensor).
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
