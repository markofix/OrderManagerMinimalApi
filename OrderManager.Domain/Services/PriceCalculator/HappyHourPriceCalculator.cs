using OrderManager.Domain.Entites;

namespace OrderManager.Domain.Services.PriceCalculator
{
    internal class HappyHourPriceCalculator : IPriceCalculator
    {
        private const int HAPPY_HOUR_DISCOUNT_PERCENTAGE = 20;

        public decimal CalculateDiscountAmount(IEnumerable<OrderItem> orderItems)
        {
            var totalAmountWithoutDiscount = orderItems.Sum(x => x.Amount);
            var discountAmount = orderItems.Sum(x => x.DiscountAmount);
            var amountToApplyDiscount = totalAmountWithoutDiscount - discountAmount;
            var happyHourDiscount = amountToApplyDiscount * HAPPY_HOUR_DISCOUNT_PERCENTAGE / 100;
            return discountAmount + happyHourDiscount;
        }

        public decimal CalculateTotalAmount(IEnumerable<OrderItem> orderItems)
        {
            var totalAmount = orderItems.Sum(x => x.Amount);
            var happyHourDiscount = totalAmount * HAPPY_HOUR_DISCOUNT_PERCENTAGE / 100;
            return totalAmount - happyHourDiscount;
        }
    }
}
