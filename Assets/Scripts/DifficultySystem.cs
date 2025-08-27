using UnityEngine;

[DefaultExecutionOrder(-100)] // diğer sistemlerden önce çalışsın
public class DifficultySystem : MonoBehaviour
{
    [Header("İlerleme (0..1)")]
    [SerializeField] bool driveByTime = true;
    [SerializeField, Min(0.01f)] float secondsToMax = 90f;
    [SerializeField] bool useUnscaledTime = false;
    [Range(0, 1)] public float manualProgress = 0f;

    float elapsed;
    float t; // 0..1

    [Header("Çarpan Eğrileri (Y = multiplier)")]
    [Tooltip("Obstacle hız çarpanı. 1 = başlangıç hızı, >1 = daha hızlı.")]
    public AnimationCurve obstacleSpeedMul = new AnimationCurve(
    new Keyframe(0f, 1f),        // 0s: başlangıç
    new Keyframe(0.2f, 1.2f),    // 36s: yavaş artış
    new Keyframe(0.4f, 1.6f),    // 72s: ortalama
    new Keyframe(0.7f, 2.2f),    // 126s: zorlayıcı
    new Keyframe(1f, 2.5f)       // 180s: maksimum hız
);

    [Tooltip("Obstacle spawn interval çarpanı. 1 = başlangıç aralığı, <1 = daha SIK spawn.")]
    public AnimationCurve obstacleSpawnIntervalMul = new AnimationCurve(
    new Keyframe(0f, 1f),      // 0s → 5s
    new Keyframe(0.3f, 0.9f),  // 54s → 4.5s
    new Keyframe(0.6f, 0.75f), // 108s → 3.75s
    new Keyframe(1f, 0.6f)     // 180s → 3s (maks zorlukta)
);

    [Tooltip("Ground hız çarpanı. 1 = normal zemin hızı.")]
    public AnimationCurve groundSpeedMul = new AnimationCurve(
    new Keyframe(0f, 1f),        // 0s: başlangıç
    new Keyframe(0.2f, 1.2f),    // 36s
    new Keyframe(0.4f, 1.6f),    // 72s
    new Keyframe(0.7f, 2.2f),    // 126s
    new Keyframe(1f, 2.5f)       // 180s: tavan hız
);

    public float T => t;
    public float ObstacleSpeedMultiplier => Mathf.Max(0f, obstacleSpeedMul.Evaluate(t));
    public float ObstacleSpawnIntervalMultiplier => Mathf.Clamp(obstacleSpawnIntervalMul.Evaluate(t), 0.05f, 10f);
    public float GroundSpeedMultiplier => Mathf.Max(0f, groundSpeedMul.Evaluate(t));


    void Awake()
    {
        // Taşmayı engelle
        obstacleSpeedMul.preWrapMode = obstacleSpeedMul.postWrapMode = WrapMode.ClampForever;
        obstacleSpawnIntervalMul.preWrapMode = obstacleSpawnIntervalMul.postWrapMode = WrapMode.ClampForever;
        groundSpeedMul.preWrapMode = groundSpeedMul.postWrapMode = WrapMode.ClampForever;

    }

    void Update()
    {
        if (driveByTime)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            t = Mathf.Clamp01(elapsed / secondsToMax);
        }
        else
        {
            t = Mathf.Clamp01(manualProgress);
        }
    }

    // İstersen başka sistemden manuel sür
    public void SetProgress01(float value) => t = Mathf.Clamp01(value);
}
