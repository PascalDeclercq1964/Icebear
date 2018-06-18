using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcebearDemo.Models
{
    public class OrderDetail
    {
        public long OrderID { get; set; }
        public long ProductID { get; set; }
        public decimal UnitPrice { get; set; }
        public long Quantity { get; set; }
        public double Discount { get; set; }

        public decimal NetAmount { get { return Math.Round(Convert.ToDecimal(Quantity) * UnitPrice * (1 - Convert.ToDecimal(Discount)), 2); } }
        public decimal Vat { get { return NetAmount * 0.21m; } }
        public decimal TotalAmount { get { return NetAmount + Vat; } }

        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
