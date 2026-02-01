# Detailed Branch Changes for Conflict Testing

This file provides **exact code changes** to make in each branch to create the three conflict scenarios.

---

## Setup: Create Base Branch

```bash
git checkout -b test-base-conflicts
git add tests/RealWorldTests/
git commit -m "Add real world conflict test files"
git push origin test-base-conflicts
```

---

## Easy Conflict: OrderConfiguration.cs

### Branch A Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/add-retry-config
```

**In OrderConfiguration.cs**, add these properties after line 28 (after the `// ===== CONFLICT ZONE` comment):

```csharp
        /// <summary>
        /// Maximum number of retry attempts for failed operations.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay in seconds between retry attempts.
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;

```

**Also update the Validate() method** (around line 52) to add these checks before the `return true;`:

```csharp
            if (MaxRetryAttempts < 0)
                return false;

            if (RetryDelaySeconds < 0)
                return false;
```

```bash
git commit -am "Add retry configuration properties"
```

### Branch B Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/add-notification-config
```

**In OrderConfiguration.cs**, add these properties after line 28 (same location as Branch A):

```csharp
        /// <summary>
        /// Whether to send email notifications for order events.
        /// </summary>
        public bool EnableNotifications { get; set; } = true;

        /// <summary>
        /// Email address to send notifications to.
        /// </summary>
        public string NotificationEmail { get; set; } = "orders@example.com";

```

**Also update the Validate() method** to add this check before `return true;`:

```csharp
            if (EnableNotifications && string.IsNullOrWhiteSpace(NotificationEmail))
                return false;
```

```bash
git commit -am "Add notification configuration properties"
```

### Test the Conflict

```bash
git checkout test-base-conflicts
git merge feature/add-retry-config    # Should merge cleanly
git merge feature/add-notification-config  # Creates conflict!
```

**Expected Resolution:** Both sets of properties should be kept. The AI should recognize these are independent additions.

---

## Medium Conflict: OrderProcessor.cs

### Branch A Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/add-inventory-validation
```

**In OrderProcessor.cs**, add this method after the `ValidateOrder()` method (around line 97):

```csharp
        /// <summary>
        /// Validates that all items are in stock.
        /// </summary>
        private bool ValidateInventory(Order order)
        {
            _logger.LogInfo($"Validating inventory for order {order.Id}");

            foreach (var item in order.Items)
            {
                // Simulate inventory check
                if (item.Quantity > 100) // Assume max stock is 100
                {
                    _logger.LogError($"Product {item.ProductName} exceeds available inventory");
                    return false;
                }
            }

            _logger.LogInfo("Inventory validation passed");
            return true;
        }
```

**In the ProcessOrder() method**, add this call after the `ValidateOrder()` check (around line 43):

```csharp
            // Validate inventory availability
            if (!ValidateInventory(order))
            {
                return new OrderResult
                {
                    Success = false,
                    ErrorMessage = "Inventory validation failed - items not in stock"
                };
            }

```

```bash
git commit -am "Add inventory validation"
```

### Branch B Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/add-shipping-calculation
```

**In OrderProcessor.cs**, add this method after the `ValidateOrder()` method:

```csharp
        /// <summary>
        /// Calculates shipping cost based on order details.
        /// </summary>
        private decimal CalculateShippingCost(Order order)
        {
            _logger.LogInfo($"Calculating shipping cost for order {order.Id}");

            decimal baseCost = 5.99m;
            decimal weightCharge = order.Items.Sum(item => item.Quantity * 0.50m);
            decimal totalShipping = baseCost + weightCharge;

            _logger.LogInfo($"Shipping cost calculated: {totalShipping:C}");
            return totalShipping;
        }
```

**In the ProcessOrder() method**, add these lines after the `ValidateOrder()` check (same location as Branch A):

```csharp
            // Calculate shipping
            decimal shippingCost = CalculateShippingCost(order);
            _logger.LogInfo($"Shipping cost: {shippingCost:C}");

```

**Also modify the total calculation** (around line 50) to include shipping:

```csharp
            decimal total = subtotal + tax + shippingCost;
