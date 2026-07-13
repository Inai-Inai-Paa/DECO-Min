using UnityEngine;

enum CrabState
{
	Idle,
	Rest,
	Attack1,
	Attack2,
	Attack3,
	Stunned,
	Dead
}

public class CrabBoss : Character
{

	[Header("Parameters")]
	[SerializeField] int StateCountInState = 0;
	public float moveSpeed = 3f;
	private bool isWaiting = false;
	private float StateTimer = 0f;


	[Header("Child Object")]
	[SerializeField] CrabHand LeftHands;
	[SerializeField] CrabHand RightHands;

	[Header("Boss Settings")]
	[SerializeField] CrabState NowState;
	public bool IsActive;

	[Header("Attack1")]
	[SerializeField] GameObject Atk1ColPrefab;
	GameObject Atk1Col;
	[SerializeField] bool IsAtk1Active;
	[SerializeField] bool IsLeft;
	[SerializeField] float ChargeTime = 3f;
	[SerializeField] float RestTime = 4f;
	[SerializeField] Vector3 Atk1LeftColliderLocalOffset = new Vector3(-7.5f, 0f, 0f);
	[SerializeField] Vector3 Atk1RightColliderLocalOffset = new Vector3(7.5f, 0f, 0f);

	[Header("Attack2")]
	[SerializeField] float Atk2PrepareTime = 0.4f;
	[SerializeField] float Atk2ThrustTime = 0.25f;
	[SerializeField] float Atk2RecoveryTime = 2f;
	[SerializeField] float Atk2PullBackDistance = 2f;
	[SerializeField] float Atk2AttackRadius = 1.5f;

	[Header("Attack3")]
	[SerializeField] float Atk3LockTime = 1f;
	[SerializeField] float Atk3LandTime = 3f;
	[SerializeField] float Atk3JumpHeight = 6f;
	[SerializeField] float Atk3DamageRadius = 8f;
	[SerializeField] float Atk3MinDamage = 1f;
	[SerializeField] float Atk3MaxDamage = 100f;



	private Rigidbody crabRb;
	private Rigidbody leftHandRb;
	private Rigidbody rightHandRb;
	private Transform playerTransform;
	private PhysicsMaterial zeroBounceMaterial;

	private Vector3 leftHandDefaultLocalPosition;
	private Vector3 rightHandDefaultLocalPosition;
	private Quaternion leftHandDefaultLocalRotation;
	private Quaternion rightHandDefaultLocalRotation;
	private Vector3 atk2LockedPlayerPosition;
	private Vector3 atk2LeftStartPosition;
	private Vector3 atk2RightStartPosition;
	private Vector3 atk2LeftPrepareLocalPosition;
	private Vector3 atk2RightPrepareLocalPosition;
	private Vector3 atk2LeftTargetLocalPosition;
	private Vector3 atk2RightTargetLocalPosition;
	private bool atk2LoggedHit;

	private Vector3 atk3StartPosition;
	private Vector3 atk3LockedLandingPosition;
	private bool atk3LockedTarget;



	protected override void Start()
	{
		base.Start();

		crabRb = GetComponent<Rigidbody>();
		SetupBossRigidbody();
		SetupZeroBouncePhysicsMaterial();
		leftHandDefaultLocalPosition = LeftHands.transform.localPosition;
		rightHandDefaultLocalPosition = RightHands.transform.localPosition;
		leftHandDefaultLocalRotation = LeftHands.transform.localRotation;
		rightHandDefaultLocalRotation = RightHands.transform.localRotation;
		leftHandRb = LeftHands.GetComponent<Rigidbody>();
		rightHandRb = RightHands.GetComponent<Rigidbody>();
		SetupHandRigidbody(leftHandRb);
		SetupHandRigidbody(rightHandRb);

	}

	protected override void Update()
	{
		baseVelocity = Vector3.zero;
		additionalVelocity = Vector3.zero;
		velocity = Vector3.zero;
		StopBossPhysicsMotion();

		if (!IsActive)
		{
			Idle();
		}
		else
		{
			switch (NowState)
			{
				case CrabState.Rest: Rest(); break;
				case CrabState.Attack1: CrabAtk1(); break;
				case CrabState.Attack2: CrabAtk2(); break;
				case CrabState.Attack3: CrabAtk3(); break;
				case CrabState.Stunned: Stunned(); break;
				case CrabState.Dead: Dead(); break;
			}
		}
		StateTimer += Time.deltaTime;
	}

	void Idle()
	{
		if (IsActive)
		{
			SetBossState(CrabState.Rest);
			Debug.Log("Idle Ended, Rest Start");
		}

	}

