﻿using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
//using System.Linq;




namespace IceBear
{
    /*
     Todo's

        - [improvement] reportFooter : allow to suppress pageheader
        - [improvement] default detail height is not oke
        - [feature] implement Barcodes
        - [feature] implement output to stream
        - [feature] trap & log errors
        - [feature] implement collection fields such as DataTable rows
        - [feature] group header: repeat on each page
        - [feature] conditional formatting
        - [feature] print in columns
        - [feature] Crosstab

        Done:
        - implement Page Size : apply it in the renderer
        - implement page orientation : apply it in the renderer
        - report variable (pagenumber, totalpages)
        - Can Grow 
            - done [bug] precalculate length of section for alternating color and prevent page overflow
            - done [bug] still problems : see demo product list
        - Subreport:
            - done [feature] keep together
            - Done [bug] respect page break
        - [feature] pagefooter: force to bottom of the page
		- bug: groupkey: also use cascading get property method
        - [feature] suppress repeating values
        - [feature] ReportHeader
        - [feature] ReportFooter with force to new page
		- [bug] check if a section is defined (not null) before using it (ex footsection)
        - [improvement] suppress repeating values use of compound key instead of single values
     */
    public class Report
    {
        public Report(PageSizes PageType=PageSizes.A4, Orientations Orientation=Orientations.Portrait)
        {
            ReportGroups = new List<ReportGroup>();
            DetailSection = new ReportSection() { ID = "DetailSection", Height = DefaultDetailHeight };
            LeftMargin = 30;    //total width A4 = 594
            RightMargin = 20;
            TopMargin = 40;
            BottomMargin = 20;
            this.SelectedPageType = PageType;
            this.SelectedOrientation = Orientation;
            DefaultStyle = Style.Default;
            AlternatingRowsPrimaryColor = Color.White;
            AlternatingRowsSecondaryColor = Color.GhostWhite;
            ForcePageFooterToPageBottom = true;
        }

        PageSizes _pageType;
        public PageSizes SelectedPageType
        {
            get { return _pageType; }
            set
            {
                _pageType = value;
                GetPageSize();
            }
        }


