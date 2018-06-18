using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IcebearDemo.Models
{
    public class Product
    {
        public long ProductID { get; set; }
        public string ProductName { get; set; }
        public long SupplierID { get; set; }
        public long CategoryID { get; set; }
        public Decimal QuantityPerUnit { get; set; }
        public decimal UnitPrice { get; set; }
        public long UnitsInStock { get; set; }
        public long UnitsOnOrder { get; set; }
        public long ReorderLevel { get; set; }
        public bool Discontinued { get; set; }
    }
}
