using OrderManager.Application.Models;
using OrderManager.Domain.OperationResult;

namespace OrderManager.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Result<OrderDto>> CreateOrder(OrderDto orderDto);
        Task<OrderDto?> GetOrderById(int orderId);
        Task<Result<OrderDto>> CheckoutOrder(int orderId);
    }
}