	void Rest()
	{
		if (StateTimer >= RestTime)
		{
			isWaiting = false;
			IsLeft = Random.value > 0.5f;
			CrabState nextAttack = GetRandomAttackState();
			SetBossState(nextAttack);
			Debug.Log("Rest Ended, " + nextAttack + " Start");
		}
	}

	CrabState GetRandomAttackState()
	{
		int attackIndex = Random.Range(0, 3);

		switch (attackIndex)
		{
			case 0: return CrabState.Attack1;
			case 1: return CrabState.Attack2;
			default: return CrabState.Attack3;
		}
	}


	//-----------------------------------------------------------
	//atk1	
	//-----------------------------------------------------------
	void CrabAtk1()
	{
		switch (StateCountInState)
		{
			case 0:
				ChargeAtk1();
				break;
			case 1:
				SlamAtk1();
				break;
			case 2:
				WaitAtk1Recovery();
				break;
		}
	}
	void ChargeAtk1()
	{
		GetAtk1Hand().SetHandMaterial(true);

		if (StateTimer < ChargeTime) return;

		IsAtk1Active = true;
		StateTimer = 0;
		Debug.Log("Attack1 Active");
		StateCountInState = 1;
	}
	void SlamAtk1()
	{
		float rotationAmount = StateTimer * 120 * 2;
		CrabHand attackHand = GetAtk1Hand();
		attackHand.transform.localRotation = GetAtk1HandDefaultLocalRotation() * Quaternion.Euler(rotationAmount, 0f, 0f);

		if (rotationAmount < 120f) return;

		StateTimer = 0;
		StateCountInState = 2;
		SpawnAtk1Collider();
	}
	void WaitAtk1Recovery()
	{
		if (StateTimer < RestTime) return;

		IsAtk1Active = false;
		SetBossState(CrabState.Rest);
		ResetAtk1Hands();
	}
	CrabHand GetAtk1Hand()
	{
		return IsLeft ? LeftHands : RightHands;
	}
	Quaternion GetAtk1HandDefaultLocalRotation()
	{
		return IsLeft ? leftHandDefaultLocalRotation : rightHandDefaultLocalRotation;
	}
	void SpawnAtk1Collider()
	{
		Vector3 localOffset = IsLeft ? Atk1LeftColliderLocalOffset : Atk1RightColliderLocalOffset;
		Vector3 spawnPosition = transform.TransformPoint(localOffset);
		Quaternion spawnRotation = transform.rotation;

		Atk1Col = Instantiate(Atk1ColPrefab, spawnPosition, spawnRotation);
		Atk1Col.SetActive(true);
	}
	void ResetAtk1Hands()
	{
		RightHands.transform.localRotation = rightHandDefaultLocalRotation;
		RightHands.SetHandMaterial(false);
		LeftHands.transform.localRotation = leftHandDefaultLocalRotation;
		LeftHands.SetHandMaterial(false);
	}

	//-----------------------------------------------------------
	//atk2
	//-----------------------------------------------------------
	void CrabAtk2()
	{
		switch (StateCountInState)
		{
			case 0:
				StartAtk2();
				break;
			case 1:
				PrepareAtk2();
				break;
			case 2:
				ThrustAtk2();
				break;
			case 3:
				WaitAtk2Recovery();
				break;
		}
	}

	void StartAtk2()
	{
		atk2LoggedHit = false;
		atk2LockedPlayerPosition = GetPlayerPosition();
		FacePosition(atk2LockedPlayerPosition);

		atk2LeftStartPosition = LeftHands.transform.position;
		atk2RightStartPosition = RightHands.transform.position;

		Vector3 pullDirection = transform.position - atk2LockedPlayerPosition;
		pullDirection.y = 0f;
		pullDirection = pullDirection.normalized;
		if (pullDirection == Vector3.zero) pullDirection = -transform.forward;

		atk2LeftPrepareLocalPosition = GetLocalPosition(LeftHands.transform, atk2LeftStartPosition + pullDirection * Atk2PullBackDistance);
		atk2RightPrepareLocalPosition = GetLocalPosition(RightHands.transform, atk2RightStartPosition + pullDirection * Atk2PullBackDistance);
		atk2LeftTargetLocalPosition = GetLocalPosition(LeftHands.transform, GetHandTargetPosition(atk2LockedPlayerPosition, atk2LeftStartPosition.y));
		atk2RightTargetLocalPosition = GetLocalPosition(RightHands.transform, GetHandTargetPosition(atk2LockedPlayerPosition, atk2RightStartPosition.y));

		LeftHands.SetHandMaterial(true);
		RightHands.SetHandMaterial(true);

		StateTimer = 0f;
		StateCountInState = 1;
		Debug.Log("Attack2 Locked Player Position: " + atk2LockedPlayerPosition);
	}

