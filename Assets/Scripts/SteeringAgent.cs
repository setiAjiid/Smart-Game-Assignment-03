using UnityEngine;

/// <summary>
/// Autonomous Steering Agent.
///
/// Pipeline tiap frame:
///   Steering Behavior (Arrive / Wander / Flee)
///     -> Desired Velocity (+ Avoidance * weight)
///     -> Steering = Desired - Velocity, dibatasi Max Acceleration
///     -> Velocity += Steering * dt, dibatasi Max Speed
///     -> Movement  (position += Velocity * dt)
///     -> Rotation  (menghadap arah Velocity)
/// </summary>
[RequireComponent(typeof(SteeringSensor))]
public class SteeringAgent : MonoBehaviour
{
    public enum BehaviorState { Idle, Arrive, Wander, Avoiding, Flee }

    [Header("Target")]
    public Transform target;
    [Tooltip("true = Arrive ke target, false = Wander")]
    public bool useTarget = true;

    [Header("Movement")]
    [Tooltip("Kecepatan maksimum NPC")]
    public float maxSpeed = 4f;
    [Tooltip("Seberapa cepat velocity boleh berubah per detik (efek inersia)")]
    public float maxAcceleration = 8f;
    [Tooltip("Kecepatan rotasi menghadap arah gerak")]
    public float turnSpeed = 6f;

    [Header("Arrive")]
    [Tooltip("Jarak mulai melambat")]
    public float slowRadius = 5f;
    [Tooltip("Jarak NPC berhenti dari target")]
    public float stopRadius = 1.5f;

    [Header("Wander")]
    [Tooltip("Kecepatan saat wandering")]
    public float wanderSpeed = 2f;
    [Tooltip("Frekuensi perubahan arah (detik)")]
    public float wanderChangeInterval = 1.5f;
    [Tooltip("Besar sudut perubahan arah maksimum (derajat)")]
    public float wanderAngleChange = 60f;

    [Header("Avoidance")]
    [Tooltip("Prioritas menghindar dibanding desired velocity")]
    public float avoidanceWeight = 2f;

    [Header("Pengembangan: Flee")]
    [Tooltip("Aktifkan Flee saat Player terlalu dekat")]
    public bool enableFlee = false;
    [Tooltip("Jarak Player dianggap terlalu dekat")]
    public float fleeRadius = 2f;

    [Header("Pengembangan: Warna per Behavior")]
    public bool changeColor = true;
    public Color arriveColor = new Color(0.2f, 0.8f, 0.3f);   // hijau
    public Color wanderColor = new Color(0.3f, 0.5f, 1f);     // biru
    public Color avoidingColor = new Color(1f, 0.3f, 0.2f);   // merah
    public Color fleeColor = new Color(1f, 0.85f, 0.2f);      // kuning
    public Color idleColor = Color.gray;

    [Header("Debug (read-only)")]
    [SerializeField] BehaviorState currentState;
    [SerializeField] Vector3 velocity;
    [SerializeField] Vector3 desiredVelocity;
    [SerializeField] Vector3 avoidance;

    public Vector3 Velocity => velocity;
    public BehaviorState CurrentState => currentState;

    SteeringSensor sensor;
    Renderer rend;
    float wanderAngle;      // heading wander dalam derajat (sumbu Y)
    float wanderTimer;

