# Icebear
Icebear reports is a .net library to create reports and generate them as a PDF or send to the printer

Icebear reports are created in code, there is no designer. A first sight this sounds weird (no designer?) but take a look at this example report, with a minimum of code you get a basic report

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
- Objects on a report: text, image, line, rectangle, subreport and barcode

Give it a try. You'll be surprised how easy and fast it is to create a report in code. Even more complex ones such as an invoice.

FOr documentation check http://icebear.pascaldeclercq.be

IMPORTANT NOTE: Icebear reports is in a very early development faze. Please feel free to try and test it. We do not recommend to use it already in production.
All comments are appreciated.
