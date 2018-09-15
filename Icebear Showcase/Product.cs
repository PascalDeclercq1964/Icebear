using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icebear_Showcase
{
    public partial class Products
    {
        public decimal StockValue { get { return (UnitsInStock ?? 0)  * (UnitPrice??0); } }
    }
}
