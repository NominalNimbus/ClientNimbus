using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    public static class ExcelExportManager
    {
        public static void ExportOrders(string filename, string sheetName, IList<Order> orders)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            var summaryValue = orders.Count > 0 ? orders.Sum(order => order.Commission) : 0;

            Export(filename, sheetName, Order.GetExportHeaders(), orders.Select(o => o.GetExportValues()).ToArray(),
                "Total Trade C.", summaryValue, 12);
        }

        public static void ExportPositions(string filename, IList<Position> positions)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));

            var summaryValue = positions.Count > 0 ? positions.Sum(order => order.Profit) : 0;

            Export(filename, "Positions History", Position.GetExportHeaders(), positions.Select(o => o.GetExportValues()).ToArray(),
                 "Total Profit", summaryValue, 4);
        }

        private static void Export(string filename, string sheetName, string[] headers, object[][] values,
            string summaryHeader, object summaryValue, int summaryColumnIndex)
        {
            using (var xlBook = new XLWorkbook())
            {
                var xlSheet = xlBook.Worksheets.Add(sheetName);
                var rowIndex = 1;
                var columnIndex = 1;
                foreach (var header in headers)
                {
                    xlSheet.Cell(rowIndex, columnIndex++).Value = header;
                }

                for (var r = 0; r < values.Length; r++)
                {
                    columnIndex = 1;
                    rowIndex++;
                    foreach (var value in values[r])
                    {
                        xlSheet.Cell(rowIndex, columnIndex++).Value = value;
                    }
                }

                rowIndex += 2;
                xlSheet.Cell(rowIndex, summaryColumnIndex).Value = summaryHeader;

                rowIndex++;
                xlSheet.Cell(rowIndex, summaryColumnIndex).Value = summaryValue;

                xlBook.SaveAs(filename);
            }
        }
    }
}