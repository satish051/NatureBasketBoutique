using System.Security.Cryptography;
using System.Text;

namespace NatureBasketBoutique.Utility
{
    public static class SD
    {
        // --- ROLES ---
        public const string Role_Customer = "Customer";
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee"; // <--- This was missing!

        // --- ORDER STATUS ---
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusInProcess = "Processing";
        public const string StatusShipped = "Shipped";
        public const string StatusCancelled = "Cancelled";
        public const string StatusRefunded = "Refunded";

        // --- PAYMENT STATUS ---
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
        public const string PaymentStatusRejected = "Rejected";

        // --- ESEWA HELPER (Keep this if you added it in Step 7) ---
        public static string GenerateEsewaSignature(string totalAmount, string transactionUuid, string productCode)
        {
            var message = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={productCode}";
            var secretKey = "8gBm/:&EnhH.1/q";

            var encoding = new ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(message);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
    }
}