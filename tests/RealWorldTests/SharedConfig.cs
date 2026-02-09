namespace ECommerce.Configuration
{
    /// <summary>
    /// Configuration settings shared across the e-commerce services.
    /// This file is referenced by other test files to create a realistic dependency structure.
    /// </summary>
    public class OrderConfiguration
    {
        public int MaxItemsPerOrder { get; set; } = 50;
        public decimal MinimumOrderAmount { get; set; } = 10.00m;
        public bool AutoCalculateTax { get; set; } = true;
        public decimal TaxRate { get; set; } = 0.08m;
        public bool EnablePremiumFeatures { get; set; } = false;
        public int CartExpirationDays { get; set; } = 7;
        public string CurrencyCode { get; set; } = "USD";
        public bool AllowGuestCheckout { get; set; } = true;
    }
}
