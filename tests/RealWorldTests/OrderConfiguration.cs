using System;

namespace ECommerce.Configuration
{
    /// <summary>
    /// Configuration settings for order processing.
    /// EASY CONFLICT SCENARIO: Two developers will add different properties to the same region.
    /// </summary>
    public class OrderConfiguration
    {
        /// <summary>
        /// Maximum number of items allowed per order.
        /// </summary>
        public int MaxItemsPerOrder { get; set; } = 50;

        /// <summary>
        /// Minimum order amount in dollars.
        /// </summary>
        public decimal MinimumOrderAmount { get; set; } = 10.00m;

        /// <summary>
        /// Whether to apply tax automatically.
        /// </summary>
        public bool AutoCalculateTax { get; set; } = true;

        /// <summary>
        /// Default tax rate as a percentage.
        /// </summary>
        public decimal TaxRate { get; set; } = 0.08m;

        // ===== CONFLICT ZONE: Both branches will add properties here =====
        // Branch A will add: MaxRetryAttempts and RetryDelaySeconds
        // Branch B will add: EnableNotifications and NotificationEmail
        
        /// <summary>
        /// Whether to enable premium customer benefits.
        /// </summary>
        public bool EnablePremiumFeatures { get; set; } = false;

        /// <summary>
        /// Number of days before an abandoned cart expires.
        /// </summary>
        public int CartExpirationDays { get; set; } = 7;

        /// <summary>
        /// Default currency code (ISO 4217).
        /// </summary>
        public string CurrencyCode { get; set; } = "USD";

        /// <summary>
        /// Whether to allow guest checkout without account creation.
        /// </summary>
        public bool AllowGuestCheckout { get; set; } = true;

        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if configuration is valid.</returns>
        public bool Validate()
        {
            if (MaxItemsPerOrder <= 0)
                return false;

            if (MinimumOrderAmount < 0)
                return false;

            if (TaxRate < 0 || TaxRate > 1)
                return false;

            if (CartExpirationDays <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(CurrencyCode))
                return false;

            return true;
        }

        /// <summary>
        /// Gets a formatted display string for this configuration.
        /// </summary>
        public override string ToString()
        {
            return $"OrderConfiguration: Max Items={MaxItemsPerOrder}, " +
                   $"Min Amount={MinimumOrderAmount:C}, " +
                   $"Tax Rate={TaxRate:P}";
        }
    }
}
