using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using IcebearDemo.Models;
using IceBear;

namespace IcebearDemo
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        List<Customer> customers;
        List<Order> orders;
        List<OrderDetail> orderDetails;
        List<Product> products;

        private void MainForm_Load(object sender, EventArgs e)
        {
            List<ReportDemo> reportDemos = new List<ReportDemo>() {
                new ReportDemo(){Name="Customer list - simple", h=BasicCustomerList},
                new ReportDemo(){Name="Customer list - grouped per country", h=CustomerListPerCountry},
                new ReportDemo(){Name="Product list", h=ProductList},
                new ReportDemo(){Name="Orders", h=OrderNotes},
                new ReportDemo(){Name="Orders -subreport demo", h=OrderWithSubReport}
            };

            listBox1.DataSource = reportDemos;
            listBox1.DisplayMember = "Name";

            LoadData();
        }

        void LoadData()
        {
            SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("datasource=Northwind_small.sqlite");
            conn.Open();

            SQLiteCommand cmd = new SQLiteCommand("select * from Customer", conn);

            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);

            DataTable dataTable = new DataTable();
            da.Fill(dataTable);

            customers = new List<Customer>();
            foreach (DataRow row in dataTable.Rows)
            {
                customers.Add(
                    new Models.Customer()
                    {
                        Id = (string)row["Id"],
                        CompanyName = (string)row["CompanyName"],
                        ContactName = (string)row["ContactName"],
                        ContactTitle = (string)row["ContactTitle"],
                        Address = (string)row["Address"],
                        City = (string)row["City"],
                        Country = (string)row["Country"],
                        Fax = row.IsNull("Fax") ? "" : (string)row["Fax"],
                        Phone = (string)row["Phone"],
                        PostalCode = row.IsNull("PostalCode") ? "" : (string)row["PostalCode"],
                        Region = (string)row["Region"]

                    }
                );

            }

            dataTable = new DataTable();
            da.SelectCommand.CommandText = "select * from [order]";
            da.Fill(dataTable);
            orders = new List<Order>();
            foreach (DataRow row in dataTable.Rows)
            {
                orders.Add(new Order()
                {
                    OrderID = getField<long>(row, "ID"),
                    CustomerID = getField<string>(row, "CustomerID"),
                    EmployeeID = getField<long>(row, "EmployeeID"),
                    OrderDate = Convert.ToDateTime(getField<string>(row, "OrderDate")),
                    RequiredDate = Convert.ToDateTime(getField<string>(row, "RequiredDate")),
                    ShipAddress = getField<string>(row, "ShipAddress"),
                    ShipVia = getField<long>(row, "ShipVia"),
                    Freight = getField<decimal>(row, "Freight"),
                    ShipName = getField<string>(row, "ShipName"),
                    ShipCity = getField<string>(row, "ShipCity"),
                    ShipPostalCode = getField<string>(row, "ShipPostalCode"),
                    ShipRegion = getField<string>(row, "ShipRegion"),
                    ShipCountry = getField<string>(row, "ShipCountry"),
                    ShippedDate = Convert.ToDateTime(getField<string>(row, "ShippedDate")),
                    Comment=""
                });
            }

            orders[0].Comment = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt.";
            orders[1].Comment = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            orders[2].Comment = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation.";

            dataTable = new DataTable();
            da.SelectCommand.CommandText = "select * from orderDetail";
            da.Fill(dataTable);
            orderDetails = new List<OrderDetail>();
            foreach (DataRow row in dataTable.Rows)
            {
                orderDetails.Add(new OrderDetail() {
                    OrderID = getField<long>(row, "OrderID"),
                    ProductID = getField<long>(row, "ProductID"),
                    Discount = getField<double>(row, "Discount"),
                    Quantity = getField<long>(row, "Quantity"),
                    UnitPrice = getField<decimal>(row, "UnitPrice")
                });
            }

            dataTable = new DataTable();
            da.SelectCommand.CommandText = "select * from product";
            da.Fill(dataTable);
            products = new List<Product>();
            foreach (DataRow row in dataTable.Rows)
            {
                products.Add(new Product() {
                    ProductID = getField<long>(row, "Id"),
                    ProductName = getField<string>(row, "ProductName"),
                    SupplierID = getField<long>(row, "SupplierID"),
                    CategoryID = getField<long>(row, "CategoryID"),
                    //QuantityPerUnit= Convert.ToInt32(getField<string>(row, "QuantityPerUnit")),
                    UnitPrice = getField<decimal>(row, "UnitPrice"),
                    UnitsInStock = getField<long>(row, "UnitsInStock"),
                    UnitsOnOrder = getField<long>(row, "UnitsOnOrder"),
                    ReorderLevel = getField<long>(row, "ReorderLevel"),
                    Discontinued = Convert.ToBoolean(getField<long>(row, "Discontinued"))
                });
            }


            conn.Close();

            foreach (OrderDetail orderDetail in orderDetails)
            {
                orderDetail.Order = orders.FirstOrDefault(o => o.OrderID == orderDetail.OrderID);
                orderDetail.Product = products.FirstOrDefault(p => p.ProductID == orderDetail.ProductID);
            }
        }

        T getField<T>(DataRow row, string FieldName)
        {
            return row.IsNull(FieldName) ? default(T) : (T)row[FieldName];
        }

        class ReportDemo
        {
            public string Name { get; set; }
            public string ReportName { get; set; }
            public ExecuteReport h { get; set; }
        }

        delegate void ExecuteReport();
        Report report;

        private void button2_Click(object sender, EventArgs e)
        {
            ReportDemo reportDemo = (ReportDemo)listBox1.SelectedItem;

            reportDemo.h.Invoke();

            report.ShowGrid = checkBoxShowGrid.Checked;

            report.GeneratePDF($"c:\\temp\\Report {DateTime.Now:yyyyMMddhhmmss}.pdf");
        }

        void BasicCustomerList()
        {
            report = new Report();
            report.DataSource = customers.OrderBy(c => c.CompanyName);
            //report.SelectedOrientation = Report.Orientations.Landscape;
            //report.SelectedPageType = Report.PageSizes.A5;

            report.AddField("Id", 50, HeaderLabel:"ID");
            report.AddField("CompanyName", 150, HeaderLabel:"Name");
            report.AddField("Address", 200, HeaderLabel:"Address");
            report.AddField("City", 200, HeaderLabel:"City");

            report.ReportHeader = new ReportSection()
            {
                Height = 100,
                ReportObjects =
               {
                   new ReportObjectLabel("Report header",0, 1, Width:150)
               },
                ForceNewPage=ReportSection.ForceNewPageTypes.AfterSection
            };
            report.ReportFooter = new ReportSection()
            {
                Height = 100,
                ReportObjects =
               {
                   new ReportObjectLabel("Report footer",0, 1, Width:150)
               },
                ForceNewPage=ReportSection.ForceNewPageTypes.BeforeSection
            };

        }
        void CustomerListPerCountry()
        {
            report = NewReport("Customer list by country");
            report.DataSource = customers.OrderBy(c => c.Country);
           
            report.AddField("Id", 50, X:5, HeaderLabel: "ID");
            report.AddField("CompanyName", 100, HeaderLabel: "Name");
            report.AddField("Address", 100, HeaderLabel: "Address");
            report.AddField("City", 100, HeaderLabel: "City");

            report.ReportGroups.Add(new ReportGroup()
            {
                GroupingKey = "Country",
                HeaderSection = new ReportSection()
                {
                    Height = 18,
                    ReportObjects = {
                        new ReportObjectRectangle(){XLeft=0, XRight=report.PrintableAreaWidth, YTop=0, YBottom=14},
                        new ReportObjectField("Country", 3, 0, Width:100)
                    }
                },
                FooterSection = new ReportSection()
                {
                    Height = 25,
                    ReportObjects = {
                        new ReportObjectLabel("Customer count:",0, 1, Width:150),
                        new ReportObjectField("count(id)", 80, 1, Width:120)
                    }
                }
            });
        }

        void ProductList()
        {
            report = NewReport("ProductList");
            report.DataSource = products.OrderBy(p => p.ProductID);
            report.AddField("ProductID", 60, X: 15, HeaderLabel: "Product ID");
            report.AddField("ProductName", 150, HeaderLabel: "Description", CanGrow:true);
            report.AddField("UnitPrice", 60, HeaderLabel: "Unit price", Alignment: Alignment.Right, Mask: "0.00");
            report.AddField("UnitsInStock", 60, HeaderLabel: "Stock qty", Alignment: Alignment.Right);
            report.AddField("UnitsOnOrder", 60, HeaderLabel: "On order qty", Alignment: Alignment.Right, Mask: "#");
            report.AddField("ReorderLevel", 60, HeaderLabel: "Reorder lvl", Alignment: Alignment.Right, Mask: "#");

            report.DetailSection.ReportObjects.Add(new ReportObjectImage() { ImageFileName = @"C:\Users\Pascal\Pictures\discontinued.png", XLeft = 0, YTop = 0, YBottom = 11, ID = "discontinued" });

            report.DetailSection.IsVisible += DetailSection_IsVisible;

        }

        private bool DetailSection_IsVisible(string ReportObjectID, object Row)
        {
            return ReportObjectID != "discontinued" || ((Product)Row).Discontinued;
        }


        public void OrderNotes()
        {
            report = new Report();
            report.DataSource = orderDetails.Take(100);

            report.ReportGroups.Add(new ReportGroup() {
                GroupingKey = "OrderID",
                StartOnNewPage=true,
                HeaderSection = new ReportSection() {
                    ReportObjects = {
                        new ReportObjectImage(){ImageFileName=@"C:\Users\Pascal\Pictures\Icebear reporting company logo.png", XLeft=0, YTop=0, YBottom=25},
                        new ReportObjectLabel("Icebear report company", 0, 28, Width:150, Alignment:Alignment.Center ),
                        new ReportObjectLabel("52nd Street 54",0, 40, Width:150, Alignment:Alignment.Center ),
                        new ReportObjectLabel("New York",0, 52, Width:150, Alignment:Alignment.Center ),

                        new ReportObjectField("Order.ShipName", 300, 70, Width:250),
                        new ReportObjectField("Order.ShipAddress", 300, 80, Width:250 ),
                        new ReportObjectField("Order.ShipPostalCode", 300, 90, Width:50),
                        new ReportObjectField("Order.ShipCity", 350, 90, Width:200),
                        new ReportObjectField("Order.ShipCountry",300, 100, Width:250),

                        new ReportObjectRectangle(){XLeft=0, XRight=report.PrintableAreaWidth, YTop=130, YBottom=150},
                    },
                    Height = 200
                },
                FooterSection = new ReportSection()
                {
                    ReportObjects = new List<IReportObject>() {
                        new ReportObjectField("Order.Comment", 0, 0, Width:report.PrintableAreaWidth, Heigth:1){CanGrow=true },
                        new ReportObjectRectangle(){XLeft=0, XRight=report.PrintableAreaWidth, YTop=1, YBottom=80 },
                        new ReportObjectLabel("Total orderamount ex VAT", 300, 45, Width:150 ),
                        new ReportObjectField("sum(NetAmount)", 450, 45, Width:75, Mask: "0.00", Alignment:Alignment.Right ),
                        new ReportObjectLabel("Vat", 300, 55, Width:150 ),
                        new ReportObjectField(){FieldName="sum(Vat)", XLeft=450, XRight=525, YTop=55, YBottom=65, Mask= "0.00", Alignment=Alignment.Right },
                        new ReportObjectLabel("Total orderamount", 300, 65, Width:150 ),
                        new ReportObjectField(){FieldName="sum(TotalAmount)", XLeft=450, XRight=525, YTop=65, YBottom=75, Mask= "0.00", Alignment=Alignment.Right }

                    },
                    Height = 100
                }
            });

            report.SectionForAutoHeaderLabels = report.ReportGroups[0].HeaderSection; // we want the headerlabels of the detail fields in the groupheader not in the pageheader which is the default
            report.YTopForAutoAddedFieldsInHeader = 133;
            report.AddField("ProductID", 20, HeaderLabel:"ID");
            report.AddField("Product.ProductName", 150, HeaderLabel: "Description");
            report.AddField("UnitPrice", 75, Mask: "0.00", HeaderLabel: "Unit price", Alignment: Alignment.Right);
            report.AddField("Quantity", 75, HeaderLabel: "Quantity", Mask: "0", Alignment: Alignment.Right);
            report.AddField("Discount", 75, HeaderLabel: "Discount", Mask: "0.00", Alignment: Alignment.Right);
            report.AddField("NetAmount", 75, HeaderLabel: "Net amount", Mask: "0.00", Alignment: Alignment.Right);

        }


        public void OrderWithSubReport()
        {

            var selectedOrders = orders.Take(100);

            selectedOrders.ToList().ForEach(o => o.OrderDetails = orderDetails.Where(od => od.OrderID == o.OrderID).ToList());

            Report orderDetail = new Report();
            orderDetail.AddField("ProductID", 20, HeaderLabel: "ID");
            orderDetail.AddField("Product.ProductName", 150, HeaderLabel: "Description");
            orderDetail.AddField("UnitPrice", 75, Mask: "0.00", HeaderLabel: "Unit price", Alignment: Alignment.Right);
            orderDetail.AddField("Quantity", 75, HeaderLabel: "Quantity", Mask: "0", Alignment: Alignment.Right);
            orderDetail.AddField("Discount", 75, HeaderLabel: "Discount", Mask: "0.00", Alignment: Alignment.Right);
            orderDetail.AddField("NetAmount", 75, HeaderLabel: "Net amount", Mask: "0.00", Alignment: Alignment.Right);
            //orderDetail.PageHeader.Height = 0;
            //orderDetail.PageFooter.Height = 0;
            //orderDetail.PageHeader.ReportObjects.Add(new ReportObjectLine() { XRight = 100 });
            //orderDetail.AlternatingRowsPrimaryColor = Color.Yellow;
            //orderDetail.AlternatingRowsPrimaryColor = Color.Green;
            //orderDetail.DetailSection.Height = 15;
            //orderDetail.DetailSection.ReportObjects.Add(new ReportObjectLine() { XRight = 100, YTop=15, YBottom=15 });
            report = NewReport("Order - subreport demo");
            report.DataSource = selectedOrders;
            report.AddField("OrderID", 100);
            report.DetailSection.ReportObjects.Add(new ReportObjectSubReport() {SubReport=orderDetail, DataSource="OrderDetails", XLeft=20, XRight=500, YTop=15, YBottom=65 });


        }

        Report NewReport(string Title)
        {
            Report report = new Report();

            Style defaultStyle = report.DefaultStyle.Clone();
            defaultStyle.Font = new Font("Arial", 11, GraphicsUnit.Point);
            report.DefaultStyle = defaultStyle;

            Style titleStyle = defaultStyle.Clone();
            titleStyle.Font=new Font("Calibri", 20, FontStyle.Bold, GraphicsUnit.Point);
            Style headerStyle = defaultStyle.Clone();
            headerStyle.Font= new Font("Calibri", 11, FontStyle.Bold, GraphicsUnit.Point);

            report.PageHeader = new ReportSection()
            {
                Height = 48,
                ID = "PageHeader",
                DefaultStyle = headerStyle,
                ReportObjects = {
                    new ReportObjectLabel(Title, 300, 3, XRight:report.PrintableAreaWidth, Alignment:Alignment.Right){Style=titleStyle },
                    new ReportObjectRectangle(){XLeft=0, XRight=report.PrintableAreaWidth, YTop=26, YBottom=46 },
                    new ReportObjectImage(){ImageFileName=@"C:\Users\Pascal\Pictures\Icebear reporting company logo.png", XLeft=0, YTop=0, YBottom=25}
                }
            };
            report.YTopForAutoAddedFieldsInHeader = 29;
            report.PageFooter = new ReportSection()
            {
                Height = 40,
                ID = "PageFooter",
                ReportObjects = {
                    new ReportObjectLine(){XLeft=0, XRight=report.PrintableAreaWidth,YTop=0, YBottom=0},
                    new ReportObjectLabel("Icebear report company",0, 0, Width:200),
                    new ReportObjectLabel("52nd Street 54",180, 0, Width:180, Alignment:Alignment.Center),
                    new ReportObjectLabel("New York",400, 0, XRight:report.PrintableAreaWidth, Alignment: Alignment.Right ),
                    new ReportObjectLabel($"Printed {DateTime.Now: dd-MM-yyyy hh:mm}", 0,12,Width:150),
                    new ReportObjectLabel("Page {PageNumber} of {TotalPages}", 400, 12, XRight: report.PrintableAreaWidth, Alignment:Alignment.Right )
                }
            };

            report.AlternatingRowsPrimaryColor = Color.White;
            report.AlternatingRowsSecondaryColor = Color.LightGray;
            report.AlternateColorOfDetailLines = true;
            return report;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult res = printDialog1.ShowDialog();

            if (res != DialogResult.OK)
                return;

            ReportDemo reportDemo = (ReportDemo)listBox1.SelectedItem;

            reportDemo.h.Invoke();

            report.ShowGrid = checkBoxShowGrid.Checked;

            report.Print(printDialog1.PrinterSettings.PrinterName);

        }
    }
}
