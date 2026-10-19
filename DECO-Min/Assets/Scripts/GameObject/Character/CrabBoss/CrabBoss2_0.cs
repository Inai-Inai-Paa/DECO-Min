using UnityEngine;

public enum CrabBoss2Attack
{
    RightSlam,
    LeftRocket,
    JumpSequence
}

public enum CrabBoss2State
{
    Inactive,
    Approach,
    Rest,
    Attack1,
    Attack2,
    Attack3,
    Recovery
}

public static class CrabBoss2AttackSelector
{
    // Kept for the existing edit-mode tests and for callers that name the right hand first.
    public static CrabBoss2Attack SelectAttack(bool hasRightHand, bool hasLeftHand, int randomIndex)
    {
        return Select(hasLeftHand, hasRightHand, randomIndex);
    }

    public static CrabBoss2Attack Select(bool hasLeftHand, bool hasRightHand, int randomIndex)
    {
        if (!hasLeftHand && !hasRightHand)
        {
            return CrabBoss2Attack.JumpSequence;
        }

        if (!hasLeftHand)
        {
            return CrabBoss2Attack.RightSlam;
        }

        if (!hasRightHand)
        {
            return CrabBoss2Attack.LeftRocket;
        }

        return randomIndex % 2 == 0 ? CrabBoss2Attack.RightSlam : CrabBoss2Attack.LeftRocket;
    }
}

[System.Serializable]
public class CrabBoss2HandState
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private bool isDestroyed;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDestroyed => isDestroyed;

    public CrabBoss2HandState() { }

    public CrabBoss2HandState(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
    }

    public void ApplyDamage(int damage)
    {
        if (isDestroyed || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        isDestroyed = currentHealth == 0;
    }

    public void Destroy()
    {
        currentHealth = 0;
        isDestroyed = true;
    }

    public void Restore()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDestroyed = false;
    }

    public void Normalize()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        isDestroyed = currentHealth == 0;
    }
}

[System.Serializable]
public class CrabBoss2HealthState
{
    [SerializeField, Min(1)] private int maxHealth = 1000;
    [SerializeField] private int currentHealth = 1000;
    [SerializeField] private bool isDestroyed;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDestroyed => isDestroyed;

    public void ApplyDamage(int damage)
    {
        if (isDestroyed || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        isDestroyed = currentHealth == 0;
    }

    public void Restore()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        isDestroyed = false;
    }

    public void Normalize()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        isDestroyed = currentHealth == 0;
    }
}

/// <summary>
/// Standalone 2.0 boss behavior. This deliberately does not inherit any 1.0 boss logic.
/// </summary>
public class CrabBoss2_0 : Character
{
    private enum Phase
    {
        Inactive,
        Rest,
        RightWindUp,
        RightTelegraph,
        RightSlam,
        RightRecovery,
        LeftPullBack,
        LeftTelegraph,
        LeftThrust,
        LeftRecovery,
        ShortJumpRise,
        ShortJumpTravel,
        FinalJumpRise,
        FinalJumpTravel,
        LandingRecovery
    }

    [Header("Hand Status")]
    [SerializeField] private CrabBoss2HandState leftHandState = new CrabBoss2HandState();
    [SerializeField] private CrabBoss2HandState rightHandState = new CrabBoss2HandState();
    [SerializeField] private CrabBoss2HealthState bodyHealth = new CrabBoss2HealthState();

    [SerializeField, HideInInspector] private CrabHand leftHand;
    [SerializeField, HideInInspector] private CrabHand rightHand;

    [SerializeField, HideInInspector, Min(0f)] private float approachSpeed = 8f;
    [SerializeField, HideInInspector, Min(0f)] private float attackRange = 12f;
    [SerializeField, HideInInspector, Min(0f)] private float turnSpeed = 360f;
    [SerializeField, HideInInspector, Min(0f)] private float restDuration = 1f;

