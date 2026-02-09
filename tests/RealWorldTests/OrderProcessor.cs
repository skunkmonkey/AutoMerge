using System;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Processing
{
    /// <summary>
    /// Handles order processing workflow.
    /// MEDIUM CONFLICT SCENARIO: Two developers add methods that need to be called in ProcessOrder().
    /// </summary>
    public class OrderProcessor
    {
        private readonly OrderConfiguration _config;
        private readonly ILogger _logger;

        public OrderProcessor(OrderConfiguration config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes an order through the complete workflow.
        /// </summary>
        /// <param name="order">The order to process.</param>
        /// <returns>The processing result.</returns>
        public OrderResult ProcessOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            _logger.LogInfo($"Starting to process order {order.Id}");

            // Validate basic order data
            if (!ValidateOrder(order))
            {
                return new OrderResult
                {
                    Success = false,
                    ErrorMessage = "Order validation failed"
                };
            }

            // ===== CONFLICT ZONE: Both branches will add method calls here =====
            // Branch A will add: ValidateInventory() method and call it here
            // Branch B will add: CalculateShippingCost() method and call it here
            // The AI should recognize both need to be called in the right order:
            // 1. ValidateInventory (before continuing processing)
            // 2. CalculateShippingCost (after validation, before payment)

            // Calculate totals
            decimal subtotal = order.Items.Sum(item => item.Price * item.Quantity);
            decimal tax = _config.AutoCalculateTax ? subtotal * _config.TaxRate : 0;
            decimal total = subtotal + tax;

            _logger.LogInfo($"Order {order.Id} totals: Subtotal={subtotal:C}, Tax={tax:C}, Total={total:C}");

            // Process payment
            bool paymentSuccess = ProcessPayment(order, total);
            if (!paymentSuccess)
            {
                return new OrderResult
                {
                    Success = false,
                    ErrorMessage = "Payment processing failed"
                };
            }

            // Update order status
            order.Status = OrderStatus.Completed;
            order.CompletedDate = DateTime.UtcNow;

            _logger.LogInfo($"Order {order.Id} completed successfully");

            return new OrderResult
            {
                Success = true,
                OrderId = order.Id,
                Total = total
            };
        }

        /// <summary>
        /// Validates basic order information.
        /// </summary>
        private bool ValidateOrder(Order order)
        {
            if (order.Items == null || !order.Items.Any())
            {
                _logger.LogError($"Order {order.Id} has no items");
                return false;
            }

            if (order.Items.Count > _config.MaxItemsPerOrder)
            {
                _logger.LogError($"Order {order.Id} exceeds maximum items ({_config.MaxItemsPerOrder})");
                return false;
            }

            decimal orderTotal = order.Items.Sum(item => item.Price * item.Quantity);
            if (orderTotal < _config.MinimumOrderAmount)
            {
                _logger.LogError($"Order {order.Id} below minimum amount ({_config.MinimumOrderAmount:C})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Processes payment for the order.
        /// </summary>
        private bool ProcessPayment(Order order, decimal amount)
        {
            _logger.LogInfo($"Processing payment of {amount:C} for order {order.Id}");

            // Simulate payment processing
            if (string.IsNullOrWhiteSpace(order.PaymentMethod))
            {
                _logger.LogError("Payment method not specified");
                return false;
            }

            // Payment processing logic would go here
            _logger.LogInfo($"Payment successful for order {order.Id}");
            return true;
        }
    }

    // Supporting types for the scenario
    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    public class OrderResult
    {
        public bool Success { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message);
    }
}
