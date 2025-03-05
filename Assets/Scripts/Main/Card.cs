using UnityEngine;
using UnityEngine.UI;
using MyBox;
using System.Collections;
using Photon.Pun;

public class Card : PhotonCompatible
{

#region Variables

    public Button button { get; private set; }
    public Image border { get; private set; }
    public CardLayout layout { get; private set; }

    public string extraText { get; protected set; }
    public int coinCost { get; protected set; }

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        border = this.transform.Find("Border").GetComponent<Image>();
        button = GetComponent<Button>();
        layout = GetComponent<CardLayout>();
        this.transform.localScale = Vector3.Lerp(Vector3.one, Manager.inst.canvas.transform.localScale, 0.5f);
    }
    
    #endregion

#region Animations

    public IEnumerator MoveCard(Vector3 newPos, float waitTime, Vector3 newScale)
    {
        float elapsedTime = 0;
        Vector2 originalPos = this.transform.localPosition;
        Vector2 originalScale = this.transform.localScale;

        while (elapsedTime < waitTime)
        {
            this.transform.localPosition = Vector3.Lerp(originalPos, newPos, elapsedTime / waitTime);
            this.transform.localScale = Vector3.Lerp(originalScale, newScale, elapsedTime / waitTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localPosition = newPos;
    }

    public IEnumerator RevealCard(float totalTime)
    {
        if (this.layout.cg.alpha == 1)
            yield break;

        transform.localEulerAngles = new Vector3(0, 0, 0);
        float elapsedTime = 0f;

        Vector3 originalRot = this.transform.localEulerAngles;
        Vector3 newRot = new(0, 90, 0);

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(originalRot, newRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.layout.cg.alpha = 1;
        elapsedTime = 0f;

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(newRot, originalRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localEulerAngles = originalRot;
    }

    private void FixedUpdate()
    {
        try { this.border.SetAlpha(Manager.inst.opacity); } catch { }
    }

    #endregion

#region Layout

    public virtual Color MyColor()
    {
        return Color.white;
    }

#endregion

#region Gameplay

    public virtual bool CanPlayMe(Player player, bool pay)
    {
        return !pay || player.coins >= coinCost;
    }

    public virtual void OnPlayEffect(Player player, int logged)
    {
        player.PopStack();
    }

    public virtual void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
            card.OtherCardPlayed(entity, createdEntity, logged);
        Log.inst.RememberStep(player, StepType.UndoPoint, () => player.MayPlayCard());
    }

    #endregion

#region Abilities

    public virtual void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
    }

    public virtual void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
    }

    public virtual void StartOfCombat(Entity entity, int logged)
    {
    }

    public virtual void EndOfTurn(Entity entity, int logged)
    {
    }

    public virtual int CoinEffect(Player player, Entity entity, int logged)
    {
        return 0;
    }

    public virtual (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        return (0, 0);
    }

#endregion

}
