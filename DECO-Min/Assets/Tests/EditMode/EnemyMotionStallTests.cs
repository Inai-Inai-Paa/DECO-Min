using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMotionStallTests
{
    [Test]
    public void HasProgress_IgnoresVerticalMovementBelowThreshold()
    {
        Vector3 start = Vector3.zero;

        Assert.IsFalse(EnemyMotionStall.HasProgress(start, new Vector3(0.04f, 3.0f, 0.0f)));
        Assert.IsTrue(EnemyMotionStall.HasProgress(start, new Vector3(0.0f, 0.0f, 0.05f)));
    }

    [Test]
    public void ShouldGiveUp_OnlyAfterTheLimitWithoutProgress()
    {
        float stall = 0.0f;
        const float delta = 0.5f;

        stall = EnemyMotionStall.Advance(stall, delta);
        Assert.IsFalse(EnemyMotionStall.ShouldGiveUp(stall, EnemyMotionStall.GiveUpSeconds));

        stall = EnemyMotionStall.Advance(stall, EnemyMotionStall.GiveUpSeconds);
        Assert.IsTrue(EnemyMotionStall.ShouldGiveUp(stall, EnemyMotionStall.GiveUpSeconds));
    }

    [Test]
    public void NavigationCalls_DoNotThrowWhenAgentIsOffNavMesh()
    {
        GameObject enemyObject = new GameObject("OffMeshEnemy");
        TackleEnemy enemy = enemyObject.AddComponent<TackleEnemy>();
        NavMeshAgent agent = enemyObject.GetComponent<NavMeshAgent>();
        typeof(Enemy).GetField("_agent", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(enemy, agent);

        try
        {
            Assert.IsFalse(agent.isOnNavMesh);
            Assert.DoesNotThrow(() => enemy.StopMove());
            Assert.DoesNotThrow(() => enemy.ResumeMove());
            Assert.DoesNotThrow(() => enemy.SetMoveDestination(Vector3.forward));
            Assert.DoesNotThrow(() => enemy.StepToward(Vector3.forward, 3.0f));
            Assert.IsFalse(enemy.IsAgentFollowingPath());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyObject);
        }
    }
}
