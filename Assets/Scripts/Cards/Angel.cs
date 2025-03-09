using UnityEngine;

public class Angel : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 6;
        this.power = 3;
        this.health = 4;
        this.extraText = "End of turn: You get +1 Health per Troop you have (including this).";
        this.artistText = "Magali Villeneuve\nMTG: Oath of the Gatewatch\n(Linvala, the Preserver)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        int hasTroop = 0;
        for (int i = 0; i<Manager.inst.allRows.Count; i++)
        {
            if (Manager.inst.FindMyTroop(entity.player, i) != null)
                hasTroop++;
        }
        entity.player.myBase.ChangeHealthRPC(hasTroop, logged, this.name);
    }
}
