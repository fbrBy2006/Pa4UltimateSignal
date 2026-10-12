using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A light security enemy with two readable states: patrol and pursuit.
/// Vision is sampled on a timer so distance checks and raycasts are not repeated every frame.
/// </summary>
public sealed class SecurityDroneAI : MonoBehaviour
{
    private enum BehaviorState
    {
        Patrol,
        Chase
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerCheckpoint playerCheckpoint;
    [SerializeField] private Transform waypointA;
    [SerializeField] private Transform waypointB;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Light alertLight;
    [SerializeField] private AudioSource alertAudio;
    [SerializeField] private AudioClip alertClip;

    [Header("Movement and vision")]
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.7f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.2f;
    [SerializeField, Min(1f)] private float detectionRange = 10f;
    [SerializeField, Min(1f)] private float loseInterestRange = 14f;
    [SerializeField, Min(0.05f)] private float visionCheckInterval = 0.2f;
    [SerializeField, Min(0f)] private float searchDuration = 2.4f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.2f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 2.4f;

    private BehaviorState state = BehaviorState.Patrol;
    private Transform currentWaypoint;
    private Vector3 lastKnownPlayerPosition;
    private float nextVisionCheck;
    private float nextAttackTime;
    private float timeWithoutSight;
    private float originalLightIntensity;

    /// <summary>
    /// Supplies the PA4 systems at scene startup as well as in the editor-built scene.
    /// This lets the saved PA3 scene remain playable while the project is upgraded.
    /// </summary>
    public static void EnsureSceneSystems()
    {
        FirstPersonController playerController = FindAnyObjectByType<FirstPersonController>();
        if (playerController == null)
            return;

        TMP_Text missionLabel = FindHudText("MissionObjective");
        TMP_Text statusLabel = FindHudText("SecurityStatus");
        if (missionLabel == null || statusLabel == null)
        {
            Transform hud = GetOrCreateHud();
            if (missionLabel == null)
                missionLabel = CreateHudText(hud, "MissionObjective",
                    "OBJETIVO: RECOGE LAS 7 CELDAS, ACTIVA EL NÚCLEO Y ESCAPA\nWASD Mover   |   Shift Correr   |   Espacio Saltar   |   E Interactuar",
                    new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(900f, 72f),
                    18f, new Color(0.86f, 0.94f, 1f), TextAlignmentOptions.Center);
            if (statusLabel == null)
                statusLabel = CreateHudText(hud, "SecurityStatus", "SENTINELA: PATRULLANDO",
                    new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(340f, 42f),
                    20f, new Color(0.35f, 0.92f, 0.8f), TextAlignmentOptions.Right);
        }

        SecurityDroneAI existing = FindAnyObjectByType<SecurityDroneAI>();
        if (existing != null)
        {
            existing.statusText = statusLabel;
            return;
        }

        Transform props = GameObject.Find("ENVIRONMENT")?.transform.Find("Props");
        Transform route = new GameObject("SentinelPatrolRoute").transform;
        if (props != null)
            route.SetParent(props, false);

        Transform waypointA = new GameObject("PatrolWaypointA").transform;
        waypointA.SetParent(route, false);
        waypointA.localPosition = new Vector3(0f, 0.04f, 37.5f);
        Transform waypointB = new GameObject("PatrolWaypointB").transform;
        waypointB.SetParent(route, false);
        waypointB.localPosition = new Vector3(0f, 0.04f, 44.5f);

        GameObject sentinel = new GameObject("SecuritySentinel");
        if (props != null)
            sentinel.transform.SetParent(props, false);
        sentinel.transform.localPosition = waypointA.localPosition;
        sentinel.SetActive(false);

        Material armor = CreateRuntimeMaterial(new Color(0.32f, 0.39f, 0.43f));
        Material accent = CreateRuntimeMaterial(new Color(0.04f, 0.65f, 0.78f), true);
        Material optic = CreateRuntimeMaterial(new Color(0.95f, 0.08f, 0.025f), true);
        CreatePart(sentinel.transform, PrimitiveType.Cube, "ArmorCore", new Vector3(0f, 1.05f, 0f),
            new Vector3(0.9f, 0.8f, 0.65f), armor);
        CreatePart(sentinel.transform, PrimitiveType.Sphere, "SensorHead", new Vector3(0f, 1.68f, 0.06f),
            new Vector3(0.62f, 0.43f, 0.56f), armor);
        CreatePart(sentinel.transform, PrimitiveType.Sphere, "Optic", new Vector3(0f, 1.67f, 0.34f),
            new Vector3(0.34f, 0.12f, 0.1f), optic);
        CreatePart(sentinel.transform, PrimitiveType.Cube, "LeftArm", new Vector3(-0.6f, 1.05f, 0f),
            new Vector3(0.22f, 0.68f, 0.26f), accent);
        CreatePart(sentinel.transform, PrimitiveType.Cube, "RightArm", new Vector3(0.6f, 1.05f, 0f),
            new Vector3(0.22f, 0.68f, 0.26f), accent);
        CreatePart(sentinel.transform, PrimitiveType.Capsule, "LeftLeg", new Vector3(-0.24f, 0.58f, 0f),
            new Vector3(0.22f, 0.58f, 0.22f), armor);
        CreatePart(sentinel.transform, PrimitiveType.Capsule, "RightLeg", new Vector3(0.24f, 0.58f, 0f),
            new Vector3(0.22f, 0.58f, 0.22f), armor);

        GameObject beacon = new GameObject("AlertLight");
        beacon.transform.SetParent(sentinel.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 2.02f, 0f);
        Light alertBeacon = beacon.AddComponent<Light>();
        alertBeacon.type = LightType.Point;
        alertBeacon.color = new Color(1f, 0.22f, 0.08f);
        alertBeacon.intensity = 0.45f;
        alertBeacon.range = 5f;
        alertBeacon.shadows = LightShadows.None;

        AudioSource audioSource = sentinel.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.25f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 18f;

        SecurityDroneAI ai = sentinel.AddComponent<SecurityDroneAI>();
        ai.player = playerController.transform;
        ai.playerCheckpoint = playerController.GetComponent<PlayerCheckpoint>();
        ai.waypointA = waypointA;
        ai.waypointB = waypointB;
        ai.statusText = statusLabel;
        ai.alertLight = alertBeacon;
        ai.alertAudio = audioSource;
        ai.alertClip = CreateRuntimeAlertClip();

        // Realtime point-light shadows are disabled once at scene start to reduce GPU work.
        foreach (Light pointLight in FindObjectsByType<Light>())
            if (pointLight.type == LightType.Point)
                pointLight.shadows = LightShadows.None;

        sentinel.SetActive(true);
    }