	void PrepareAtk2()
	{
		float progress = Mathf.Clamp01(StateTimer / Atk2PrepareTime);
		LeftHands.transform.localPosition = Vector3.Lerp(leftHandDefaultLocalPosition, atk2LeftPrepareLocalPosition, progress);
		RightHands.transform.localPosition = Vector3.Lerp(rightHandDefaultLocalPosition, atk2RightPrepareLocalPosition, progress);

		if (StateTimer < Atk2PrepareTime) return;

		StateTimer = 0f;
		StateCountInState = 2;
		Debug.Log("Attack2 Thrust Start");
	}

	void ThrustAtk2()
	{
		float progress = Mathf.Clamp01(StateTimer / Atk2ThrustTime);
		LeftHands.transform.localPosition = Vector3.Lerp(atk2LeftPrepareLocalPosition, atk2LeftTargetLocalPosition, progress);
		RightHands.transform.localPosition = Vector3.Lerp(atk2RightPrepareLocalPosition, atk2RightTargetLocalPosition, progress);
		LogAtk2HitIfPlayerInPath();

		if (StateTimer < Atk2ThrustTime) return;

		StateTimer = 0f;
		StateCountInState = 3;
		Debug.Log("Attack2 Recovery Window Start");
	}

	void WaitAtk2Recovery()
	{
		LeftHands.transform.localPosition = atk2LeftTargetLocalPosition;
		RightHands.transform.localPosition = atk2RightTargetLocalPosition;
		LogAtk2HitIfPlayerInPath();

		if (StateTimer < Atk2RecoveryTime) return;

		ResetAtk2Hands();
		SetBossState(CrabState.Rest);
	}

	void ResetAtk2Hands()
	{
		LeftHands.transform.localPosition = leftHandDefaultLocalPosition;
		RightHands.transform.localPosition = rightHandDefaultLocalPosition;
		LeftHands.SetHandMaterial(false);
		RightHands.SetHandMaterial(false);
	}

	void SetupHandRigidbody(Rigidbody handRb)
	{
		if (handRb == null) return;

		handRb.linearVelocity = Vector3.zero;
		handRb.angularVelocity = Vector3.zero;
		handRb.useGravity = false;
		handRb.isKinematic = true;
	}

	void SetupBossRigidbody()
	{
		if (crabRb == null) return;

		crabRb.linearVelocity = Vector3.zero;
		crabRb.angularVelocity = Vector3.zero;
		crabRb.useGravity = false;
		crabRb.isKinematic = true;
	}

	void StopBossPhysicsMotion()
	{
		if (crabRb == null || crabRb.isKinematic) return;

		crabRb.linearVelocity = Vector3.zero;
		crabRb.angularVelocity = Vector3.zero;
	}

	void SetupZeroBouncePhysicsMaterial()
	{
		zeroBounceMaterial = new PhysicsMaterial("CrabBossZeroBounce");
		zeroBounceMaterial.bounciness = 0f;
		zeroBounceMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
		zeroBounceMaterial.dynamicFriction = 0.6f;
		zeroBounceMaterial.staticFriction = 0.6f;
		zeroBounceMaterial.frictionCombine = PhysicsMaterialCombine.Average;

		Collider[] bossColliders = GetComponentsInChildren<Collider>();
		foreach (Collider bossCollider in bossColliders)
		{
			if (bossCollider == null || bossCollider.isTrigger) continue;

			bossCollider.material = zeroBounceMaterial;
		}
	}

	Vector3 GetLocalPosition(Transform targetTransform, Vector3 worldPosition)
	{
		if (targetTransform.parent == null) return worldPosition;

		return targetTransform.parent.InverseTransformPoint(worldPosition);
	}

	Vector3 GetWorldPosition(Transform targetTransform, Vector3 localPosition)
	{
		if (targetTransform.parent == null) return localPosition;

		return targetTransform.parent.TransformPoint(localPosition);
	}

	Vector3 GetHandTargetPosition(Vector3 lockedPlayerPosition, float handY)
	{
		return new Vector3(lockedPlayerPosition.x, handY, lockedPlayerPosition.z);
	}

