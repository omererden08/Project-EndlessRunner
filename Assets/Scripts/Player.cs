using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Player : MonoBehaviour
{
    [Header("Şerit Ayarları")]
    [Tooltip("Şerit X offset: sol = –1, orta = 0, sağ = +1")]
    [SerializeField, Range(0.5f, 2f)] private float laneXOffset = 1f;
    [SerializeField, Range(0.05f, 0.5f)] private float laneChangeDuration = 0.1f;

    [Header("Kamera Takibi")]
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private Camera mainCam;

    [Header("Bileşenler")]
    [SerializeField] private Rigidbody rb;

    // Şeritler: -1, 0, +1
    private static readonly float[] lanes = { -1f, 0f, +1f };
    private int currentLane = 1;

    private Vector3 velocity; // SmoothDamp için
    private Vector2 swipeStart;
    private bool swipeProcessed;

    private const float SWIPE_SQR_THRESHOLD = 100f * 100f;

#if UNITY_EDITOR
    private static bool simInitialized;
#endif
    private static bool inputInitialized = false;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (mainCam == null) mainCam = Camera.main;

        // Input sistemini sadece bir kez başlat
        if (!inputInitialized)
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            if (!simInitialized)
            {
                TouchSimulation.Enable();
                simInitialized = true;
            }
#endif
            inputInitialized = true;
        }

        // Rigidbody ayarları
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private Vector3 camOffset;

    private void Start()
    {
        transform.position = new Vector3(0f, -3.05f, -6f); // Başlangıç pozisyonu
        camOffset = mainCam.transform.position - transform.position;
    }


    private void OnEnable()
    {
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerMove += OnFingerMove;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerMove -= OnFingerMove;
    }

    private void OnFingerDown(Finger finger)
    {
        swipeStart = finger.screenPosition;
        swipeProcessed = false;
    }

    private void OnFingerMove(Finger finger)
    {
        if (swipeProcessed) return;

        Vector2 delta = finger.screenPosition - swipeStart;
        if (delta.sqrMagnitude < SWIPE_SQR_THRESHOLD) return;

        swipeProcessed = true;

        // Sadece yatay swipe kontrolü
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            int direction = delta.x > 0 ? 1 : -1;
            currentLane = Mathf.Clamp(currentLane + direction, 0, lanes.Length - 1);
        }
    }

    private void FixedUpdate()
    {
        Vector3 currentPos = rb.position;
        Vector3 targetPos = new Vector3(lanes[currentLane] * laneXOffset, currentPos.y, currentPos.z);

        Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.fixedDeltaTime / laneChangeDuration);
        rb.MovePosition(newPos);
    }


    private void LateUpdate()
    {
        if (!mainCam) return;

        Vector3 targetCamPos = new Vector3(lanes[currentLane] * 0.5f, transform.position.y, transform.position.z) + camOffset;

        mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetCamPos, Time.deltaTime * cameraFollowSpeed);
    }


    private void OnTriggerEnter(Collider other)
    {
        EventManager.Invoke("GameOver");
    }
}
