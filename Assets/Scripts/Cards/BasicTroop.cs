using UnityEngine;

public class BasicTroop : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 2;
        this.health = 2;
        this.extraText = "";
        Math();
    }
}
