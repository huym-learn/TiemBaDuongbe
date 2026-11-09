using System.Collections.Generic;

namespace TiemBaDuong.Models.Services
{
    /// <summary>
    /// Cấu hình nghiệp vụ đặt hàng. Các giá trị marked PROVISIONAL chưa được chốt
    /// (DATABASE_GAPS / IMPLEMENTATION_NOTES Q4, Q5). Đổi tại đây khi có quyết định.
    /// </summary>
    public static class CheckoutOptions
    {
        // PROVISIONAL (Q4)
        public static readonly string[] DeliveryMethods = { "Giao tận nơi", "Nhận tại cửa hàng" };

        // PROVISIONAL (Q5). PayPal chưa bật (Phase 7).
        public static readonly string[] PaymentMethods = { "Thanh toán khi nhận hàng", "Chuyển khoản" };

        // Q12: chưa có bảng phí, mặc định 0.
        public const decimal ShippingFee = 0m;
    }

    /// <summary>Mã trạng thái đơn lưu trong DB (OrderStatus) và nhãn tiếng Việt hiển thị (PPT).</summary>
    public static class OrderStatuses
    {
        public const string PendingPayment = "PendingPayment";
        public const string Processing = "Processing";
        public const string Shipping = "Shipping";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        public static readonly IList<KeyValuePair<string, string>> Tabs = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>(PendingPayment, "Chờ thanh toán"),
            new KeyValuePair<string, string>(Processing, "Đang xử lý"),
            new KeyValuePair<string, string>(Shipping, "Đang vận chuyển"),
            new KeyValuePair<string, string>(Delivered, "Đã giao"),
            new KeyValuePair<string, string>(Cancelled, "Đã hủy")
        };

        public static string Label(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Chưa cập nhật";
            foreach (var tab in Tabs)
            {
                if (tab.Key == code) return tab.Value;
            }
            return code;
        }

        public static bool IsValid(string code)
        {
            foreach (var tab in Tabs)
            {
                if (tab.Key == code) return true;
            }
            return false;
        }
    }
}
