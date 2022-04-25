using System;

namespace PaymentIntegration.Models
{
    public class TransactionModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public int Amount { get; set; }
        public string TrfRef { get; set; }
        public string Email { get; set; }
        public bool status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

    }
}