    [SerializeField, HideInInspector, Min(0f)] private float rightWindUpDuration = 0.8f;
    [SerializeField, HideInInspector, Min(0f)] private float rightTelegraphDuration = 2f;
    [SerializeField, HideInInspector, Min(0f)] private float rightSlamDuration = 0.4f;
    [SerializeField, HideInInspector, Min(0f)] private float rightRecoveryDuration = 3f;
    [SerializeField, HideInInspector, Min(0f)] private float rightReturnDuration = 1f;
    [SerializeField, HideInInspector] private Vector3 rightSlamColliderLocalOffset = new Vector3(7.5f, 0f, 0f);

    [SerializeField, HideInInspector, Min(0f)] private float leftPullBackDuration = 0.4f;
    [SerializeField, HideInInspector, Min(0f)] private float leftTelegraphDuration = 3f;
    [SerializeField, HideInInspector, Min(0f)] private float leftThrustDuration = 0.25f;
    [SerializeField, HideInInspector, Min(0f)] private float leftRecoveryDuration = 3f;
    [SerializeField, HideInInspector, Min(0f)] private float leftReturnDuration = 1f;
    [SerializeField, HideInInspector, Min(0f)] private float leftPullBackDistance = 2f;
    [SerializeField, HideInInspector, Min(0f)] private float leftAttackRadius = 1.5f;

    [SerializeField, HideInInspector, Min(0)] private int shortJumpCount = 2;
    [SerializeField, HideInInspector, Min(0f)] private float shortJumpHeight = 2f;
    [SerializeField, HideInInspector, Min(0f)] private float shortJumpDuration = 0.75f;
    [SerializeField, HideInInspector, Min(0f)] private float finalJumpWindUpDuration = 0.5f;
    [SerializeField, HideInInspector, Min(0f)] private float finalJumpHeight = 6f;
    [SerializeField, HideInInspector, Min(0f)] private float finalJumpTravelDuration = 3f;
    [SerializeField, HideInInspector, Min(0f)] private float landingRecoveryDuration = 3f;
    [SerializeField, HideInInspector, Min(0f)] private float landingReturnDuration = 1f;
    [SerializeField, HideInInspector, Min(0f)] private float landingDamageRadius = 8f;
    [SerializeField, HideInInspector, Min(0f)] private float landingMinDamage = 1f;
    [SerializeField, HideInInspector, Min(0f)] private float landingMaxDamage = 100f;

    private Rigidbody bossRigidbody;
    private Transform playerTransform;
    private Phase phase = Phase.Inactive;
    private bool isActive;
    private CrabBoss2State currentState = CrabBoss2State.Inactive;
    private float phaseTimer;
    private int completedShortJumps;
    private Vector3 phaseStartPosition;
    private Vector3 lockedTargetPosition;
    private Vector3 leftHandDefaultLocalPosition;
    private Vector3 rightHandDefaultLocalPosition;
    private Quaternion leftHandDefaultLocalRotation;
    private Quaternion rightHandDefaultLocalRotation;
    private Vector3 leftThrustStartLocalPosition;
    private Vector3 leftThrustTargetLocalPosition;
    private Vector3 leftThrustStartWorldPosition;
    private bool defaultsCached;

    public bool IsActive => isActive;
    public CrabBoss2State CurrentState => currentState;
    public bool CanDamageBody => leftHandState != null && rightHandState != null &&
                                 leftHandState.IsDestroyed && rightHandState.IsDestroyed;
    public int BodyCurrentHealth => bodyHealth != null ? bodyHealth.CurrentHealth : 0;

    private void Awake()
    {
        SetActive(false);
    }

    private void OnEnable()
    {
        SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        bossRigidbody = GetComponent<Rigidbody>();
        EnsureHandStates();
        EnsureHandReferences();
        leftHandState.Normalize();
        rightHandState.Normalize();
        bodyHealth.Normalize();
        ApplyHandStates();
        StopPhysicsMotion();
    }