    void Awake()
    {
        sensor = GetComponent<SteeringSensor>();
        rend = GetComponentInChildren<Renderer>();
        wanderAngle = transform.eulerAngles.y;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---------- 1. Steering Behavior -> Desired Velocity ----------
        if (enableFlee && target != null &&
            Vector3.Distance(Flat(transform.position), Flat(target.position)) < fleeRadius)
        {
            desiredVelocity = Flee();
            currentState = BehaviorState.Flee;
        }
        else if (useTarget && target != null)
        {
            desiredVelocity = Arrive();
            currentState = desiredVelocity.sqrMagnitude > 0.001f ? BehaviorState.Arrive : BehaviorState.Idle;
        }
        else
        {
            desiredVelocity = Wander(dt);
            currentState = BehaviorState.Wander;
        }

        // ---------- 2. Obstacle Avoidance (selalu aktif, juga saat Wander) ----------
        // Sensor mengarah ke arah gerak sekarang; kalau diam, ke arah desired.
        Vector3 senseDir = velocity.sqrMagnitude > 0.01f ? velocity : desiredVelocity;
        avoidance = sensor.Sense(senseDir);

        if (avoidance.sqrMagnitude > 0.0001f)
        {
            // Gaya hindaran diberi skala maxSpeed agar sebanding dengan desired,
            // lalu dikalikan weight = seberapa "diprioritaskan" menghindar.
            desiredVelocity += avoidance * maxSpeed * avoidanceWeight;
            desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
            currentState = BehaviorState.Avoiding;
        }

        // ---------- 3. Acceleration (efek inersia) ----------
        Vector3 steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, maxAcceleration * dt);

        // ---------- 4. Velocity ----------
        velocity += steering;
        velocity.y = 0f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // ---------- 5. Movement ----------
        transform.position += velocity * dt;

        // ---------- 6. Rotation ----------
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
        }

        ApplyColor();
    }

    // ---------------- Behaviors ----------------

    /// <summary>
    /// Arrive: seperti Seek, tapi kecepatan diturunkan secara linear di dalam
    /// Slow Radius dan menjadi nol di dalam Stop Radius.
    /// </summary>
    Vector3 Arrive()
    {
        Vector3 toTarget = Flat(target.position) - Flat(transform.position);
        float distance = toTarget.magnitude;

        if (distance <= stopRadius) return Vector3.zero;   // sudah sampai -> berhenti

        float speed = maxSpeed;
        if (distance < slowRadius)
        {
            // 0 di stopRadius, 1 di slowRadius
            float t = (distance - stopRadius) / Mathf.Max(slowRadius - stopRadius, 0.001f);
            speed = maxSpeed * t;
        }
        return toTarget.normalized * speed;
    }

    /// <summary>
    /// Wander: pertahankan heading, setiap interval ubah heading dengan sudut acak.
    /// </summary>
    Vector3 Wander(float dt)
    {
        wanderTimer -= dt;
        if (wanderTimer <= 0f)
        {
            wanderAngle += Random.Range(-wanderAngleChange, wanderAngleChange);
            wanderTimer = wanderChangeInterval;
        }
        Vector3 dir = Quaternion.Euler(0f, wanderAngle, 0f) * Vector3.forward;
        return dir * wanderSpeed;
    }

    /// <summary>Flee: kebalikan Seek, lari menjauhi target dengan kecepatan penuh.</summary>
    Vector3 Flee()
    {
        Vector3 away = Flat(transform.position) - Flat(target.position);
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        return away.normalized * maxSpeed;
    }

    // ---------------- Helpers ----------------

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void ApplyColor()
    {
        if (!changeColor || rend == null) return;
        Color c = currentState switch
        {
            BehaviorState.Arrive   => arriveColor,
            BehaviorState.Wander   => wanderColor,
            BehaviorState.Avoiding => avoidingColor,
            BehaviorState.Flee     => fleeColor,
            _                      => idleColor,
        };
        rend.material.color = c;
    }

    // ---------------- Gizmos ----------------

    void OnDrawGizmos()
    {
        Vector3 pos = transform.position + Vector3.up * 0.05f;

        // Velocity (hijau) dan desired velocity (cyan)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + velocity);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + desiredVelocity);

        // Avoidance (merah)
        if (avoidance.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + avoidance * maxSpeed * avoidanceWeight);
        }

        if (target != null)
        {
            // Garis ke target (magenta)
            Gizmos.color = useTarget ? Color.magenta : new Color(1f, 0f, 1f, 0.25f);
            Gizmos.DrawLine(pos, target.position);

#if UNITY_EDITOR
            Vector3 tp = target.position + Vector3.up * 0.05f;
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(tp, Vector3.up, slowRadius);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(tp, Vector3.up, stopRadius);
            if (enableFlee)
            {
                UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f);
                UnityEditor.Handles.DrawWireDisc(tp, Vector3.up, fleeRadius);
            }
#endif
        }
    }
}