	void LogAtk2HitIfPlayerInPath()
	{
		if (atk2LoggedHit) return;

		Transform player = GetPlayerTransform();
		if (player == null) return;

		Vector3 leftPlayerPosition = new Vector3(player.position.x, atk2LeftStartPosition.y, player.position.z);
		Vector3 rightPlayerPosition = new Vector3(player.position.x, atk2RightStartPosition.y, player.position.z);
		Vector3 leftTargetPosition = GetWorldPosition(LeftHands.transform, atk2LeftTargetLocalPosition);
		Vector3 rightTargetPosition = GetWorldPosition(RightHands.transform, atk2RightTargetLocalPosition);
		float leftDistance = DistanceToSegment(leftPlayerPosition, atk2LeftStartPosition, leftTargetPosition);
		float rightDistance = DistanceToSegment(rightPlayerPosition, atk2RightStartPosition, rightTargetPosition);

		if (leftDistance > Atk2AttackRadius && rightDistance > Atk2AttackRadius) return;

		atk2LoggedHit = true;
		Debug.Log("Player hit by Attack2 path!");
	}

	//-----------------------------------------------------------
	//atk3
	//-----------------------------------------------------------
	void CrabAtk3()
	{
		switch (StateCountInState)
		{
			case 0:
				StartAtk3();
				break;
			case 1:
				HoverAtk3();
				break;
		}
	}

	void StartAtk3()
	{
		atk3StartPosition = transform.position;
		atk3LockedLandingPosition = atk3StartPosition;
		atk3LockedTarget = false;
		StateTimer = 0f;
		StateCountInState = 1;
		Debug.Log("Attack3 Jump Start");
	}

	void HoverAtk3()
	{
		MoveBossTo(new Vector3(transform.position.x, atk3StartPosition.y + Atk3JumpHeight, transform.position.z));

		if (!atk3LockedTarget && StateTimer >= Atk3LockTime)
		{
			Vector3 playerPosition = GetPlayerPosition();
			atk3LockedLandingPosition = new Vector3(playerPosition.x, atk3StartPosition.y, playerPosition.z);
			atk3LockedTarget = true;
			Debug.Log("Attack3 Locked Player Position: " + atk3LockedLandingPosition);
		}

		if (StateTimer < Atk3LandTime) return;

		if (!atk3LockedTarget)
		{
			Vector3 playerPosition = GetPlayerPosition();
			atk3LockedLandingPosition = new Vector3(playerPosition.x, atk3StartPosition.y, playerPosition.z);
		}

		MoveBossTo(atk3LockedLandingPosition);
		LogAtk3LandingDamage();
		SetBossState(CrabState.Rest);
		Debug.Log("Attack3 Landed");
	}

	void MoveBossTo(Vector3 position)
	{
		transform.position = position;
		StopBossPhysicsMotion();
	}

	void LogAtk3LandingDamage()
	{
		Transform player = GetPlayerTransform();
		if (player == null) return;

		Vector3 playerGroundPosition = new Vector3(player.position.x, atk3LockedLandingPosition.y, player.position.z);
		float distance = Vector3.Distance(playerGroundPosition, atk3LockedLandingPosition);
		float damage = CalculateLandingDamage(distance, Atk3DamageRadius, Atk3MinDamage, Atk3MaxDamage);

		if (damage <= 0f) return;

		Debug.Log("Player hit by Attack3 landing! Distance: " + distance + ", Damage: " + damage);
	}

	void Stunned() { }
	void Dead() { }
	void Damage() { }

	Transform GetPlayerTransform()
	{
		if (playerTransform != null) return playerTransform;

		GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
		if (playerObject != null)
		{
			playerTransform = playerObject.transform;
			return playerTransform;
		}

		Player player = FindFirstObjectByType<Player>();
		if (player == null) return null;

		playerTransform = player.transform;
		return playerTransform;
	}

	Vector3 GetPlayerPosition()
	{
		Transform player = GetPlayerTransform();
		return player != null ? player.position : transform.position;
	}

	void FacePosition(Vector3 targetPosition)
	{
		Vector3 direction = targetPosition - transform.position;
		direction.y = 0f;

		if (direction.sqrMagnitude <= Mathf.Epsilon) return;

		transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		StopBossPhysicsMotion();
	}

	public static float CalculateLandingDamage(float distance, float radius, float minDamage, float maxDamage)
	{
		if (radius <= 0f || distance > radius) return 0f;

		float distanceRate = Mathf.Clamp01(distance / radius);
		return Mathf.Lerp(maxDamage, minDamage, distanceRate);
	}

	public static float DistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
	{
		Vector3 segment = segmentEnd - segmentStart;
		float segmentLengthSquared = segment.sqrMagnitude;

		if (segmentLengthSquared <= Mathf.Epsilon) return Vector3.Distance(point, segmentStart);

		float t = Vector3.Dot(point - segmentStart, segment) / segmentLengthSquared;
		t = Mathf.Clamp01(t);

		Vector3 closestPoint = segmentStart + segment * t;
		return Vector3.Distance(point, closestPoint);
	}

	void SetBossState(CrabState newState)
	{
		NowState = newState;
		StateTimer = 0f;
		StateCountInState = 0;
	}
}
