using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A gift card that can be applied to a basket to discount it.
    /// </summary>
    [Serializable]
    public class GiftCard
    {
        public string card_number;

        public GiftCard(string cardNumber)
        {
            card_number = cardNumber;
        }
    }
}
