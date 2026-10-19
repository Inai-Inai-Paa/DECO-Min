using NUnit.Framework;

public class CrabBoss2AttackSelectorTests
{
    [Test]
    public void SelectAttack_UsesRightSlamWhenOnlyRightHandRemains()
    {
        Assert.AreEqual(
            CrabBoss2Attack.RightSlam,
            CrabBoss2AttackSelector.SelectAttack(true, false, 0));
    }

    [Test]
    public void SelectAttack_UsesLeftRocketWhenOnlyLeftHandRemains()
    {
        Assert.AreEqual(
            CrabBoss2Attack.LeftRocket,
            CrabBoss2AttackSelector.SelectAttack(false, true, 0));
    }

    [Test]
    public void SelectAttack_UsesJumpSequenceWhenNoHandsRemain()
    {
        Assert.AreEqual(
            CrabBoss2Attack.JumpSequence,
            CrabBoss2AttackSelector.SelectAttack(false, false, 0));
    }

    [Test]
    public void SelectAttack_AlternatesAvailableArmAttacksWhenBothHandsRemain()
    {
        Assert.AreEqual(
            CrabBoss2Attack.RightSlam,
            CrabBoss2AttackSelector.SelectAttack(true, true, 0));
        Assert.AreEqual(
            CrabBoss2Attack.LeftRocket,
            CrabBoss2AttackSelector.SelectAttack(true, true, 1));
    }
}
