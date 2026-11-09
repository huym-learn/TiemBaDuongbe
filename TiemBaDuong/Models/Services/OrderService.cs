using System;
using System.Collections.Generic;
using System.Linq;

namespace TiemBaDuong.Models.Services
{
    /// <summary>
    /// Tạo đơn hàng trong một transaction. Giá và tổng tiền do server tính từ DB,
    /// không dùng UnitPrice/Total gửi từ client.
    /// </summary>
    public class OrderService
    {
        private readonly mystoreEntities db;

        public OrderService(mystoreEntities db)
        {
            this.db = db;
        }

        /// <exception cref="InvalidOperationException">Khi dữ liệu không còn hợp lệ.</exception>
        public int PlaceOrder(int customerId, IList<CartItem> selectedItems, string addressDelivery,
                              string deliveryMethod, string paymentMethod)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                throw new InvalidOperationException("Chưa chọn sản phẩm nào để đặt hàng.");
            }

            var productIds = selectedItems.Select(i => i.ProductID).Distinct().ToList();

            using (var tx = db.Database.BeginTransaction())
            {
                var products = db.Products
                    .Where(p => productIds.Contains(p.ProductID) && p.IsActive)
                    .ToList();

                if (products.Count != productIds.Count)
                {
                    throw new InvalidOperationException("Một số sản phẩm đã ngừng bán. Vui lòng kiểm tra lại giỏ hàng.");
                }

                var order = new Order
                {
                    CustomerID = customerId,
                    OrderDate = DateTime.Today,
                    PaymentStatus = "Pending",
                    OrderStatus = OrderStatuses.PendingPayment,
                    AddressDelivery = addressDelivery,
                    DeliveryMethod = deliveryMethod,
                    PaymentMethod = paymentMethod,
                    TotalAmount = 0m
                };

                foreach (var item in selectedItems)
                {
                    if (item.Quantity < 1 || item.Quantity > CartService.MaxQuantityPerItem)
                    {
                        throw new InvalidOperationException("Số lượng không hợp lệ cho sản phẩm " + item.ProductName + ".");
                    }

                    var product = products.First(p => p.ProductID == item.ProductID);
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = product.ProductPrice
                    });
                    order.TotalAmount += product.ProductPrice * item.Quantity;
                }

                order.TotalAmount += CheckoutOptions.ShippingFee;

                db.Orders.Add(order);
                db.SaveChanges();
                tx.Commit();
                return order.OrderID;
            }
        }
    }
}
