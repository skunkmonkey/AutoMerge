using System;
using System.Text.RegularExpressions;

namespace ECommerce.Payment
{
    /// <summary>
    /// Handles payment processing and validation.
    /// HARD CONFLICT SCENARIO: Two developers refactor the same validation logic differently.
    /// </summary>
    public class PaymentService
    {
        private readonly IPaymentGateway _gateway;
        private readonly ILogger _logger;

        public PaymentService(IPaymentGateway gateway, ILogger logger)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a payment transaction.
        /// </summary>
        /// <param name="request">The payment request details.</param>
        /// <returns>The payment result.</returns>
        public PaymentResult ProcessPayment(PaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInfo($"Processing payment of {request.Amount:C}");

            // ===== CONFLICT ZONE: Both branches will refactor this validation logic =====
            // Branch A will:
            //   - Extract into ValidatePaymentMethod(request)
            //   - Add credit card validation rules (Luhn algorithm, expiry check)
            //   - Use exceptions for validation failures
            //
            // Branch B will:
            //   - Extract into ValidateTransaction(request)  
            //   - Add fraud detection checks (amount limits, velocity checks)
            //   - Use ValidationResult pattern instead of exceptions
            //
            // The AI must understand both intents and merge into coherent solution.

            // Current monolithic validation (both branches will refactor this differently)
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                _logger.LogError("Payment method is required");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment method is required"
                };
            }

            if (request.Amount <= 0)
            {
                _logger.LogError("Payment amount must be positive");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment amount must be positive"
                };
            }

            if (request.PaymentMethod == "CreditCard")
            {
                if (string.IsNullOrWhiteSpace(request.CardNumber))
                {
                    _logger.LogError("Card number is required");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Card number is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.CardCvv))
                {
                    _logger.LogError("CVV is required");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "CVV is required"
                    };
                }
            }
            // ===== END CONFLICT ZONE =====

            // Process through payment gateway
            try
            {
                var gatewayRequest = new GatewayRequest
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    CardNumber = request.CardNumber,
                    CardCvv = request.CardCvv,
                    CardExpiry = request.CardExpiry
                };

                var gatewayResponse = _gateway.Charge(gatewayRequest);

                if (gatewayResponse.Success)
                {
                    _logger.LogInfo($"Payment processed successfully. Transaction ID: {gatewayResponse.TransactionId}");
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = gatewayResponse.TransactionId,
                        Amount = request.Amount
                    };
                }
                else
                {
                    _logger.LogError($"Gateway declined payment: {gatewayResponse.ErrorMessage}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = gatewayResponse.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Payment processing failed: {ex.Message}");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment processing failed. Please try again."
                };
            }
        }

        /// <summary>
        /// Refunds a previous payment.
        /// </summary>
        public PaymentResult RefundPayment(string transactionId, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentNullException(nameof(transactionId));

            if (amount <= 0)
                throw new ArgumentException("Refund amount must be positive", nameof(amount));

            _logger.LogInfo($"Processing refund of {amount:C} for transaction {transactionId}");

            try
            {
                var refundResponse = _gateway.Refund(transactionId, amount);

                if (refundResponse.Success)
                {
                    _logger.LogInfo($"Refund processed successfully");
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = refundResponse.TransactionId,
                        Amount = amount
                    };
                }
                else
                {
                    _logger.LogError($"Refund failed: {refundResponse.ErrorMessage}");
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = refundResponse.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Refund processing failed: {ex.Message}");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Refund processing failed. Please contact support."
                };
            }
        }
    }

    // Supporting types
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardCvv { get; set; } = string.Empty;
        public string CardExpiry { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IPaymentGateway
    {
        GatewayResponse Charge(GatewayRequest request);
        GatewayResponse Refund(string transactionId, decimal amount);
    }

    public class GatewayRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardCvv { get; set; } = string.Empty;
        public string CardExpiry { get; set; } = string.Empty;
    }

    public class GatewayResponse
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message);
    }
}
