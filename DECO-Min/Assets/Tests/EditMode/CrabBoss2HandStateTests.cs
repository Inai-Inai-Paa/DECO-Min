using NUnit.Framework;

public class CrabBoss2HandStateTests
{
    [Test]
    public void ApplyDamage_DestroysHandWhenHealthReachesZero()
    {
        CrabBoss2HandState hand = new CrabBoss2HandState(100);

        hand.ApplyDamage(100);

        Assert.AreEqual(0, hand.CurrentHealth);
        Assert.IsTrue(hand.IsDestroyed);
    }

    [Test]
    public void Restore_ReturnsHandToMaximumHealth()
    {
        CrabBoss2HandState hand = new CrabBoss2HandState(100);
        hand.ApplyDamage(100);

        hand.Restore();

        Assert.AreEqual(100, hand.CurrentHealth);
        Assert.IsFalse(hand.IsDestroyed);
    }

    [Test]
    public void CrabBoss2_DoesNotInheritCrabBoss()
    {
        Assert.IsFalse(typeof(CrabBoss).IsAssignableFrom(typeof(CrabBoss2_0)));
    }

    [Test]
    public void DamageBody_OnlyWorksAfterBothHandsAreDestroyed()
    {
        UnityEngine.GameObject bossObject = new UnityEngine.GameObject("CrabBoss2Test");
        CrabBoss2_0 boss = bossObject.AddComponent<CrabBoss2_0>();

        boss.DamageBody(25);
        Assert.AreEqual(1000, boss.BodyCurrentHealth);

        boss.DestroyLeftHand();
        boss.DestroyRightHand();
        boss.DamageBody(25);

        Assert.AreEqual(975, boss.BodyCurrentHealth);
        UnityEngine.Object.DestroyImmediate(bossObject);
    }
}
