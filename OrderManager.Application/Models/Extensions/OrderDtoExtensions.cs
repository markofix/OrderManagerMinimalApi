namespace OrderManager.Application.Models.Extensions
{
    public static class OrderDtoExtensions
    {
        public static IEnumerable<int> ProductIds(this OrderDto orderDto)
        {
            return orderDto.OrderItems.Select(x => x.ProductId);
        }
    }
}
