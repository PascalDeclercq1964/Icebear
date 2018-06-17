# Icebear
Icebear reporting is a .net library to create reports and generate them as a PDF or send to the printer

Icebear reports are created in code, there is no designer. A first sight his sounds weird (no designer?) but take look at this example report, with a minimum if code you get a basic report

    Report report = new Report();
    report.DataSource = customers.OrderBy(c => c.CompanyName);

    report.AddField("Id", 50, HeaderLabel:"ID");
    report.AddField("CompanyName", 100, HeaderLabel:"Name");
    report.AddField("Address", 100, HeaderLabel:"Address");
    report.AddField("City", 100, HeaderLabel:"City");

    report.GeneratePDF($"c:\\temp\\Report {DateTime.Now:yyyyMMddhhmmss}.pdf");
    
Features
- 7 available sections: reportheader, pageheader, groupHeader, detailsection, groupfooter, pagefooter and reportfooter
- Unlimited grouping with totals in the groupfooter section : Sum, Count, Min, Max, Average
