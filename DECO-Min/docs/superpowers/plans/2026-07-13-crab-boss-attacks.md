# Crab Boss Attacks 2 and 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add CrabBoss Attack2 and Attack3 boss-flow behavior without taking over player damage, hand weak-point, or teammate-owned combat integration.

**Architecture:** Keep behavior inside the existing `CrabBoss` timed state pattern. Add small helper methods for attack path distance and landing damage calculation so the math can be inspected independently from the attack flow.

**Tech Stack:** Unity 6 style C# scripts, `Rigidbody.linearVelocity`, MSBuild compile check for generated Unity C# projects.

## Global Constraints

- Attack2 and Attack3 match Attack1 integration level: movement, timing, range detection, and `Debug.Log`, not formal damage.
- Attack2 locks the player position at attack start, prepares for 0.4 seconds, thrusts both hands, leaves them extended for 2 seconds, then recovers.
- Attack3 jumps, locks the player position after 1 second, lands at 3 seconds, and logs distance-scaled damage.
- Attack3 damage formula is linear: center is 100, outer edge is 1.
- Rest chooses among Attack1, Attack2, and Attack3.

---

### Task 1: Add Attack Math Helpers

**Files:**
- Modify: `Assets/Scripts/GameObject/Character/CrabBoss/CrabBoss.cs`

**Interfaces:**
- Produces: `CrabBoss.CalculateLandingDamage(float distance, float radius, float minDamage, float maxDamage)`.
- Produces: `CrabBoss.DistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)`.

- [ ] **Step 1: Add distance and damage helpers**

```csharp
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
```

- [ ] **Step 2: Check expected helper outputs by inspection**

Expected: distance 0 at radius 10 returns 100 damage, distance 5 returns 50.5, distance 10 returns 1, distance beyond radius returns 0.

### Task 2: Implement Attack2 and Attack3 Boss Flow

**Files:**
- Modify: `Assets/Scripts/GameObject/Character/CrabBoss/CrabBoss.cs`

**Interfaces:**
- Produces: Attack2 state steps for lock, prep, thrust, recovery.
- Produces: Attack3 state steps for jump, lock, land, area log.

- [ ] **Step 1: Add serialized tuning fields**

Add fields for Attack2 prep time, thrust time, recovery time, attack radius, hand pull-back offset, and Attack3 jump/lock/land timings, jump height, radius, min damage, max damage.

- [ ] **Step 2: Cache original transforms**

Cache both hand original local positions and the boss original ground Y in `Start()`.

- [ ] **Step 3: Replace fixed Rest attack selection**

Change `Rest()` so it randomly selects Attack1, Attack2, or Attack3.

- [ ] **Step 4: Implement Attack2 phases**

Use `StateCountInState` phases:

0. lock player position and hand start positions.
1. prepare hands backward for 0.4 seconds.
2. thrust hands toward locked position.
3. wait extended for 2 seconds and log player hits along the hand paths.
4. recover hands and return to Rest.

- [ ] **Step 5: Implement Attack3 phases**

Use `StateCountInState` phases:

0. start jump and cache ground position.
1. at 1 second, lock player position.
2. at 3 seconds, land at locked position and log radial damage.
3. return to Rest.

- [ ] **Step 6: Run build to verify compile readiness**

Run: `dotnet build DECO-Min.sln`

Expected: build exits 0. Existing Unity package warnings may remain.

### Task 3: Verify Compile Readiness and Scope

**Files:**
- Inspect: `Assets/Scripts/GameObject/Character/CrabBoss/CrabBoss.cs`
- Inspect: `Assets/Tests/EditMode/CrabBossAttackMathTests.cs`

**Interfaces:**
- Consumes: completed Task 1 and Task 2.
- Produces: final confidence that the code is in the requested scope.

- [ ] **Step 1: Check for C# syntax mistakes**

Run a local compiler or Unity project generation check if available.

- [ ] **Step 2: Scope check**

Confirm no player damage system, hand weak-point system, or teammate-owned combat behavior was added.

- [ ] **Step 3: Manual play-mode checklist**

In Unity, verify Attack2 hand prep/thrust/recovery, Attack3 jump/lock/landing log, and Attack1 still works.