    protected override void Update()
    {
        StopPhysicsMotion();

        if (!isActive)
        {
            if (phase != Phase.Inactive)
            {
                SetPhase(Phase.Inactive);
            }
            return;
        }

        if (phase == Phase.Inactive)
        {
            SetPhase(Phase.Rest);
        }

        phaseTimer += Time.deltaTime;
        RunPhase();
    }

    protected override void FixedUpdate()
    {
        StopPhysicsMotion();
    }

    public void SetActive(bool active)
    {
        if (isActive == active)
        {
            return;
        }

        isActive = active;
        if (!isActive)
        {
            completedShortJumps = 0;
            RestoreHandTransforms();
            SetPhase(Phase.Inactive);
        }
    }

    public void DamageLeftHand(int damage)
    {
        EnsureHandStates();
        leftHandState.ApplyDamage(damage);
        ApplyHandStates();
    }

    public void DamageRightHand(int damage)
    {
        EnsureHandStates();
        rightHandState.ApplyDamage(damage);
        ApplyHandStates();
    }

    public void DestroyLeftHand()
    {
        EnsureHandStates();
        leftHandState.Destroy();
        ApplyHandStates();
    }

    public void DestroyRightHand()
    {
        EnsureHandStates();
        rightHandState.Destroy();
        ApplyHandStates();
    }

    public void RestoreLeftHand()
    {
        EnsureHandStates();
        leftHandState.Restore();
        ApplyHandStates();
    }

    public void RestoreRightHand()
    {
        EnsureHandStates();
        rightHandState.Restore();
        ApplyHandStates();
    }

    public void DamageBody(int damage)
    {
        EnsureHandStates();
        if (!CanDamageBody)
        {
            return;
        }

        bodyHealth.ApplyDamage(damage);
    }

    public void RestoreBodyHealth()
    {
        EnsureHandStates();
        bodyHealth.Restore();
    }

    private void RunPhase()
    {
        switch (phase)
        {
            case Phase.Rest:
                UpdateRest();
                break;
            case Phase.RightWindUp:
                UpdateRightWindUp();
                break;
            case Phase.RightTelegraph:
                UpdateRightTelegraph();
                break;
            case Phase.RightSlam:
                UpdateRightSlam();
                break;
            case Phase.RightRecovery:
                UpdateRightRecovery();
                break;
            case Phase.LeftPullBack:
                UpdateLeftPullBack();
                break;
            case Phase.LeftTelegraph:
                UpdateLeftTelegraph();
                break;
            case Phase.LeftThrust:
                UpdateLeftThrust();
                break;
            case Phase.LeftRecovery:
                UpdateLeftRecovery();
                break;
            case Phase.ShortJumpRise:
                UpdateShortJumpRise();
                break;
            case Phase.ShortJumpTravel:
                UpdateShortJumpTravel();
                break;
            case Phase.FinalJumpRise:
                UpdateFinalJumpRise();
                break;
            case Phase.FinalJumpTravel:
                UpdateFinalJumpTravel();
                break;
            case Phase.LandingRecovery:
                UpdateLandingRecovery();
                break;
        }
    }

    private void UpdateRest()
    {
        MoveTowardPlayer();
        if (IsPlayerOutsideAttackRange())
        {
            currentState = CrabBoss2State.Approach;
            phaseTimer = 0f;
            return;
        }

        currentState = CrabBoss2State.Rest;
        if (phaseTimer < restDuration)
        {
            return;
        }

        CrabBoss2Attack attack = CrabBoss2AttackSelector.Select(HasLeftHand(), HasRightHand(), Random.Range(0, 2));
        SetPhase(attack == CrabBoss2Attack.RightSlam ? Phase.RightWindUp :
            attack == CrabBoss2Attack.LeftRocket ? Phase.LeftPullBack : Phase.ShortJumpRise);
    }

    private void UpdateRightWindUp()
    {
        if (rightHand != null)
        {
            float progress = Progress(rightWindUpDuration);
            rightHand.transform.localRotation = Quaternion.Slerp(
                rightHandDefaultLocalRotation,
                rightHandDefaultLocalRotation * Quaternion.Euler(-55f, 0f, 0f),
                progress);
        }

        if (phaseTimer >= rightWindUpDuration)
        {
            SetPhase(Phase.RightTelegraph);
        }
    }

