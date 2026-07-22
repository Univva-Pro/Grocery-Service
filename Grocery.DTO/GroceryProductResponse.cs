namespace Grocery.DTO
{
    // The restricted view for normal users (only allowed columns)
    public class GroceryProductResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
    }

    // The full view for Admins (all columns including stock and temperature)
    public class GroceryProductAdminResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public double Price { get; set; }
        public bool IsFresh { get; set; }
    }
    
    // Request DTO for creating/updating products
    public class GroceryProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public double Price { get; set; }
    }
}