    private static TMP_Text FindHudText(string name)
    {
        GameObject label = GameObject.Find(name);
        return label != null ? label.GetComponent<TMP_Text>() : null;
    }

    private static Transform GetOrCreateHud()
    {
        GameObject existingHud = GameObject.Find("InteractionHUD");
        if (existingHud != null)
            return existingHud.transform;

        GameObject canvas = new GameObject("InteractionHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas.transform;
    }

    private static TMP_Text CreateHudText(Transform parent, string name, string value, Vector2 anchor,
        Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static Material CreateRuntimeMaterial(Color color, bool emission = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }
        return material;
    }

    private static void CreatePart(Transform parent, PrimitiveType type, string name, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            partCollider.enabled = false;
    }

    private static AudioClip CreateRuntimeAlertClip()
    {
        const int sampleRate = 22050;
        int segmentSamples = Mathf.RoundToInt(sampleRate * 0.12f);
        float[] samples = new float[segmentSamples * 2];
        for (int i = 0; i < segmentSamples; i++)
        {
            float fadeIn = Mathf.Clamp01(i / (sampleRate * 0.008f));
            float fadeOut = Mathf.Clamp01((segmentSamples - i) / (sampleRate * 0.015f));
            samples[i] = 0.34f * fadeIn * fadeOut * Mathf.Sin(2f * Mathf.PI * 880f * i / sampleRate);
            int second = segmentSamples + i;
            samples[second] = 0.34f * fadeIn * fadeOut * Mathf.Sin(2f * Mathf.PI * 660f * i / sampleRate);
        }

        AudioClip clip = AudioClip.Create("SentinelAlert", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void Awake()
    {
        if (player == null)
        {
            FirstPersonController foundPlayer = FindAnyObjectByType<FirstPersonController>();
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        if (playerCheckpoint == null && player != null)
            playerCheckpoint = player.GetComponent<PlayerCheckpoint>();

        currentWaypoint = waypointA;
        if (alertLight != null)
            originalLightIntensity = alertLight.intensity;

        if (player != null)
            lastKnownPlayerPosition = player.position;
        SetState(BehaviorState.Patrol);
    }

    private void Update()
    {
        if (player == null || waypointA == null || waypointB == null)
            return;

        // Sensing is intentionally sampled at 5 Hz; locomotion remains smooth at the frame rate.
        if (Time.time >= nextVisionCheck)
        {
            nextVisionCheck = Time.time + visionCheckInterval;
            UpdateVision();
        }

        if (state == BehaviorState.Chase)
        {
            MoveToward(lastKnownPlayerPosition, chaseSpeed);
            TryAttack();
        }
        else
        {
            Patrol();
        }
    }

    private void UpdateVision()
    {
        Vector3 playerPosition = player.position;
        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;
        float distanceSquared = toPlayer.sqrMagnitude;

        if (state == BehaviorState.Patrol && distanceSquared > detectionRange * detectionRange)
            return;

        if (state == BehaviorState.Chase && distanceSquared > loseInterestRange * loseInterestRange)
        {
            SetState(BehaviorState.Patrol);
            return;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 1.35f;
        Vector3 target = playerPosition + Vector3.up * 1.1f;
        Vector3 rayDirection = target - rayOrigin;
        float rayDistance = rayDirection.magnitude;
        bool seesPlayer = rayDistance > 0.01f &&
                          Physics.Raycast(rayOrigin, rayDirection / rayDistance, out RaycastHit hit,
                              rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                          hit.collider.transform.IsChildOf(player);

        if (seesPlayer)
        {
            lastKnownPlayerPosition = playerPosition;
            timeWithoutSight = 0f;
            SetState(BehaviorState.Chase);
        }
        else if (state == BehaviorState.Chase)
        {
            timeWithoutSight += visionCheckInterval;
            if (timeWithoutSight >= searchDuration)
                SetState(BehaviorState.Patrol);
        }
    }

    private void Patrol()
    {
        if (currentWaypoint == null)
            currentWaypoint = waypointA;

        MoveToward(currentWaypoint.position, patrolSpeed);
        Vector3 difference = currentWaypoint.position - transform.position;
        difference.y = 0f;
        if (difference.sqrMagnitude <= 0.25f)
            currentWaypoint = currentWaypoint == waypointA ? waypointB : waypointA;
    }

    private void MoveToward(Vector3 destination, float speed)
    {
        destination.y = transform.position.y;
        Vector3 difference = destination - transform.position;
        if (difference.sqrMagnitude < 0.0001f)
            return;

        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(difference.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    }

    private void TryAttack()
    {
        Vector3 difference = player.position - transform.position;
        difference.y = 0f;
        if (difference.sqrMagnitude <= attackRange * attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            if (playerCheckpoint != null)
                playerCheckpoint.Die();

            PlayAlertSound();
        }
    }

    private void SetState(BehaviorState nextState)
    {
        if (state == nextState)
            return;

        state = nextState;
        if (statusText != null)
        {
            statusText.text = state == BehaviorState.Chase
                ? "ALERTA: SENTINELA EN PERSECUCIÓN"
                : "SENTINELA: PATRULLANDO";
            statusText.color = state == BehaviorState.Chase
                ? new Color(1f, 0.38f, 0.2f)
                : new Color(0.35f, 0.92f, 0.8f);
        }

        if (alertLight != null)
            alertLight.intensity = state == BehaviorState.Chase ? Mathf.Max(originalLightIntensity, 1.6f) : originalLightIntensity;

        if (state == BehaviorState.Chase)
            PlayAlertSound();
    }

    private void PlayAlertSound()
    {
        if (alertAudio != null && alertClip != null)
            alertAudio.PlayOneShot(alertClip, 0.8f);
    }
}
