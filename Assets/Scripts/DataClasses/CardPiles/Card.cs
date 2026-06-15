using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses.CardEffects;
using DataClasses.Enums;
using UnityEngine;

namespace DataClasses.CardPiles
{
    public class Card
    {
        // IMMUTABLE IDENTITY
        // Set at creation and never changed. Use these when you need to know
        // what the card fundamentally *is* (e.g. deck building, round resets).

        public Suit OriginalSuit { get; }
        public Rank OriginalRank { get; }

        // MUTABLE PLAY-STATE
        // Changed during a round (e.g. a wild card's suit is locked in after
        // the player chooses a colour). Reset to the originals via
        // ResetActiveState(), which is called on reshuffle.

        public Suit ActiveSuit { get; set; }
        public Rank ActiveRank { get; set; }

        // OTHER FIELDS

        public bool IsStartingCard { get; set; }

        public IReadOnlyList<CardEffect> Effects { get; }

        // Used for things like keeping track of random rotation, modifier mapping, etc.
        public Guid Guid;

        // DISCARD PILE CACHED LAYOUT
        // Computed once when the card is first laid out in the discard pile and
        // reused on every subsequent render; avoids re-seeding the PRNG each frame.
        // Null means the values have not been baked yet (card has never been in the
        // discard pile, or it was reset via ResetDiscardLayout).

        public float? CachedDiscardRotation;
        public Vector2? CachedDiscardDisplacement;

        public Card(Suit suit, Rank rank, bool isStartingCard, params CardEffect[] effects)
        {
            OriginalSuit = suit;
            OriginalRank = rank;
            ActiveSuit   = suit;
            ActiveRank   = rank;
            IsStartingCard = isStartingCard;
            Effects = effects.ToList();
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Restores ActiveSuit and ActiveRank to the card's original values.
        /// Call this when reshuffling the discard pile back into the draw pile so
        /// wild cards don't carry a previously-chosen suit into the next round.
        /// </summary>
        public void ResetActiveState()
        {
            ActiveSuit = OriginalSuit;
            ActiveRank = OriginalRank;
        }

        /// <summary>
        /// Clears the cached discard-pile layout values so they will be recomputed
        /// the next time the card is laid out in the discard pile. Call this
        /// alongside ResetActiveState() during a reshuffle.
        /// </summary>
        public void ResetDiscardLayout()
        {
            CachedDiscardRotation    = null;
            CachedDiscardDisplacement = null;
        }

        public override string ToString()
        {
            return $"{{ suit: {ActiveSuit}, rank: {ActiveRank}, isStartingCard: {IsStartingCard}. effects: {Effects.Count} }}";
        }
    }
}
