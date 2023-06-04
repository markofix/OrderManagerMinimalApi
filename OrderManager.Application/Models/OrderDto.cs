#nullable disable
namespace OrderManager.Application.Models
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public IEnumerable<OrderItemDto> OrderItems { get; set; }
    }
}
