using NUnit.Framework;
using UnityEngine;

public class BossActiveAreaTests
{
    [Test]
    public void OnTriggerStay_IgnoresColliderWhenBossIsNotAssigned()
    {
        GameObject areaObject = new GameObject("BossActiveAreaTest");
        GameObject otherObject = new GameObject("OtherCollider");
        BoxCollider otherCollider = otherObject.AddComponent<BoxCollider>();

        BossActiveArea area = areaObject.AddComponent<BossActiveArea>();

        Assert.DoesNotThrow(() => area.SendMessage("OnTriggerStay", otherCollider, SendMessageOptions.RequireReceiver));

        Object.DestroyImmediate(otherObject);
        Object.DestroyImmediate(areaObject);
    }
}