        Orientations _orientation;
        public Orientations SelectedOrientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                GetPageSize();
            }
        }

        public enum PageSizes { A0, A1, A2, A3, A4, A5, A6, Letter }
        public enum Orientations { Portrait, Landscape }

        public ReportSection PageHeader { get; set; }
        public ReportSection PageFooter { get; set; }
        public ReportSection ReportHeader { get; set; }
        public ReportSection ReportFooter { get; set; }
        public ReportSection DetailSection { get; set; }

        public ReportSection SectionForAutoHeaderLabels { get; set; }   // When AddField is used and a headerlabel is provided, then this label will be put in this section (Default= PageHeader)

        public IEnumerable<object> DataSource { get; set; }
        public List<ReportGroup> ReportGroups { get; set; }

        public Style DefaultStyle { get; set; }

        public double LeftMargin { get; set; }
        public double RightMargin { get; set; }
        public double TopMargin { get; set; }
        public double BottomMargin { get; set; }
        double PageLength { get; set; }
        double PageWidth { get; set; }

        public bool ShowGrid { get; set; }

        public Color AlternatingRowsPrimaryColor {get; set;}
        public Color AlternatingRowsSecondaryColor { get; set; }
        public bool AlternateColorOfDetailLines { get; set; }

        public double PrintableAreaWidth { get { return PageWidth - LeftMargin - RightMargin; } }
        public double PrintableAreaLength { get { return PageLength - TopMargin - BottomMargin; } }

        public double YTopForAutoAddedFieldsInHeader { get; set; }

        public bool ForcePageFooterToPageBottom { get; set; }

        double DefaultHeaderHeight = 30;
        double DefaultDetailHeight = Style.Default.Font.SizeInPoints*fontSizeToHeight;

        bool applyAlternateColor = false;
        double xOffset;
        double yOffset;
        double xNextPosition;

        int pagenumber;

        internal GenericReport genericReport;

        const double fontSizeToHeight = 1.25; //1.25f;

        public void GeneratePDF(string FileName, bool OpenWhenDone = true)
        {
            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    return;
            //}

            generateGenericReport();

            RenderToPDF engine = new RenderToPDF(SelectedPageType, SelectedOrientation);

            renderTheReport(engine);

            engine.Save(FileName);

            if (OpenWhenDone)
                Process.Start(FileName);

        }

        public void Print(string PrinterName)
        {
            generateGenericReport();

            PrintDocument printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = PrinterName;
            //printDocument.DefaultPageSettings.PrinterResolution.X = 72;
            //printDocument.DefaultPageSettings.PrinterResolution.Y = 72;
            //printDocument.DefaultPageSettings.PrinterResolution.Kind = PrinterResolutionKind.High;
            //printDocument.DefaultPageSettings.PaperSize = printDocument.DefaultPageSettings.PaperSize = new PaperSize(SelectedPageType.ToString(), Convert.ToInt32(PageWidth), Convert.ToInt32(PageLength));

            if (SelectedOrientation==Orientations.Landscape)    
                printDocument.DefaultPageSettings.Landscape = true;

            printDocument.PrintPage += PrintDocument_PrintPage;
            pageToPrint = 0;
            printDocument.Print();
        }

        public MemoryStream StreamPDF()
        {
            MemoryStream memoryStream = new MemoryStream();
            this.generateGenericReport();
            RenderToPDF renderToPdf = new RenderToPDF(this.SelectedPageType, this.SelectedOrientation);
            this.renderTheReport((IRenderEngine)renderToPdf);
            return renderToPdf.GetStream();
        }


        int pageToPrint;
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            reportPage page = genericReport.reportPages[pageToPrint];

            foreach (reportItem item in page.reportItems)
            {
                switch (item.reportObjectType)
                {
                    case reportObjectTypes.Rectangle:
                        Pen penUsed = (Pen)item.pen.Clone();
                        //penUsed.Brush = item.brush;
                        if (item.brush != null)
                            e.Graphics.FillRectangle(item.brush, item.XPrint, item.YPrint, item.WPrint, item.HPrint);
                        e.Graphics.DrawRectangle(penUsed, item.XPrint, item.YPrint, item.WPrint, item.HPrint);
                        break;
                    case reportObjectTypes.String:
                        StringFormat sf = new StringFormat();
                        if (item.alignment == Alignment.Left || item.alignment == Alignment.Justify)
                            sf.Alignment = StringAlignment.Near;
                        else if (item.alignment == Alignment.Center)
                            sf.Alignment = StringAlignment.Center;
                        else
                            sf.Alignment = StringAlignment.Far;
                        e.Graphics.DrawString(item.text, item.style.Font, item.style.Brush, new RectangleF(item.XPrint, item.YPrint, item.WPrint, item.HPrint), sf);
                        break;
                    case reportObjectTypes.Line:
                        e.Graphics.DrawLine(item.pen, item.XPrint, item.YPrint, item.XPrint+item.WPrint, item.YPrint+item.HPrint);
                        break;
                    case reportObjectTypes.Image:
                        e.Graphics.DrawImage(item.image, item.XPrint, item.YPrint, item.WPrint, item.HPrint);
                        break;
                    default:
                        break;
                }
            }

            pageToPrint++;

            if (pageToPrint < genericReport.reportPages.Count)
                e.HasMorePages = true;
        }

        private void renderTheReport(IRenderEngine engine)
        {
            foreach (reportPage page in genericReport.reportPages)
            {
                engine.NewPage();

                foreach (var reportItem in page.reportItems)
                {
                    switch (reportItem.reportObjectType)
                    {
                        case reportObjectTypes.Rectangle:
                            engine.DrawRectangle(reportItem.pen, reportItem.brush, reportItem.X, reportItem.Y, reportItem.W, reportItem.H);
                            break;
                        case reportObjectTypes.String:
                            engine.DrawString(reportItem.text, reportItem.style, reportItem.X, reportItem.Y, reportItem.W, reportItem.H, reportItem.alignment);
                            break;
                        case reportObjectTypes.Line:
                            engine.DrawLine(reportItem.pen, reportItem.X, reportItem.Y, reportItem.W, reportItem.H);
                            break;
                        case reportObjectTypes.Image:
                            engine.DrawImage(reportItem.image, reportItem.X, reportItem.Y, reportItem.W, reportItem.H);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        internal void generateGenericReport()
        { 
            
            genericReport = new GenericReport();

            yOffset = TopMargin;
            xOffset = LeftMargin;

            object previousRow = null;
            bool pageBreak = true;
            pagenumber = 0;
            List<reportItem> renderedItems;

            foreach (object row in DataSource)
            {

                if (pageBreak || IsPageBreakNeeded())
                {
                    NewPage(previousRow, row);
                    pageBreak = false;
                }

                //GroupFooters
                Footers(previousRow, row);


                //GroupHeaders
                foreach (ReportGroup reportGroup in ReportGroups)
                {
                    if (reportGroup.GroupingHasChanged(row))
                    {
                        double growthForThisSection;
                        reportGroup.HeaderSection.Render2(row, xOffset, DefaultStyle, out renderedItems, out growthForThisSection);

                        if (previousRow!=null && (reportGroup.StartOnNewPage && yOffset>TopMargin ) || IsPageBreakNeeded(reportGroup.HeaderSection.Height+growthForThisSection))
                            NewPage(previousRow, row);

                        genericReport.AddReportItems(renderedItems, yOffset);

                        yOffset += reportGroup.HeaderSection.Height+growthForThisSection;
						if (reportGroup.FooterSection!=null)
                        	reportGroup.FooterSection.aggregatedValues = null;

                    }
                }

                double growthForDetailSection;
                DetailSection.Render2(row, xOffset, DefaultStyle, out renderedItems, out growthForDetailSection);

                if (IsPageBreakNeeded(DetailSection.Height + growthForDetailSection))
                {
                    NewPage(previousRow, row);
                    DetailSection.Render2(row, xOffset, DefaultStyle, out renderedItems, out growthForDetailSection);   //rerender because suppressed repeating values need should not be suppressed at the beginning of a page
                }

                //Alternating detail color
                if (AlternateColorOfDetailLines)
                {
                    Brush brush = applyAlternateColor ? new SolidBrush(AlternatingRowsPrimaryColor) : new SolidBrush( AlternatingRowsSecondaryColor);
                    Pen pen = new Pen(Color.White, 0);
                    genericReport.CurrentPage.reportItems.Add(new reportItem() {reportObjectType= reportObjectTypes.Rectangle, brush=brush, X=LeftMargin, Y=yOffset, W=PrintableAreaWidth, H=DetailSection.Height+growthForDetailSection, pen=pen });
                    applyAlternateColor = !applyAlternateColor;
                }


                genericReport.AddReportItems(renderedItems, yOffset);

                yOffset += DetailSection.Height+growthForDetailSection;

                if (PageFooter != null)
                    PageFooter.calculateAggregates(row);
                if (ReportFooter != null)
                    ReportFooter.calculateAggregates(row);

                foreach (ReportGroup reportGroup in ReportGroups)
                {
                    if (reportGroup.FooterSection!=null)
                        reportGroup.FooterSection.calculateAggregates(row);
                    reportGroup.previousRow = row;
                }
                previousRow = row;

            }

            if (previousRow != null)
            {
                Footers(previousRow, null);
                if (ReportFooter != null)
                {
                    if (ReportFooter.ForceNewPage == ReportSection.ForceNewPageTypes.BeforeSection)
                    {
                        NewPage(previousRow, previousRow);
                    }
                    double growthReportFooter;
                    ReportFooter.Render2(previousRow, xOffset, DefaultStyle, out renderedItems, out growthReportFooter);

                    double y = PageLength - (ReportHeader != null ? ReportHeader.Height : 0) - growthReportFooter;
                    genericReport.AddReportItems(renderedItems, yOffset);

                    yOffset += ReportFooter.Height + growthReportFooter;
                }

                if (PageFooter != null)
                {
                    double pageFooterGrowth;
                    PageFooter.Render2(previousRow, xOffset, DefaultStyle, out renderedItems, out pageFooterGrowth);

                    double y = PageLength - (PageFooter.Height+pageFooterGrowth) - (ReportHeader != null ? ReportHeader.Height : 0)- BottomMargin;
                    if (y < yOffset)
                    {
                        genericReport.NewPage();
                        yOffset = TopMargin;
                        pagenumber++;
                    }

                    if (ForcePageFooterToPageBottom)
                        yOffset = PageLength - PageFooter.Height - BottomMargin - pageFooterGrowth;

                    genericReport.AddReportItems(renderedItems, yOffset);
                    yOffset += PageFooter.Height+pageFooterGrowth;
                }

            }


            int currentPage = 0;
            foreach (reportPage p in genericReport.reportPages)
            {
                currentPage++;
                foreach (reportItem item in p.reportItems)
                {
                    if (item.reportObjectType==reportObjectTypes.String)
                    {
                        item.text = item.text.Replace("{PageNumber}", currentPage.ToString()).Replace("{TotalPages}", genericReport.reportPages.Count.ToString());
                    }
                }
            }
        }

        private bool IsPageBreakNeeded(double HeigthOfNextSection=0)
        {
            return yOffset + HeigthOfNextSection > (PageLength - BottomMargin - (PageFooter != null ? PageFooter.Height : 0));

        }

        private void NewPage(object previousRow, object row)
        {
            List<reportItem> renderedItems;
            double growth;

            if (previousRow != null && PageFooter != null)
            {

                PageFooter.Render2(previousRow, xOffset, DefaultStyle, out renderedItems, out growth);

                if (ForcePageFooterToPageBottom)
                    yOffset = PageLength - PageFooter.Height - BottomMargin - growth;

                genericReport.AddReportItems(renderedItems, yOffset);
            }

            genericReport.NewPage();
            yOffset = TopMargin;
            pagenumber++;

            if (previousRow == null && ReportHeader != null)
            {
                ReportHeader.Render2(row, xOffset, DefaultStyle, out renderedItems, out growth);
                genericReport.AddReportItems(renderedItems, yOffset);
                if (ReportHeader.ForceNewPage == ReportSection.ForceNewPageTypes.AfterSection)
                {
                    genericReport.NewPage();
                    yOffset = TopMargin;
                    pagenumber++;
                }
                else
                   yOffset += ReportHeader.Height+growth;

            }

            if (PageHeader != null)
            {
                PageHeader.Render2(row, xOffset, DefaultStyle, out renderedItems, out growth);
                genericReport.AddReportItems(renderedItems, yOffset);
                yOffset += PageHeader.Height+growth;
            }

            if (ShowGrid)
            {
                Pen gridPen = new Pen(Color.LightGray, 0.5f);

                //vertical lines
                for (double i = LeftMargin; i < PrintableAreaWidth; i += 25)
                {
                    genericReport.CurrentPage.reportItems.Add(new reportItem() { reportObjectType = reportObjectTypes.Line, X = i, Y = TopMargin, W = 0, H = PrintableAreaLength, pen = gridPen });
                }

                //horizontal lines
                for (double i = TopMargin; i < PrintableAreaLength; i += 25)
                {
                    genericReport.CurrentPage.reportItems.Add(new reportItem() { reportObjectType = reportObjectTypes.Line, X = LeftMargin, Y = i, W = PrintableAreaWidth, H = 0, pen = gridPen });
                }
            }

            foreach (var item in this.DetailSection.ReportObjects)
            {
                if (item is ReportObjectField)
                {
                    ((ReportObjectField)item).previousValue = null;
                }
            }
        }

        void Footers(object previousRow, object row)
        {

            var groups = ReportGroups;
            for (int i = ReportGroups.Count - 1; i >= 0; i--)
            {
                var reportGroup = ReportGroups[i];
                if (reportGroup.previousRow != null && (row == null || reportGroup.GroupingHasChanged(row)))
                {
                    Debug.WriteLine(yOffset);

                    if (reportGroup.FooterSection != null)
                    {
                        if (IsPageBreakNeeded(reportGroup.FooterSection.Height))
                            NewPage(previousRow, row);

                        double growth;
                        List<reportItem> renderedItems;
                        reportGroup.FooterSection.Render2(reportGroup.previousRow, xOffset, DefaultStyle, out renderedItems, out growth);
                        genericReport.AddReportItems(renderedItems, yOffset);

                        yOffset += reportGroup.FooterSection.Height + growth;
                    }
                }
            }
        }

        ///// <summary>
        ///// Adds a field to the detail section and corresponding label in the header
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="HeaderLabel"></param>
        ///// <param name="YTopHeader"></param>
        ///// <param name="YBottomHeader"></param>
        //public void AddField(ReportObjectField field, string HeaderLabel, double YTopHeader, double YBottomHeader)
        //{
        //    this.PageHeader.ReportObjects.Add(new ReportObjectLabel(HeaderLabel, field.XLeft, field.XRight, YTopHeader, YBottomHeader));
        //    this.DetailSection.ReportObjects.Add(field);
        //}

        public ReportObjectField AddField(string FieldName, double Width, double? X=null, double? Y=null, double? Height=null, string HeaderLabel = null, Alignment Alignment = Alignment.Left, string Mask = null, string ID = null, bool CanGrow=true, bool SuppressRepeatingValues=false)
        {
            if (Y.HasValue)
                xNextPosition = 0;

            X = X ?? xNextPosition;
            Height = Height ?? DefaultStyle.Font.Size*fontSizeToHeight;

            if (HeaderLabel != null)
            {
                if (this.SectionForAutoHeaderLabels == null)
                {
                    if (this.PageHeader==null)
                        this.PageHeader = new ReportSection() { Height = DefaultHeaderHeight };

                    this.SectionForAutoHeaderLabels = this.PageHeader;  //not sure if this is the best solution. The problem is that when a report is instantiated, the pageheader does not exist yet so the SectionForAutoHeaderLabels cannot be set to it yet as a default
                }

                this.SectionForAutoHeaderLabels.ReportObjects.Add(new ReportObjectLabel(HeaderLabel, X.Value, YTopForAutoAddedFieldsInHeader, Width: Width, Heigth:Height, Alignment: Alignment));
            }


            ReportObjectField reportObjectField = new ReportObjectField() { FieldName = FieldName, XLeft = X.Value, XRight = X.Value + Width, YTop = 0, YBottom = Height.Value, Alignment = Alignment, Mask = Mask, ID = ID, CanGrow = CanGrow, SuppressRepeatingValues = SuppressRepeatingValues };
            this.DetailSection.ReportObjects.Add(reportObjectField);

            xNextPosition += Width + 3;

            return reportObjectField;
        }

        private void GetPageSize()
        {

            switch (SelectedPageType)
            {
                case PageSizes.A0:
                    PageWidth = 2384;
                    PageLength = 3370;
                    break;
                case PageSizes.A1:
                    PageWidth = 1684;
                    PageLength = 2384;
                    break;
                case PageSizes.A2:
                    PageWidth = 1191;
                    PageLength = 1684;
                    break;
                case PageSizes.A3:
                    PageWidth = 842;
                    PageLength = 1191;
                    break;
                case PageSizes.A4:
                    PageWidth = 595;
                    PageLength = 842;
                    break;
                case PageSizes.A5:
                    PageWidth = 420;
                    PageLength = 595;
                    break;
                case PageSizes.A6:
                    PageWidth = 298;
                    PageLength = 420;
                    break;
                case PageSizes.Letter:
                    PageWidth = 791;
                    PageLength = 612;
                    break;
                default:    //default to A4
                    PageWidth = 595;
                    PageLength = 842;
                    break;
            }

            if (SelectedOrientation==Orientations.Landscape)
            {
                double swap = PageWidth;
                PageWidth = PageLength;
                PageLength = swap;
            }
        }



    }

    #region Interfaces
    public interface IRenderEngine
    {
        void NewPage();

        //void Save(string FileName);

        void DrawRectangle(Pen pen, Brush brush, double X, double Y, double W, double H);
        void DrawString(string valueToRender, Style style, double X, double Y, double W, double H, Alignment Alignment);
        void DrawLine(Pen penUsed, double X, double Y, double W, double H);
        void DrawImage(Image image, double X, double Y, double W, double H);
    }

    public interface IReportObject
    {
        double XLeft { get; set; }
        double XRight { get; set; }
        double YBottom { get; set; }
        double YTop { get; set; }
        string ID { get; set; }

        void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null);
    }

    #endregion

    #region class definitions
    public class RenderToPDF : IRenderEngine
    {
        public RenderToPDF(Report.PageSizes PageSize, Report.Orientations Orientation)
        {
            doc = new PdfDocument();
            gfx = null;

            orientation = (Orientation == Report.Orientations.Portrait) ? PdfSharp.PageOrientation.Portrait : PdfSharp.PageOrientation.Landscape;
            pageSize = (PdfSharp.PageSize)pageSizeMapping[(int)PageSize];
        }

        PdfDocument doc;
        XGraphics gfx;
        PdfSharp.PageOrientation orientation;
        PdfSharp.PageSize pageSize;

        int[] pageSizeMapping={ 1,2,3,4,5, 6, 6, 104}; // A6 maps to A5, pdfsharp does not seem to have A6

        public void NewPage()
        {
            PdfPage page = new PdfPage();
            page.Orientation = orientation;
            page.Size = pageSize;
            var currentPage = doc.AddPage(page);
            gfx = XGraphics.FromPdfPage(currentPage);
        }


        public void DrawRectangle(Pen pen, Brush brush, double X, double Y, double W, double H)
        {

            XPen penUsed = pen!=null ? new XPen(XColor.FromName(pen.Color.Name), pen.Width) : null;
            XBrush brushUsed = (XBrush)brush;
            gfx.DrawRectangle(penUsed, brushUsed, X, Y, W, H);

        }

        public void DrawString(string valueToRender, Style style, double X, double Y, double W, double H, Alignment Alignment)
        {

            XTextFormatter tf = new XTextFormatter(gfx);
            tf.Alignment = (XParagraphAlignment)Alignment;

            Font font =  new Font(style.Font.FontFamily, style.Font.SizeInPoints, style.Font.Style, GraphicsUnit.World);
            tf.DrawString(valueToRender, (XFont)font, (XBrush)style.Brush, new XRect(X, Y, W, H));

        }
        public void DrawLine(Pen pen, double X, double Y, double W, double H)
        {
            gfx.DrawLine((XPen)pen, X, Y, X+W, Y+H);
        }

        public void DrawImage(Image image, double X, double Y, double W, double H)
        {
            gfx.DrawImage(image, X, Y, W, H);
        }

        //public void DrawBarCode(Image image, double X, double Y, double W, double H)
        //{
        //    XPoint point = new XPoint(X, Y);
        //    PdfSharp.Drawing.BarCodes.BarCode barcode=;
        //    gfx.DrawBarCode(barcode, point);
        //}

        public void Save(string FileName)
        {
            doc.Save(FileName);
        }

        public MemoryStream GetStream()
        {
            MemoryStream memoryStream = new MemoryStream();
            this.doc.Save((Stream)memoryStream, false);
            return memoryStream;
        }

    }

    public class ReportSection
    {
        public ReportSection()
        {
            ReportObjects = new List<IReportObject>();
            xPosition = 0;
        }

        public enum ForceNewPageTypes { None, BeforeSection, AfterSection }

        public List<IReportObject> ReportObjects { get; set; }

        public double Height { get; set; }
        public Style DefaultStyle { get; set; }
        public string ID { get; set; }

        public ForceNewPageTypes ForceNewPage {get; set;}


        public event ConditionDelegate IsVisible;
        public event StyleDelegate Styler;

        internal Dictionary<string, double> aggregatedValues;
        internal List<Tuple<string, AggregateTypes>> Aggregates { get; set; }
        internal double xPosition;


        internal void Render2(object row, double XOffset, Style defaultStyle, out List<reportItem> RenderedItems, out double Growth)
        {
            RenderedItems = new List<reportItem>();
            Growth = 0;
            double yOffset = 0;

            if (!string.IsNullOrEmpty(ID) && IsVisible != null)
                if (!IsVisible(ID, row))
                    return;

            Style styleUsed = DefaultStyle;
            if (!string.IsNullOrEmpty(ID) && Styler != null)
                styleUsed = Styler(ID, row);

            double previousYTop = -1;
            double growthForThisLine = 0;

            foreach (IReportObject reportObject in ReportObjects)
            {
                if (!string.IsNullOrEmpty(reportObject.ID) && IsVisible != null)
                    if (!IsVisible(reportObject.ID, row))
                        continue;


                if (!string.IsNullOrEmpty(reportObject.ID) && Styler != null)
                    styleUsed = Styler(reportObject.ID, row);


                if (reportObject.YTop != previousYTop)
                {
                    Growth += growthForThisLine;
                    yOffset += growthForThisLine;
                    growthForThisLine = 0;
                }

                double growthForThisObject;
                List<reportItem> renderedItems = null;
                reportObject.Render2(row,  XOffset, yOffset, styleUsed ?? defaultStyle, out renderedItems, out growthForThisObject, aggregatedValues);
                RenderedItems.AddRange(renderedItems);

                growthForThisLine = Math.Max(growthForThisLine, growthForThisObject);
                previousYTop = reportObject.YTop;
            }

            Growth += growthForThisLine;

            return;
        }


        internal void calculateAggregates(object row)
        {
            if (aggregatedValues == null)
                aggregatedValues = new Dictionary<string, double>();

            foreach (IReportObject reportObject in ReportObjects)
            {
                ReportObjectField reportObjectField = reportObject as ReportObjectField;
                if (reportObjectField == null)
                    continue;

                string fieldName = reportObjectField.FieldName;
                AggregateTypes aggregateType = GetAggregateType(fieldName);
                if (aggregateType == AggregateTypes.None)
                    continue;

                string bareFieldName = GetBareFieldName(fieldName, aggregateType);

                if (aggregateType == AggregateTypes.Count)
                {
                    if (!aggregatedValues.ContainsKey(fieldName))
                        aggregatedValues.Add(fieldName, 1);
                    else
                        aggregatedValues[fieldName] = aggregatedValues[fieldName] + 1;
                }
                else
                {
                    object valueObject = row.GetType().GetProperty(bareFieldName).GetValue(row);
                    if (valueObject != null)
                    {
                        switch (aggregateType)
                        {
                            case AggregateTypes.Sum:
                                if (!aggregatedValues.ContainsKey(fieldName))
                                    aggregatedValues.Add(fieldName, Convert.ToDouble(valueObject));
                                else
                                    aggregatedValues[fieldName] = aggregatedValues[fieldName] + Convert.ToDouble(valueObject);
                                break;
                            case AggregateTypes.Average:
                                if (!aggregatedValues.ContainsKey(fieldName))
                                    aggregatedValues.Add(fieldName, 0);
                                else
                                    aggregatedValues[fieldName] = aggregatedValues[fieldName] + 0;
                                break;
                            case AggregateTypes.Min:
                                if (!aggregatedValues.ContainsKey(fieldName))
                                    aggregatedValues.Add(fieldName, Convert.ToDouble(valueObject));
                                else
                                    aggregatedValues[fieldName] = Math.Min(aggregatedValues[fieldName], Convert.ToDouble(valueObject));
                                break;
                            case AggregateTypes.Max:
                                if (!aggregatedValues.ContainsKey(fieldName))
                                    aggregatedValues.Add(fieldName, Convert.ToDouble(valueObject));
                                else
                                    aggregatedValues[fieldName] = Math.Max(aggregatedValues[fieldName], Convert.ToDouble(valueObject));
                                break;
                        }
                    }
                }
            }
        }

        private string GetBareFieldName(string fieldName, AggregateTypes aggregateType)
        {
            return fieldName.Substring(aggregateType.ToString().Length + 1, fieldName.Length - aggregateType.ToString().Length - 2);
        }

        private AggregateTypes GetAggregateType(string fieldName)
        {
            AggregateTypes returnValue = AggregateTypes.None;
            foreach (var aggregateTypeName in Enum.GetNames(typeof(AggregateTypes)))
            {
                if (fieldName.Length > aggregateTypeName.Length && fieldName.Substring(0, aggregateTypeName.Length + 1) == aggregateTypeName.ToLower() + "(")
                    returnValue = (AggregateTypes)Enum.Parse(typeof(AggregateTypes), aggregateTypeName);
            }
            return returnValue;
        }
    }

    public class Style : ICloneable
    {

        public Font Font { get; set; }
        public Brush Brush { get; set; }
        public Brush BrushForRectangles { get; set; }
        public Pen Pen { get; set; }
        public static Style Default
        {
            get
            {
                return new Style()
                {
                    Font = new Font("Verdana", 11, FontStyle.Regular, GraphicsUnit.Point),
                    Brush = Brushes.Black,
                    Pen = new Pen(Color.Black, 0.5f),
                    BrushForRectangles = Brushes.PaleTurquoise
                };
            }
        }


        public Style Clone()
        {
            return new Style()
            {
                Font = this.Font,
                Brush = this.Brush,
                Pen = this.Pen,
                BrushForRectangles = this.BrushForRectangles
            };
        }

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class ReportObjectField : IReportObject
    {

        public ReportObjectField()
        {
            Alignment = Alignment.Left;
        }

        public ReportObjectField(string FieldName, double XLeft, double YTop, double? XRight = null, double? Width = null, double? YBottom = null, double? Heigth = null, Alignment? Alignment = null, string Mask = null)
        {
            this.FieldName = FieldName;
            this.XLeft = XLeft;
            this.XRight = XRight ?? XLeft + (Width ?? 10);
            this.YTop = YTop;
            this.YBottom = YBottom ?? (Heigth != null ? YTop + Heigth.Value : -1); // cannot be null => -1 : which means not specified, will be set at render time
            this.Alignment = Alignment ?? IceBear.Alignment.Left;
            this.Mask = Mask;
        }

        public string FieldName { get; set; }
        public Style Style { get; set; }
        public Alignment Alignment { get; set; }
        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public string Mask { get; set; }
        public string ID { get; set; }
        public bool CanGrow { get; set; }
        public bool SuppressRepeatingValues { get; set; }
        /// <summary>
        /// When SuppressRepeatingValues is used, SuppressRepeatingValuesChild can be used to reset the supression of a repeating value of that child field
        /// Example: Given a report with fields "Name" & "Date", both with SuppressRepeatingValues=true. If "Name" changes, we want the "Date" repeated even when it is the same value of the previous line.
        /// Assign the "Date" field to the SuppressRepeatingValuesChild of "Name".
        /// </summary>
        public ReportObjectField SuppressRepeatingValuesChild { get; set; }

        internal string previousValue;

        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<IceBear.reportItem>();

            string valueToRender;
            if (aggregatedValues != null && aggregatedValues.ContainsKey(FieldName))
            {
                valueToRender = aggregatedValues[FieldName].ToString(Mask).Trim();
            }
            else
            {
                object value = Utility.getPropertyValue(row, FieldName);
                if (value != null)
                {
                    if (Mask == null)
                        valueToRender = Utility.getPropertyValue(row, FieldName).ToString().Trim(); //row.GetType().GetProperty(FieldName).GetValue(row).ToString().Trim();
                    else
                        valueToRender = string.Format("{0:" + Mask + "}", Utility.getPropertyValue(row, FieldName)); // row.GetType().GetProperty(FieldName).GetValue(row));
                }
                else
                    valueToRender = "";
            }

            if (SuppressRepeatingValues && previousValue == valueToRender)
                return;


            if (SuppressRepeatingValuesChild != null)
                SuppressRepeatingValuesChild.previousValue = null;

            Style usedStyle = Style ?? DefaultStyle;
            double yBottomUsed = YBottom != -1 ? YBottom : YTop + usedStyle.Font.SizeInPoints;

            if (CanGrow)
            {
                /*
                Measured heights (by the g.measurestring a bit further) for a text in point size 11:
                1 lines = 14.74
                2 lines = 28.11
                3 lines = 41.48
                4 lines = 54.84
                5 lines = 68.21
                Delta = 1.215x fontsize (13.37) and an extra ofn 0.1245x fontsize for the first line
                */
                Graphics g = Graphics.FromHwnd(IntPtr.Zero);

                g.PageUnit = GraphicsUnit.Point;
                SizeF s = g.MeasureString(valueToRender, usedStyle.Font, Convert.ToInt32(XRight - XLeft));
                float heigth = s.Height;

                if (heigth > (yBottomUsed - YTop))
                {
                    Growth = heigth - (yBottomUsed - YTop); // + usedStyle.Font.SizeInPoints/2; //this last stuff is experimantal
                    yBottomUsed += Growth;
                }
            }


            RenderedItems.Add(new reportItem() { reportObjectType = reportObjectTypes.String, text = valueToRender, style = usedStyle, X = XOffset + XLeft, Y = YOffset + YTop, W = XRight - XLeft, H = yBottomUsed - YTop, alignment = Alignment });

            previousValue = valueToRender;
        }
    }

    public class ReportObjectLabel : IReportObject
    {
        public ReportObjectLabel()
        {
            this.Alignment = Alignment.Left;
        }

        public ReportObjectLabel(string Text, double XLeft, double YTop, double? XRight=null, double? Width=null, double? YBottom=null, double? Heigth=null, Alignment? Alignment=null)
        {
            this.Text = Text;
            this.XLeft = XLeft;
            this.XRight = XRight ?? XLeft+(Width??100);
            this.YTop = YTop;
            this.YBottom = YBottom ?? (Heigth !=null ? YTop+Heigth.Value : -1); // cannot be null => -1 : which means not specified, will be set at render time
            this.Alignment = Alignment ?? IceBear.Alignment.Left;
        }

        public string Text { get; set; }
        public Style Style { get; set; }
        public Alignment Alignment { get; set; }

        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public string ID { get; set; }

        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<IceBear.reportItem>();

            Style usedStyle = Style ?? DefaultStyle;
            double yBottomUsed = YBottom != -1 ? YBottom : YTop + usedStyle.Font.SizeInPoints;

            //engine.DrawString(Text, usedStyle, XOffset + XLeft, YOffset + YTop, XRight - XLeft, yBottomUsed - YTop, Alignment);
            RenderedItems.Add(new reportItem() { reportObjectType = reportObjectTypes.String, text = Text, style = usedStyle, X = XOffset + XLeft, Y = YOffset + YTop, W = XRight - XLeft, H = yBottomUsed - YTop, alignment = Alignment });
        }
    }

    public class ReportObjectLine : IReportObject
    {

        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public Pen Pen { get; set; }
        public string ID { get; set; }

        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<IceBear.reportItem>();

            Pen penUsed = Pen ?? DefaultStyle.Pen;
            RenderedItems.Add(new reportItem() { reportObjectType = reportObjectTypes.Line, pen = penUsed, X = XLeft + XOffset, Y = YBottom + YOffset, W = XRight - XLeft, H = YBottom - YTop });
        }
    }

    public class ReportObjectRectangle : IReportObject
    {
        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public Pen Pen { get; set; }
        public Brush Brush { get; set; }
        public string ID { get; set; }


        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<IceBear.reportItem>();

            Pen penUsed = Pen ?? DefaultStyle.Pen;
            Brush brushUsed = Brush ?? DefaultStyle.BrushForRectangles;
            //engine.DrawRectangle(penUsed, brushUsed, XLeft + XOffset, YTop + YOffset, XRight - XLeft, YBottom - YTop);
            RenderedItems.Add(new reportItem() { reportObjectType = reportObjectTypes.Rectangle, pen = penUsed, brush = brushUsed, X = XLeft + XOffset, Y = YTop + YOffset, W = XRight - XLeft, H = YBottom - YTop });
        }
    }

    public class ReportObjectImage : IReportObject
    {
        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public string ImageFileName { get; set; }
        public string ImageArrayInRow { get; set; }
        public string ImageFileNameInRow { get; set; }
        public string ID { get; set; }


        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<IceBear.reportItem>();

            Image image;

            try
            {
                if (!string.IsNullOrEmpty(ImageFileName))
                {
                    image = Image.FromFile(ImageFileName);
                }
                else if (!string.IsNullOrEmpty(ImageFileNameInRow))
                {
                    object fileName = row.GetType().GetProperty(ImageFileNameInRow).GetValue(row);
                    if (fileName == null)
                        return;
                    image = Image.FromFile(fileName.ToString());
                }
                else if (!string.IsNullOrEmpty(ImageArrayInRow))
                {
                    object imageArray = row.GetType().GetProperty(ImageArrayInRow).GetValue(row);
                    if (imageArray == null)
                        return;

                    image = Image.FromStream(new MemoryStream((byte[])imageArray));

                }
                else
                    return; //no image provided
            }
            catch (Exception e)
            {
                //AddToLog("ERROR", "Error loading picture", e);
                return;
            }

            double width = 0; // XRight-XLeft;
            double height = 0; // YTop-YBottom;
            if (XRight == 0 && YBottom == 0)
            {
                width = image.Width;
                height = image.Height;
            }
            else if (XRight == 0)
            {
                height = YBottom - YTop;
                width = image.Width * (height) / image.Height;
            }
            else
            {
                width = XRight - XLeft;
                height = image.Height * (width) / image.Width;
            }
            RenderedItems.Add(new reportItem() { reportObjectType = reportObjectTypes.Image, X = XLeft + XOffset, Y = YTop + YOffset, W = width, H = height, image = image });
        }
    }

    public class ReportObjectSubReport : IReportObject
    {
        public double XLeft { get; set; }
        public double XRight { get; set; }
        public double YBottom { get; set; }
        public double YTop { get; set; }
        public string ID { get; set; }
        public Report SubReport { get; set; }
        public string DataSource { get; set; }

        public void Render2(object row, double XOffset, double YOffset, Style DefaultStyle, out List<reportItem> RenderedItems, out double Growth, Dictionary<string, double> aggregatedValues = null)
        {
            Growth = 0;
            RenderedItems = new List<reportItem>();

            SubReport.DataSource = row.GetType().GetProperty(DataSource).GetValue(row) as IEnumerable<object>;
            SubReport.TopMargin = 0;
            SubReport.BottomMargin = 0;
            SubReport.generateGenericReport();


            YOffset += YTop;
            XOffset += XLeft;
            foreach (reportItem item in SubReport.genericReport.CurrentPage.reportItems)
            {
                Growth = Math.Max(Growth, item.Y + item.H);

                item.Y += YOffset;
                item.X += XOffset;

                RenderedItems.Add(item);
            }
        }
    }

    //public class ReportObjectBarcode : IReportObject
    //{
    //    public double XLeft { get; set; }
    //    public double XRight { get; set; }
    //    public double YBottom { get; set; }
    //    public double YTop { get; set; }
    //    public string ID { get; set; }

    //    public void Render(object row, IRenderEngine engine, double XOffset, double YOffset, Style DefaultStyle, Dictionary<string, double> aggregatedValues = null)
    //    {
    //        engine.DrawBarcode();
    //    }
    //}

    public class ReportGroup
    {
        public ReportGroup()
        {
            ForceToBottomOfPage = false;
        }

        public ReportSection HeaderSection { get; set; }
        public ReportSection FooterSection { get; set; }
        public bool StartOnNewPage { get; set; }
        public bool ForceToBottomOfPage { get; set; }

        public enum AggregateTypes { Sum, Count, Average, Min, Max }

        object _previousRow;
        internal object previousRow
        {
            get { return _previousRow; }
            set
            {
                _previousRow = value;
            }
        }
        internal Boolean GroupingHasChanged(object row)
        {
            if (previousRow == null)
                return true;

            object previousValue = Utility.getPropertyValue(previousRow, this.GroupingKey);
            object currentValue = Utility.getPropertyValue(row, this.GroupingKey);

            return  !previousValue.Equals(currentValue);
        }

        public string GroupingKey { get; set; }
    }

    public class GenericReport
    {
        public GenericReport()
        {
            reportPages = new List<reportPage>();
        }

        internal List<reportPage> reportPages { get; set; }

        internal reportPage CurrentPage { get { return reportPages.Count == 0 ? null : reportPages[reportPages.Count - 1]; } }

        internal void NewPage()
        {
            reportPages.Add(new reportPage());
        }

        internal void AddReportItems(List<reportItem> ReportItems, double YOffset)
        {
            foreach (reportItem item in ReportItems)
            {
                item.Y += YOffset;
                CurrentPage.reportItems.Add(item);
            }
        }
    }

    #endregion

    #region Enumerators
    public enum Alignment { Left = 1, Right = 3, Center = 2, Justify = 4 }
    public enum AggregateTypes { Sum, Count, Average, Min, Max, None }
    #endregion

    #region Delegates
    public delegate bool ConditionDelegate(string ReportObjectID, object Row);
    public delegate Style StyleDelegate(string ReportObjectID, object Row);
    #endregion

    //public struct PageType
    //{
    //    string Name;
    //    double Heigth;
    //    double Width;
    //}

    enum reportObjectTypes { Rectangle, String, Line, Image}


    internal class reportPage
    {
        public reportPage()
        {
            reportItems= new List<reportItem>();
        }

        internal List<reportItem> reportItems { get; set; }
    }

    public class reportItem 
    {

        internal reportObjectTypes reportObjectType { get; set; }
        internal Pen pen { get; set; }
        internal Brush brush { get; set; }
        internal double X { get; set; }
        internal int XPrint { get { return Convert.ToInt32(X *1169/842); } }
        internal double Y { get; set; }
        internal int YPrint { get { return Convert.ToInt32(Y * 1169 / 842); } }
        internal double W { get; set; }
        internal int WPrint { get { return Convert.ToInt32(W * 1169 / 842); } }
        internal double H { get; set; }
        internal int HPrint { get { return Convert.ToInt32(H * 1169 / 842); } }
        internal string text { get; set; }
        internal Style style { get; set; }
        internal Alignment alignment { get; set; }
        internal Image image { get; set; }
    }

    internal class Utility
    {
        public static object getPropertyValue(object obj, string propertyName)
        {
            try
            {
                foreach (string propName in propertyName.Split('.'))
                {
                    if (obj == null)
                        break;

                    obj = obj.GetType().GetProperty(propName).GetValue(obj, null);
                }
                return obj;
            }
            catch
            {
                return "";
            }
        }
    }
}