```

```bash
git commit -am "Add shipping cost calculation"
```

### Test the Conflict

```bash
git checkout test-base-conflicts
git merge feature/add-inventory-validation  # Should merge cleanly
git merge feature/add-shipping-calculation  # Creates conflict!
```

**Expected Resolution:** The AI should recognize that:
1. Both methods should be added
2. Both should be called in ProcessOrder()
3. They should be called in the right order: inventory validation first (before processing), then shipping calculation
4. Shipping cost needs to be included in the total

---

## Hard Conflict: PaymentService.cs

### Branch A Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/refactor-card-validation
```

**In PaymentService.cs**, replace the entire validation section in `ProcessPayment()` (lines 33-68) with:

```csharp
            // Validate payment method and card details
            try
            {
                ValidatePaymentMethod(request);
            }
            catch (PaymentValidationException ex)
            {
                _logger.LogError($"Payment validation failed: {ex.Message}");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
```

**Then add this new method** after the `ProcessPayment()` method (before `RefundPayment()`):

```csharp
        /// <summary>
        /// Validates payment method and card details.
        /// </summary>
        private void ValidatePaymentMethod(PaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                throw new PaymentValidationException("Payment method is required");

            if (request.Amount <= 0)
                throw new PaymentValidationException("Payment amount must be positive");

            if (request.PaymentMethod == "CreditCard")
            {
                ValidateCreditCard(request);
            }
        }

        /// <summary>
        /// Validates credit card details including Luhn algorithm check.
        /// </summary>
        private void ValidateCreditCard(PaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CardNumber))
                throw new PaymentValidationException("Card number is required");

            // Remove spaces and validate format
            string cardNumber = request.CardNumber.Replace(" ", "");
            if (!Regex.IsMatch(cardNumber, @"^\d{13,19}$"))
                throw new PaymentValidationException("Invalid card number format");

            // Luhn algorithm check
            if (!IsValidLuhn(cardNumber))
                throw new PaymentValidationException("Invalid card number");

            if (string.IsNullOrWhiteSpace(request.CardCvv))
                throw new PaymentValidationException("CVV is required");

            if (!Regex.IsMatch(request.CardCvv, @"^\d{3,4}$"))
                throw new PaymentValidationException("Invalid CVV format");

            // Validate expiry date
            if (string.IsNullOrWhiteSpace(request.CardExpiry))
                throw new PaymentValidationException("Card expiry date is required");

            if (!TryParseExpiry(request.CardExpiry, out DateTime expiryDate))
                throw new PaymentValidationException("Invalid expiry date format");

            if (expiryDate < DateTime.UtcNow)
                throw new PaymentValidationException("Card has expired");
        }

        /// <summary>
        /// Validates card number using Luhn algorithm.
        /// </summary>
        private bool IsValidLuhn(string cardNumber)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Parses expiry date in MM/YY or MM/YYYY format.
        /// </summary>
        private bool TryParseExpiry(string expiry, out DateTime expiryDate)
        {
            expiryDate = DateTime.MinValue;
            var parts = expiry.Split('/');

            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int month) || month < 1 || month > 12)
                return false;

            if (!int.TryParse(parts[1], out int year))
                return false;

            // Handle 2-digit year
            if (year < 100)
                year += 2000;

            expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return true;
        }
```

**Add this exception class** at the end of the file (before the last closing brace):

```csharp
    public class PaymentValidationException : Exception
    {
        public PaymentValidationException(string message) : base(message) { }
    }
```

```bash
git commit -am "Refactor payment validation with card checks"
```

### Branch B Changes

```bash
git checkout test-base-conflicts
git checkout -b feature/refactor-fraud-detection
```

**In PaymentService.cs**, replace the entire validation section in `ProcessPayment()` (lines 33-68) with:

```csharp
            // Validate transaction for fraud
            var validationResult = ValidateTransaction(request);
            if (!validationResult.IsValid)
            {
                _logger.LogError($"Transaction validation failed: {validationResult.ErrorMessage}");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }
```

**Then add this new method** after the `ProcessPayment()` method:

