using OrderManager.Application.Models;
using OrderManager.Application.Repositories;
using OrderManager.Application.Services.Interfaces;
using OrderManager.Domain.Dates;
using OrderManager.Domain.Entites;
using OrderManager.Domain.Enums;
using OrderManager.Domain.Errors;
using OrderManager.Domain.OperationResult;
using OrderManager.Domain.ValueObjects;

namespace OrderManager.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly IProductRepository _productRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OrderService(
            IOrderRepository orderRepository,
            IRestaurantRepository restaurantRepository,
            IProductRepository productRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _restaurantRepository = restaurantRepository ?? throw new ArgumentNullException(nameof(restaurantRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        }

        public async Task<Result<OrderDto>> CreateOrder(OrderDto orderDto)
        {
            var restaurant = await _restaurantRepository.GetRestaurantById(orderDto.RestaurantId);
            var products = await _productRepository.GetProductsByIds(orderDto.OrderItems.Select(x => x.ProductId));
            var specialOffers = await _productRepository.GetSpecialOffersByProductIds(orderDto.OrderItems.Select(x => x.ProductId));

            if (restaurant is null)
            {
                return Result<OrderDto>.Failure(Errors.Restaurant.RestaurantNotFound());
            }

            if (products.Count() != orderDto.OrderItems.Count())
            {
                return Result<OrderDto>.Failure(Errors.Product.ProductNotFound());
            }

            var orderItems = CreateOrderItems(orderDto, products, specialOffers);
            var order = Order.Create(orderItems, restaurant!, _dateTimeProvider.UtcNow());

            await _orderRepository.CreateOrder(order);
            orderDto.Id = order.Id;

            return Result<OrderDto>.Success(orderDto);
        }

        public async Task<OrderDto?> GetOrderById(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);

            return order is null
                ? null
                : new OrderDto()
                {
                    Id = order.Id,
                    DiscountAmount = order.DiscountAmount,
                    TotalAmount = order.TotalAmount
                };
        }

        public async Task<Result<OrderDto>> CheckoutOrder(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);

            if (order is null)
            {
                return Result<OrderDto>.Failure(Errors.Order.OrderNotFound());
            }

            var result = order!.CheckoutOrder(_dateTimeProvider.UtcNow());

            if (!result.IsSuccessful)
            {
                return Result<OrderDto>.Failure(result.Error);
            }

            await _orderRepository.UpdateOrder(order);
            return Result<OrderDto>.Success(CreateOrderDto(order));
        }

        private IList<OrderItem> CreateOrderItems(OrderDto orderDto, IEnumerable<Product> products, IEnumerable<SpecialOffer> specialOffers)
        {
            var orderItems = new List<OrderItem>();

            foreach (var item in orderDto.OrderItems)
            {
                var product = products.Single(x => x.Id == item.ProductId);
                orderItems.Add(CreateOrderItem(specialOffers, product, new Quantity(item.Quantity)));
            }

            return orderItems;
        }

        private OrderItem CreateOrderItem(IEnumerable<SpecialOffer> specialOffers, Product product, Quantity quantity)
        {
            decimal price;
            decimal discountAmount = 0;
            if (product.ProductType == ProductType.SpecialOffer)
            {
                var specialOffer = specialOffers.Single(x => x.Id == product.Id);
                price = specialOffer.PriceWithDiscount;
                discountAmount = specialOffer.DiscountAmount;
            }
            else
            {
                price = product.Price;
            }

            return new OrderItem(product, quantity, price, discountAmount);
        }

        private OrderDto CreateOrderDto(Order order)
        {
            return new OrderDto()
            {
                Id = order.Id,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount
            };
        }
    }
}