    private void UpdateRightTelegraph()
    {
        if (phaseTimer >= rightTelegraphDuration)
        {
            SetPhase(Phase.RightSlam);
        }
    }

    private void UpdateRightSlam()
    {
        if (rightHand != null)
        {
            float progress = Progress(rightSlamDuration);
            Quaternion strike = rightHandDefaultLocalRotation * Quaternion.Euler(120f, 0f, 0f);
            rightHand.transform.localRotation = Quaternion.Slerp(
                rightHandDefaultLocalRotation * Quaternion.Euler(-55f, 0f, 0f), strike, progress);
        }

        if (phaseTimer >= rightSlamDuration)
        {
            SpawnRightSlamCollider();
            SetPhase(Phase.RightRecovery);
        }
    }

    private void UpdateRightRecovery()
    {
        if (rightHand != null)
        {
            float returnProgress = Progress(rightReturnDuration);
            rightHand.transform.localRotation = Quaternion.Slerp(
                rightHandDefaultLocalRotation * Quaternion.Euler(120f, 0f, 0f),
                rightHandDefaultLocalRotation,
                returnProgress);
        }

        if (phaseTimer >= rightRecoveryDuration)
        {
            SetPhase(Phase.Rest);
        }
    }

    private void UpdateLeftPullBack()
    {
        TurnToward(lockedTargetPosition - transform.position);
        if (leftHand != null)
        {
            leftHand.transform.localPosition = Vector3.Lerp(
                leftHandDefaultLocalPosition,
                leftHandDefaultLocalPosition + Vector3.back * leftPullBackDistance,
                Progress(leftPullBackDuration));
        }

        if (phaseTimer >= leftPullBackDuration)
        {
            SetPhase(Phase.LeftTelegraph);
        }
    }

    private void UpdateLeftTelegraph()
    {
        TurnToward(lockedTargetPosition - transform.position);
        if (phaseTimer >= leftTelegraphDuration)
        {
            if (leftHand != null)
            {
                leftThrustStartLocalPosition = leftHand.transform.localPosition;
                leftThrustStartWorldPosition = leftHand.transform.position;
                leftThrustTargetLocalPosition = transform.InverseTransformPoint(lockedTargetPosition);
                leftThrustTargetLocalPosition.y = leftHandDefaultLocalPosition.y;
            }
            SetPhase(Phase.LeftThrust);
        }
    }

    private void UpdateLeftThrust()
    {
        if (leftHand != null)
        {
            leftHand.transform.localPosition = Vector3.Lerp(leftThrustStartLocalPosition, leftThrustTargetLocalPosition, Progress(leftThrustDuration));
        }

        if (phaseTimer >= leftThrustDuration)
        {
            CheckLeftThrustHit();
            SetPhase(Phase.LeftRecovery);
        }
    }

    private void UpdateLeftRecovery()
    {
        if (leftHand != null)
        {
            leftHand.transform.localPosition = Vector3.Lerp(leftThrustTargetLocalPosition, leftHandDefaultLocalPosition, Progress(leftReturnDuration));
        }

        if (phaseTimer >= leftRecoveryDuration)
        {
            SetPhase(Phase.Rest);
        }
    }

    private void UpdateShortJumpRise()
    {
        TurnTowardPlayer();
        if (phaseTimer >= finalJumpWindUpDuration)
        {
            phaseStartPosition = transform.position;
            lockedTargetPosition = GetPlayerPosition();
            SetPhase(Phase.ShortJumpTravel);
        }
    }

