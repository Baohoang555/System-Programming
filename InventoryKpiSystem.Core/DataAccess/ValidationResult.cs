namespace InventoryKpiSystem.Core.DataAccess
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string msg) => new() { IsValid = false, Message = msg };
    }
}