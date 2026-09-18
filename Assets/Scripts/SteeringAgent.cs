using UnityEngine;

// npc steering agent
// alurnya tiap frame: behavior -> desired velocity -> acceleration -> velocity -> gerak -> rotasi
[RequireComponent(typeof(SteeringSensor))]
public class SteeringAgent : MonoBehaviour
{
    public enum BehaviorState { Idle, Arrive, Wander, Avoiding, Flee }

    [Header("Target")]
    public Transform target;
    public bool useTarget = true;   // true = arrive ke player, false = wander

    [Header("Movement")]
    public float maxSpeed = 4f;          // kecepatan maksimal
    public float maxAcceleration = 8f;   // batas perubahan velocity per detik, ini yg bikin ada inersia
    public float turnSpeed = 6f;         // kecepatan muter ngadep arah gerak

    [Header("Arrive")]
    public float slowRadius = 5f;   // mulai ngerem
    public float stopRadius = 1.5f; // berhenti

    [Header("Wander")]
    public float wanderSpeed = 2f;
    public float wanderChangeInterval = 1.5f; // tiap berapa detik ganti arah
    public float wanderAngleChange = 60f;     // maksimal ganti arah berapa derajat

    [Header("Avoidance")]
    public float avoidanceWeight = 2f; // seberapa diprioritasin ngehindar dibanding desired velocity

    [Header("Pengembangan: Flee")]
    public bool enableFlee = false;
    public float fleeRadius = 2f; // kalo player lebih deket dari ini npc kabur

    [Header("Pengembangan: Warna per Behavior")]
    public bool changeColor = true;
    public Color arriveColor = new Color(0.2f, 0.8f, 0.3f);   // ijo
    public Color wanderColor = new Color(0.3f, 0.5f, 1f);     // biru
    public Color avoidingColor = new Color(1f, 0.3f, 0.2f);   // merah
    public Color fleeColor = new Color(1f, 0.85f, 0.2f);      // kuning
    public Color idleColor = Color.gray;

    [Header("Debug")]
    [SerializeField] BehaviorState currentState;
    [SerializeField] Vector3 velocity;
    [SerializeField] Vector3 desiredVelocity;
    [SerializeField] Vector3 avoidance;

    public Vector3 Velocity => velocity;
    public BehaviorState CurrentState => currentState;

    SteeringSensor sensor;
    Renderer rend;
    float wanderAngle;  // heading wander, derajat di sumbu y
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

        // 1. behavior -> desired velocity
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

        // 2. obstacle avoidance, selalu jalan termasuk pas wander
        // sensor ngarah ke arah gerak sekarang, kalo lagi diem pake arah desired
        Vector3 senseDir = velocity.sqrMagnitude > 0.01f ? velocity : desiredVelocity;
        avoidance = sensor.Sense(senseDir);

        if (avoidance.sqrMagnitude > 0.0001f)
        {
            // avoidance dikali maxSpeed biar skalanya sebanding sama desired, terus dikali weight
            desiredVelocity += avoidance * maxSpeed * avoidanceWeight;
            desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxSpeed);
            currentState = BehaviorState.Avoiding;
        }

        // 3. acceleration, dibatesin maxAcceleration biar ga langsung belok tajem
        Vector3 steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, maxAcceleration * dt);

        // 4. velocity
        velocity += steering;
        velocity.y = 0f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // 5. gerak
        transform.position += velocity * dt;

        // 6. rotasi ngadep arah gerak
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
        }

        ApplyColor();
    }

    // arrive: kayak seek tapi di dalem slowRadius kecepatannya diturunin, di dalem stopRadius jadi 0
    Vector3 Arrive()
    {
        Vector3 toTarget = Flat(target.position) - Flat(transform.position);
        float distance = toTarget.magnitude;

        if (distance <= stopRadius) return Vector3.zero; // udah nyampe

        float speed = maxSpeed;
        if (distance < slowRadius)
        {
            // t = 0 pas di stopRadius, 1 pas di slowRadius
            float t = (distance - stopRadius) / Mathf.Max(slowRadius - stopRadius, 0.001f);
            speed = maxSpeed * t;
        }
        return toTarget.normalized * speed;
    }

    // wander: jalan lurus, tiap interval headingnya digeser random
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

    // flee: kebalikan seek, lari menjauh dari target
    Vector3 Flee()
    {
        Vector3 away = Flat(transform.position) - Flat(target.position);
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        return away.normalized * maxSpeed;
    }

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

    void OnDrawGizmos()
    {
        Vector3 pos = transform.position + Vector3.up * 0.05f;

        // ijo = velocity, cyan = desired
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + velocity);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + desiredVelocity);

        // merah = avoidance
        if (avoidance.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + avoidance * maxSpeed * avoidanceWeight);
        }

        if (target != null)
        {
            // garis ke target, agak transparan kalo lagi ga dipake
            Gizmos.color = useTarget ? Color.magenta : new Color(1f, 0f, 1f, 0.25f);
            Gizmos.DrawLine(pos, target.position);

#if UNITY_EDITOR
            // lingkaran slow / stop / flee radius di sekitar target
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