    private void UpdateShortJumpTravel()
    {
        float progress = Progress(shortJumpDuration);
        Vector3 ground = Vector3.Lerp(phaseStartPosition, lockedTargetPosition, progress);
        transform.position = ground + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * shortJumpHeight);
        if (phaseTimer >= shortJumpDuration)
        {
            transform.position = lockedTargetPosition;
            completedShortJumps++;
            SetPhase(completedShortJumps < shortJumpCount ? Phase.ShortJumpRise : Phase.FinalJumpRise);
        }
    }

    private void UpdateFinalJumpRise()
    {
        TurnTowardPlayer();
        if (phaseTimer >= finalJumpWindUpDuration)
        {
            phaseStartPosition = transform.position;
            lockedTargetPosition = GetPlayerPosition();
            SetPhase(Phase.FinalJumpTravel);
        }
    }

    private void UpdateFinalJumpTravel()
    {
        float progress = Progress(finalJumpTravelDuration);
        Vector3 ground = Vector3.Lerp(phaseStartPosition, lockedTargetPosition, progress);
        transform.position = ground + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * finalJumpHeight);
        if (phaseTimer >= finalJumpTravelDuration)
        {
            transform.position = lockedTargetPosition;
            ApplyLandingDamage();
            SetPhase(Phase.LandingRecovery);
        }
    }

    private void UpdateLandingRecovery()
    {
        if (phaseTimer >= landingRecoveryDuration + landingReturnDuration)
        {
            SetPhase(Phase.Rest);
        }
    }

    private void SetPhase(Phase nextPhase)
    {
        phase = nextPhase;
        phaseTimer = 0f;
        currentState = GetStateForPhase(nextPhase);
        if (nextPhase == Phase.LeftPullBack || nextPhase == Phase.RightTelegraph)
        {
            lockedTargetPosition = GetPlayerPosition();
        }
        if (nextPhase == Phase.ShortJumpRise)
        {
            if (completedShortJumps >= shortJumpCount)
            {
                completedShortJumps = 0;
            }
        }
        if (nextPhase == Phase.Rest)
        {
            RestoreHandTransforms();
        }
    }

    private CrabBoss2State GetStateForPhase(Phase currentPhase)
    {
        switch (currentPhase)
        {
            case Phase.Inactive:
                return CrabBoss2State.Inactive;
            case Phase.Rest:
                return IsPlayerOutsideAttackRange() ? CrabBoss2State.Approach : CrabBoss2State.Rest;
            case Phase.RightWindUp:
            case Phase.RightTelegraph:
            case Phase.RightSlam:
                return CrabBoss2State.Attack1;
            case Phase.LeftPullBack:
            case Phase.LeftTelegraph:
            case Phase.LeftThrust:
                return CrabBoss2State.Attack2;
            case Phase.ShortJumpRise:
            case Phase.ShortJumpTravel:
            case Phase.FinalJumpRise:
            case Phase.FinalJumpTravel:
                return CrabBoss2State.Attack3;
            default:
                return CrabBoss2State.Recovery;
        }
    }

    private void MoveTowardPlayer()
    {
        Vector3 target = GetPlayerPosition();
        Vector3 flatOffset = target - transform.position;
        flatOffset.y = 0f;
        if (flatOffset.sqrMagnitude <= attackRange * attackRange)
        {
            TurnToward(flatOffset);
            return;
        }

        Vector3 next = Vector3.MoveTowards(transform.position, target, approachSpeed * Time.deltaTime);
        next.y = transform.position.y;
        transform.position = next;
        TurnToward(flatOffset);
    }

    private bool IsPlayerOutsideAttackRange()
    {
        Vector3 offset = GetPlayerPosition() - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude > attackRange * attackRange;
    }

    private void TurnTowardPlayer()
    {
        Vector3 direction = GetPlayerPosition() - transform.position;
        direction.y = 0f;
        TurnToward(direction);
    }

    private void TurnToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void SpawnRightSlamCollider()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefab/CrabBoss/Atk1Collider");
        if (prefab == null)
        {
            Debug.LogWarning("CrabBoss2.0 could not find Prefab/CrabBoss/Atk1Collider.", this);
            return;
        }

        Vector3 position = transform.TransformPoint(rightSlamColliderLocalOffset);
        Instantiate(prefab, position, transform.rotation);
    }

    private void CheckLeftThrustHit()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = DistanceToSegment(playerTransform.position, leftThrustStartWorldPosition, lockedTargetPosition);
        if (distance <= leftAttackRadius)
        {
            Debug.Log("CrabBoss2.0 left rocket hit player.", this);
        }
    }

    private void ApplyLandingDamage()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, lockedTargetPosition);
        if (distance > landingDamageRadius)
        {
            return;
        }

        float damage = Mathf.Lerp(landingMaxDamage, landingMinDamage, distance / Mathf.Max(0.01f, landingDamageRadius));
        Debug.Log("CrabBoss2.0 landing damage: " + damage, this);
    }

    private float Progress(float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(phaseTimer / duration);
    }

    private Vector3 GetPlayerPosition()
    {
        if (playerTransform == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        return playerTransform != null ? playerTransform.position : transform.position;
    }

    private bool HasLeftHand()
    {
        return !leftHandState.IsDestroyed && leftHand != null && leftHand.gameObject.activeSelf;
    }

    private bool HasRightHand()
    {
        return !rightHandState.IsDestroyed && rightHand != null && rightHand.gameObject.activeSelf;
    }

    private void ApplyHandStates()
    {
        EnsureHandStates();
        EnsureHandReferences();
        SetHandActive(leftHand, !leftHandState.IsDestroyed);
        SetHandActive(rightHand, !rightHandState.IsDestroyed);
    }

    private void SetHandActive(CrabHand hand, bool active)
    {
        if (hand != null && hand.gameObject.activeSelf != active)
        {
            hand.gameObject.SetActive(active);
        }
    }

    private void EnsureHandReferences()
    {
        if (leftHand == null || rightHand == null)
        {
            CrabHand[] hands = GetComponentsInChildren<CrabHand>(true);
            foreach (CrabHand hand in hands)
            {
                string handName = hand.name.ToLowerInvariant();
                if (leftHand == null && handName.Contains("left"))
                {
                    leftHand = hand;
                }
                else if (rightHand == null && handName.Contains("right"))
                {
                    rightHand = hand;
                }
            }

            foreach (CrabHand hand in hands)
            {
                if (leftHand == null)
                {
                    leftHand = hand;
                }
                else if (rightHand == null && hand != leftHand)
                {
                    rightHand = hand;
                }
            }
        }

        if (!defaultsCached && leftHand != null && rightHand != null)
        {
            leftHandDefaultLocalPosition = leftHand.transform.localPosition;
            rightHandDefaultLocalPosition = rightHand.transform.localPosition;
            leftHandDefaultLocalRotation = leftHand.transform.localRotation;
            rightHandDefaultLocalRotation = rightHand.transform.localRotation;
            defaultsCached = true;
        }
    }

    private void EnsureHandStates()
    {
        if (leftHandState == null)
        {
            leftHandState = new CrabBoss2HandState();
        }
        if (rightHandState == null)
        {
            rightHandState = new CrabBoss2HandState();
        }
        if (bodyHealth == null)
        {
            bodyHealth = new CrabBoss2HealthState();
        }
    }

    private void RestoreHandTransforms()
    {
        if (!defaultsCached)
        {
            return;
        }
        if (leftHand != null)
        {
            leftHand.transform.localPosition = leftHandDefaultLocalPosition;
            leftHand.transform.localRotation = leftHandDefaultLocalRotation;
        }
        if (rightHand != null)
        {
            rightHand.transform.localPosition = rightHandDefaultLocalPosition;
            rightHand.transform.localRotation = rightHandDefaultLocalRotation;
        }
    }

    private void StopPhysicsMotion()
    {
        if (bossRigidbody == null)
        {
            bossRigidbody = GetComponent<Rigidbody>();
        }
        if (bossRigidbody != null)
        {
            bossRigidbody.isKinematic = true;
            bossRigidbody.useGravity = false;
            bossRigidbody.linearVelocity = Vector3.zero;
            bossRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector3.Distance(point, start);
        }
        float progress = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * progress);
    }
}
