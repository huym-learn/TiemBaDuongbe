using System;
using System.Collections.Generic;
using System.Linq;

namespace TiemBaDuong.Models
{
    /// <summary>Kết quả một trang dữ liệu. Chỉ chứa đúng các item của trang đó.</summary>
    public class PagedResult<T>
    {
        public IList<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize); }
        }

        /// <summary>Phân trang trên IQueryable: đếm và Skip/Take ở database. Query phải có OrderBy trước.</summary>
        public static PagedResult<T> FromQuery(IQueryable<T> query, int page, int pageSize)
        {
            if (page < 1) page = 1;
            int total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<T>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        /// <summary>Phân trang trên danh sách đã nhỏ (ví dụ New Products tối đa 20 item).</summary>
        public static PagedResult<T> FromList(IList<T> all, int page, int pageSize)
        {
            if (page < 1) page = 1;
            return new PagedResult<T>
            {
                Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = all.Count
            };
        }
    }
}
