using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcebearDemo.Models
{
    public class Order
    {
        public long OrderID { get; set; }
        public string CustomerID { get; set; }
        public long EmployeeID { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public DateTime ShippedDate { get; set; }
        public long ShipVia { get; set; }
        public decimal Freight { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipCountry { get; set; }
        public string Comment { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }

    }
}
