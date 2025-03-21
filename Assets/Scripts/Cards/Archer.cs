using UnityEngine;

public class Archer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 3;
        this.health = 2;
        this.extraText = "If there's no opposing Troop here, this gets +4 Power.";
        this.artistText = "Harald Lieske\nDominion: Allies\n(Archer)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        MovingTroop otherTroop = Manager.inst.FindOpposingTroop(troop.player, troop.currentRow);
        if (otherTroop != null)
            return (0, 0);
        else
            return (4, 0);
    }
}