```csharp
        /// <summary>
        /// Validates transaction for fraud and risk factors.
        /// </summary>
        private ValidationResult ValidateTransaction(PaymentRequest request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return ValidationResult.Fail("Payment method is required");

            if (request.Amount <= 0)
                return ValidationResult.Fail("Payment amount must be positive");

            // Fraud detection: Amount limits
            if (request.Amount > 10000)
            {
                return ValidationResult.Fail("Transaction amount exceeds maximum limit. Please contact support.");
            }

            // Fraud detection: Velocity check (simplified - in reality would check database)
            if (request.Amount > 1000 && string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return ValidationResult.Fail("High-value transactions require customer verification");
            }

            // Credit card specific validation
            if (request.PaymentMethod == "CreditCard")
            {
                if (string.IsNullOrWhiteSpace(request.CardNumber))
                    return ValidationResult.Fail("Card number is required");

                if (string.IsNullOrWhiteSpace(request.CardCvv))
                    return ValidationResult.Fail("CVV is required");

                // Check for suspicious patterns
                if (IsSuspiciousCardPattern(request.CardNumber))
                    return ValidationResult.Fail("Transaction flagged for review. Please contact support.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Checks for suspicious card number patterns.
        /// </summary>
        private bool IsSuspiciousCardPattern(string cardNumber)
        {
            // Remove spaces
            cardNumber = cardNumber.Replace(" ", "");

            // Check for sequential numbers (e.g., 12345678)
            bool isSequential = true;
            for (int i = 1; i < cardNumber.Length && i < 8; i++)
            {
                if (cardNumber[i] != cardNumber[i - 1] + 1)
                {
                    isSequential = false;
                    break;
                }
            }

            if (isSequential)
                return true;

            // Check for repeating digits (e.g., 11111111)
            bool isRepeating = cardNumber.Length > 0 && cardNumber.All(c => c == cardNumber[0]);
            return isRepeating;
        }
```

**Add this class** at the end of the file (before the last closing brace):

```csharp
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        private ValidationResult() { }

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Fail(string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }
```

```bash
git commit -am "Refactor payment validation with fraud detection"
```

### Test the Conflict

```bash
git checkout test-base-conflicts
git merge feature/refactor-card-validation  # Should merge cleanly
git merge feature/refactor-fraud-detection  # Creates complex conflict!
```

**Expected Resolution:** The AI needs to understand that both refactorings have merit:
- **From Branch A:** Luhn algorithm validation, expiry date checking, detailed card format validation
- **From Branch B:** Fraud detection (amount limits, velocity checks), suspicious pattern detection, ValidationResult pattern

The AI should create a solution that:
1. Combines both validation approaches
2. Keeps the detailed credit card validation (Luhn, expiry) from Branch A
3. Keeps the fraud detection logic (limits, patterns) from Branch B  
4. Uses a consistent error handling pattern (could use ValidationResult pattern throughout, or convert to that pattern)
5. Maintains both helper methods (IsValidLuhn, TryParseExpiry, IsSuspiciousCardPattern)

This is the **hardest scenario** because it requires understanding the intent of two different architectural approaches and merging them cohesively.

---

## Testing Tips

1. **Always start from test-base-conflicts** when creating new feature branches
2. **Test scenarios independently** - you may want separate base branches for each scenario
3. **Use SourceTree's merge tool** configured to launch AutoMerge
4. **Document AI's resolution decisions** to evaluate quality
5. **Try resolving manually first** to compare against AI suggestions

---

## Evaluation Criteria

### Easy Conflict
- ✅ Both sets of properties kept
- ✅ No logical errors in merged code
- ✅ Validation methods updated for both new properties

### Medium Conflict
- ✅ Both methods added to class
- ✅ Both methods called in ProcessOrder()
- ✅ Called in correct order (inventory before shipping)
- ✅ Shipping cost included in total calculation

### Hard Conflict
- ✅ Credit card validation (Luhn, expiry) preserved
- ✅ Fraud detection logic preserved
- ✅ Consistent error handling pattern
- ✅ All helper methods included
- ✅ Code is well-structured and maintainable
