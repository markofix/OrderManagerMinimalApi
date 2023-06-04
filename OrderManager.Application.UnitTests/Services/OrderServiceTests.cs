using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrderManager.Application.Models;
using OrderManager.Application.Models.Extensions;
using OrderManager.Application.Repositories;
using OrderManager.Application.Services;
using OrderManager.Domain.Dates;
using OrderManager.Domain.Entites;
using OrderManager.Domain.Errors;
using Xunit;

namespace OrderManager.Application.UnitTests.Services
{
    public class OrderServiceTests
    {
        #region CreateOrder

        [Theory]
        [AutoMoqInlineData]
        public async Task CreateOrder_ValidRequest_CreateOrder(
            [Frozen] Mock<IProductRepository> productRepositoryMock,
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            [Frozen] Mock<IRestaurantRepository> restaurantRepositoryMock,
            [Frozen] Mock<IDateTimeProvider> dateTimeProviderMock,
            int specialOfferId,
            Restaurant restaurant,
            MainDish mainDish,
            MainDish mainDishSpecialOffer,
            Dessert dessertSpecialOffer,
            Drink drinkSpecialOffer,
            DateTime now,
            OrderDto orderDto,
            OrderService orderService)
        {
            orderDto.OrderItems = new List<OrderItemDto>()
            {
                new OrderItemDto()
                {
                    ProductId = mainDish.Id,
                    Quantity = 1,
                },
                new OrderItemDto()
                {
                    ProductId = specialOfferId,
                    Quantity = 1,
                }
            };

            var specialOfferProducts = new List<Product>()
            {
                mainDishSpecialOffer,
                dessertSpecialOffer,
                drinkSpecialOffer
            };
            var specialOffer = new SpecialOffer(specialOfferProducts, "Promo", 10)
            {
                Id = specialOfferId
            };
            var products = new List<Product>()
            {
                mainDish,
                specialOffer
            };

            productRepositoryMock
                .Setup(x => x.GetProductsByIds(orderDto.ProductIds()))
                .ReturnsAsync(products);

            productRepositoryMock
                .Setup(x => x.GetSpecialOffersByProductIds(orderDto.ProductIds()))
                .ReturnsAsync(new List<SpecialOffer>() { specialOffer });

            restaurantRepositoryMock
                .Setup(x => x.GetRestaurantById(orderDto.RestaurantId))
                .ReturnsAsync(restaurant);

            dateTimeProviderMock
                .Setup(x => x.UtcNow())
                .Returns(now);

            var result = await orderService.CreateOrder(orderDto);

            result.IsSuccessful.Should().BeTrue();
            orderRepositoryMock
                .Verify(x =>
                    x.CreateOrder(It.Is<Order>(order =>
                        order.OrderStatus == Domain.Enums.OrderStatus.Pending &&
                        order.TotalAmount == specialOffer.PriceWithDiscount + mainDish.Price &&
                        order.DiscountAmount == specialOffer.DiscountAmount &&
                        order.CreatedOnUtc == now &&
                        order.Restaurant == restaurant &&
                        order.OrderItems.All(item => products.Contains(item.Product)))),
                Times.Once);
        }

        [Theory]
        [AutoMoqInlineData]
        public async Task CreateOrder_RestaurantIsNull_ReturnsFailure(
            [Frozen] Mock<IRestaurantRepository> restaurantRepositoryMock,
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            OrderDto orderDto,
            OrderService orderService)
        {
            restaurantRepositoryMock
                .Setup(x => x.GetRestaurantById(orderDto.RestaurantId))
                .ReturnsAsync((Restaurant?)null);

            var result = await orderService.CreateOrder(orderDto);

            result.IsSuccessful.Should().BeFalse();
            result.Error.ErrorCode.Should().Be(Errors.Restaurant.RestaurantNotFound().ErrorCode);
            orderRepositoryMock
                .Verify(x => x.CreateOrder(It.IsAny<Order>()), Times.Never);
        }

