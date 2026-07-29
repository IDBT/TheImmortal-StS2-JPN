using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MokouMod.MokouModCode.Character;
using MokouMod.MokouModCode.Extensions;
using MokouMod.MokouModCode.Powers;
using MokouMod.MokouModCode.Scripts;

namespace MokouMod.MokouModCode.Cards;

[Pool(typeof(MokouModCardPool))]
public abstract class MokouModCard : ConstructedCardModel
{
    protected MokouModCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        WithTip(new TooltipSource(card => new HoverTip(new LocString("static_hover_tips", "MOKOUMOD-ARTIST-TITLE"), new LocString("cards", Id.Entry + ".artist"))));
    }

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    // public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190 

    //Uses card_portraits/card_name.png as image path. These should be smaller images.

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    public virtual Character.MokouMod.Animation Anim => Character.MokouMod.Animation.None;

    public bool IgniteActive { get; private set; }
    public bool FuryActive { get; private set; }
    public bool EmberActive { get; private set; }

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;

        if (Keywords.Contains(MokouModKeywords.Ignite))
            IgniteActive = TriggeredIgnite(this, DynamicVars.TryGetValue("Ignite", out var v) ? v.IntValue : 0);
        if (Keywords.Contains(MokouModKeywords.Fury))
            FuryActive = TriggeredFury(this);
        if (Keywords.Contains(MokouModKeywords.Ember))
            EmberActive = TriggeredEmber(this);

        // 1. Snapshot valid fuel cards currently in hand BEFORE drawing or discarding happens
        var fuelCardsToTrigger = new List<MokouModFuelCard>();
        if (IgniteActive || FuryActive || EmberActive)
            foreach (var card in PileType.Hand.GetPile(player).Cards)
                if (card is MokouModFuelCard fuelCard)
                    fuelCardsToTrigger.Add(fuelCard);

        // 2. Execute the card actions (e.g., Drawing cards)
        await OnPlayMokou(choiceContext, cardPlay);

        // 3. Trigger ONLY the fuel cards that were present initially and are still validly in hand
        if (IgniteActive || FuryActive || EmberActive)
            foreach (var fuelCard in fuelCardsToTrigger.Where(fuelCard => PileType.Hand.GetPile(player).Cards.Contains(fuelCard)))
                await fuelCard.TriggerFuel(player);

        IgniteActive = false;
        FuryActive = false;
        EmberActive = false;
    }

    protected virtual Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public static bool TriggeredIgnite(MokouModCard card, int requiredBurn)
    {
        if (card.Owner.Creature.CombatState == null || !card.Keywords.Contains(MokouModKeywords.Ignite)) return false;

        return card.Owner.Creature.CombatState.Players.Any(p => p.Creature.GetPowerAmount<BurnPower>() >= requiredBurn) || card.Owner.Creature.CombatState.HittableEnemies.Any(e => e.GetPowerAmount<BurnPower>() >= requiredBurn);
    }

    public static bool TriggeredFury(MokouModCard card)
    {
        if (card.Owner.Creature.CombatState == null || !card.Keywords.Contains(MokouModKeywords.Fury)) return false;

        return CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any(e => e.HappenedThisTurn(card.Owner.Creature.CombatState) && e.Receiver == card.Owner.Creature && e.Result.UnblockedDamage > 0);
    }

    public static bool TriggeredEmber(MokouModCard card)
    {
        if (card.Owner.Creature.CombatState == null || !card.Keywords.Contains(MokouModKeywords.Ember)) return false;

        return card.Owner.Creature.CurrentHp <= card.Owner.Creature.MaxHp * 0.5f || MokouKeywordStateRegistry.Get(card.Owner).emberTriggeredThisCombat;
    }

    public static decimal CalculateNonLethal(Creature creature, decimal hpLoss)
    {
        return creature.CurrentHp - hpLoss < 1 ? creature.CurrentHp - 1 : hpLoss;
    }

    public static EnchantmentModel? Enchant(
        EnchantmentModel enchantment,
        CardModel card,
        decimal amount = 1M)
    {
        enchantment.AssertMutable();
        if (!enchantment.CanEnchant(card))
            throw new InvalidOperationException($"Cannot enchant {card.Id} with {enchantment.Id}.");
        if (card.Enchantment == null)
        {
            card.EnchantInternal(enchantment, amount);
            enchantment.ModifyCard();
        }
        else
        {
            if (!(card.Enchantment.GetType() == enchantment.GetType()))
                throw new InvalidOperationException(
                    $"Cannot enchant {card.Id} with {enchantment.Id} because it already has enchantment {card.Enchantment.Id}.");
            card.Enchantment.Amount += (int)amount;
        }

        card.FinalizeUpgradeInternal();
        return card.Enchantment;
    }
}