        [Theory]
        [AutoMoqInlineData]
        public async Task CreateOrder_SomeProductsNotFound_ReturnsFailure(
            [Frozen] Mock<IProductRepository> productRepositoryMock,
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            MainDish mainDish,
            int specialOfferId,
            OrderDto orderDto,
            OrderService orderService)
        {
            orderDto.OrderItems = new List<OrderItemDto>()
            {
                new OrderItemDto()
                {
                    ProductId = mainDish.Id,
                    Quantity = 1,
                },
                new OrderItemDto()
                {
                    ProductId = specialOfferId,
                    Quantity = 1,
                }
            };

            var products = new List<Product>()
            {
                mainDish
            };

            productRepositoryMock
                .Setup(x => x.GetProductsByIds(orderDto.ProductIds()))
                .ReturnsAsync(products);

            var result = await orderService.CreateOrder(orderDto);

            result.IsSuccessful.Should().BeFalse();
            result.Error.ErrorCode.Should().Be(Errors.Product.ProductNotFound().ErrorCode);
            orderRepositoryMock
                .Verify(x => x.CreateOrder(It.IsAny<Order>()), Times.Never);
        }

        #endregion

        #region Checkout

        [Theory]
        [AutoMoqInlineData]
        public async Task CheckoutOrder_ValidRequest_CreateOrder(
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            Order order,
            int orderId,
            OrderService orderService)
        {
            order.Id = orderId;
            orderRepositoryMock
                .Setup(x => x.GetOrderById(orderId))
                .ReturnsAsync(order);

            var result = await orderService.CheckoutOrder(orderId);

            result.IsSuccessful.Should().BeTrue();
            result.Data.TotalAmount.Should().Be(order.TotalAmount);
            result.Data.DiscountAmount.Should().Be(order.DiscountAmount);
            result.Data.Id.Should().Be(order.Id);

            order.OrderStatus.Should().Be(Domain.Enums.OrderStatus.Completed);

            orderRepositoryMock
                .Verify(x => x.UpdateOrder(order), Times.Once);
        }

        [Theory]
        [AutoMoqInlineData(20, 13, 00, 00)]
        [AutoMoqInlineData(20, 14, 59, 59)]
        public async Task CheckoutOrder_ValidRequestHappyHour_CreateOrder(
            int happyHourDiscountPercentage,
            int hour,
            int minute,
            int second,
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            [Frozen] Mock<IDateTimeProvider> dateTimeProviderMock,
            OrderItem orderItem,
            DateTime createdOnUtc,
            int orderId,
            OrderService orderService)
        {
            var order = new Order(new List<OrderItem>() { orderItem }, new Restaurant("Restaurant 1", 0), createdOnUtc, null, Domain.Enums.OrderStatus.Pending)
            {
                Id = orderId
            };
            orderRepositoryMock
                .Setup(x => x.GetOrderById(orderId))
                .ReturnsAsync(order);

            var orderTotalAmount = order.TotalAmount;

            dateTimeProviderMock
                .Setup(x => x.UtcNow())
                .Returns(new DateTime(2022, 12, 12, hour, minute, second));

            var result = await orderService.CheckoutOrder(orderId);

            result.IsSuccessful.Should().BeTrue();
            result.Data.TotalAmount.Should().Be(order.TotalAmount);
            result.Data.DiscountAmount.Should().Be(order.DiscountAmount);
            result.Data.Id.Should().Be(order.Id);

            order.TotalAmount.Should().Be(orderTotalAmount - (orderTotalAmount * happyHourDiscountPercentage / 100));
            order.OrderStatus.Should().Be(Domain.Enums.OrderStatus.Completed);

            orderRepositoryMock
                .Verify(x => x.UpdateOrder(order), Times.Once);
        }

        [Theory]
        [AutoMoqInlineData]
        public async Task Handle_OrderAlreadyCompleted_ReturnsFailure(
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            Order order,
            DateTime now,
            int orderId,
            OrderService orderService)
        {
            order.Id = orderId;
            orderRepositoryMock
                .Setup(x => x.GetOrderById(orderId))
                .ReturnsAsync(order);

            order.CheckoutOrder(now);
            var result = await orderService.CheckoutOrder(orderId);

            result.IsSuccessful.Should().BeFalse();
            result.Error.ErrorCode.Should().Be(Errors.Order.OrderAlreadyCompleted().ErrorCode);

            orderRepositoryMock
                .Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
        }

        [Theory]
        [AutoMoqInlineData]
        public async Task Handle_OrderIsNull_ReturnsFailure(
            [Frozen] Mock<IOrderRepository> orderRepositoryMock,
            int orderId,
            OrderService orderService)
        {
            orderRepositoryMock
                .Setup(x => x.GetOrderById(orderId))
                .ReturnsAsync((Order?)null);

            var result = await orderService.CheckoutOrder(orderId);

            result.IsSuccessful.Should().BeFalse();
            result.Error.ErrorCode.Should().Be(Errors.Order.OrderNotFound().ErrorCode);
            orderRepositoryMock
                .Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
        }

        #endregion
    }
}
