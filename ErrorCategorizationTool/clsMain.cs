using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace ErrorCategorizationTool
{
    public class clsMain
    {
        private DataTable dtResult;
        private DataTable dtStackTrace;
        private DataTable dtChart;
        private DataTable dtPieChart;
        private DataTable dtErrors;
        private DataTable dtPassed;
        private DataTable dtTotalCases;
        public clsMain()
        {
            dtResult = new DataTable();
            dtResult.Columns.Add("Row",typeof(int));
            dtResult.PrimaryKey = new DataColumn[] { dtResult.Columns["Row"] };
            dtResult.Columns.Add("TestCase Id", typeof(string));
            dtResult.Columns.Add("TestCase Name", typeof(string));
            dtResult.Columns.Add("StackTrace", typeof(string));
            dtResult.Columns.Add("Error Message", typeof(string));
            dtResult.Columns.Add("ErrorLevel", typeof(string));

            dtStackTrace = new DataTable();
            dtStackTrace.Columns.Add("TestCase Name",typeof(string));
            dtStackTrace.Columns.Add("StackTrace",typeof(string));

            dtChart = new DataTable();
            dtChart.Columns.Add("Error Message", typeof(string));
            dtChart.Columns.Add("Error Level", typeof(int));

            dtPieChart = new DataTable();
            dtPieChart.Columns.Add("Error Message", typeof(string));
            dtPieChart.Columns.Add("Number of failed testcases", typeof(int));

            dtPassed = new DataTable();
            dtPassed.Columns.Add("No", typeof(int));
            dtPassed.Columns.Add("TestCase Id", typeof(string));
            dtPassed.Columns.Add("TestCase Name", typeof(string));

            dtTotalCases = new DataTable();
            dtTotalCases.Columns.Add("Case", typeof(string));
            dtTotalCases.Columns.Add("Jenkins Result", typeof(int));
        }
        public void ReadJenkinsFailureResults()
        {
            string[] buildFiles = Directory.GetFiles(Constants.buildPath);

            if (buildFiles.Length.Equals(0))
            {
                Console.WriteLine("No Builds");
                return;
            }

            if (!File.Exists(Constants.errorFilePath))
            {
                Console.WriteLine("Error file is not available");
                return;
            }

            dtErrors = GetDataTableFromExcel(Constants.errorFilePath);

            if (dtErrors.Rows.Count.Equals(0))
            {
                Console.WriteLine("No Error levels");
                return;
            }

            foreach (string build in buildFiles)
            {
                List<string> contents = File.ReadAllLines(build).ToList();
                dtTotalCases.Rows.Clear();

                var lstStackTrace = (from trace in contents
                                         where trace.Contains(Constants.StackTrace)
                                         select trace).ToList();

                var lstPassedTestCase = (from failure in contents
                                        where failure.Contains(Constants.Passed)
                                        select failure).ToList();
                if(lstPassedTestCase.Count > 0)
                {
                    dtTotalCases.Rows.Add(Constants.Passed.TrimEnd(':'), lstPassedTestCase.Count);
                }

                var lstTestCase = (from failure in contents
                                   where failure.Contains(Constants.Failure) && failure.Trim().StartsWith(Constants.Failure)
                                   select failure).ToList();

                dtTotalCases.Rows.Add(Constants.Failure.TrimEnd(':'), lstTestCase.Count);

                var lstIndexes = contents.Select((item, index) => new { Item = item, Index = index })
                  .Where(v => v.Item.Trim().ToUpper() == Constants.ErrorMessage.ToUpper())
                  .Select(v => v.Index)
                  .ToArray();

                var lstSTIndexes = contents.Select((item, index) => new { Item = item, Index = index })
                  .Where(v => v.Item.Trim().ToUpper() == Constants.StackTrace.ToUpper())
                  .Select(v => v.Index)
                  .ToArray();

                dtResult.Rows.Clear();
                dtStackTrace.Rows.Clear();
                dtChart.Rows.Clear();
                dtPieChart.Rows.Clear();
                dtPassed.Rows.Clear();

                for(int passIndex = 0; passIndex < lstPassedTestCase.Count; passIndex++)
                {
                    string tc = string.Empty;
                    string trailId = string.Empty;
                    string[] data = lstPassedTestCase[passIndex].Split("(");
                    if (data[0].Contains("."))
                    {
                        string[] tcase = data[0].Split(".");
                        tc = tcase[tcase.Length - 1].Trim();
                        trailId = data[1].TrimEnd(')').Replace('"', ' ').Trim();
                    }
                    else if (data[0].Contains(':'))
                    {
                        string[] tcase = data[0].Split(":");
                        tc = tcase[tcase.Length - 1].Trim();
                        var vv = data[1].Split("[");
                        var v1 = vv[0].TrimEnd(')').Replace('"', ' ').Trim();
                        trailId = v1.Replace(')', ' ').Trim();
                    }
                    int tcId = 0;
                    if (trailId.Contains(','))
                    {
                        string[] splitt = trailId.Split(',');
                        tcId = Convert.ToInt32(splitt[splitt.Length - 1]);
                    }
                    else
                    {
                        tcId = Convert.ToInt32(trailId);
                    }
                    dtPassed.Rows.Add(passIndex + 1, tcId, tc);
                }

                for (int index = 0; index < lstTestCase.Count; index++)
                {
                    string tc = string.Empty;
                    string trailId = string.Empty;
                    string[] data = lstTestCase[index].Split("(");
                    if (data[0].Contains("."))
                    {
                        string[] tcase = data[0].Split(".");
                         tc = tcase[tcase.Length - 1].Trim();
                        trailId = data[1].TrimEnd(')').Replace('"', ' ').Trim();
                    }
                    else if (data[0].Contains(':'))
                    {
                        string[] tcase = data[0].Split(":");
                         tc = tcase[tcase.Length - 1].Trim();
                        var vv = data[1].Split("[");
                        var v1 = vv[0].TrimEnd(')').Replace('"', ' ').Trim();
                        trailId = v1.Replace(')', ' ').Trim();
                    }
                    int tcId = 0;
                    if (trailId.Contains(','))
                    {
                        try
                        {
                            string[] splitt = trailId.Split(',');
                            tcId = Convert.ToInt32(splitt[splitt.Length - 1]);
                        }
                        catch(Exception ex)
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            tcId = Convert.ToInt32(trailId);
                        }
                        catch { continue; }
                    }
                    string message = contents[lstIndexes[index] + 1];

                    string stackMessage = string.Empty; //contents[lstSTIndexes[index] + 1];
                    int traceloop = 1;bool isStackTrace = false;
                    if (index < lstStackTrace.Count)
                    {
                        while (!isStackTrace)
                        {
                            if (contents[lstSTIndexes[index] + traceloop].Trim().Equals(Constants.StandardOutputMessages))
                            {
                                isStackTrace = true;
                            }
                            else
                            {
                                if (contents[lstSTIndexes[index] + traceloop].Contains("cs:line"))
                                {
                                    stackMessage += contents[lstSTIndexes[index] + traceloop];
                                }
                                traceloop += 1;
                            }
                        }
                    }
                    string[] traceArray = stackMessage.Split(" in ");
                    string stackTraceMessage = string.Empty;
                    foreach(string line in traceArray)
                    {
                        string classNameLine = line;
                        if (line.Contains("cs:line"))
                        {
                            if(line.Contains("   at "))
                            {
                                string[] lines = line.Split("   at ");
                                classNameLine = lines[0].ToString();
                            }
                            string[] clsName = classNameLine.Split("\\");
                            stackTraceMessage += clsName[clsName.Count()-1] + "\n";
                            goto skip;
                        }
                    }
                skip:

                    string errorlevels = string.Empty;
                    errorlevels = (from errRow in dtErrors.AsEnumerable()
                                                where  Convert.ToInt32(errRow.Field<string>("Error Level").Trim()) > 100 
                                                && message.ToLower().Contains(errRow.Field<string>("Error Message").ToLower())
                                                select errRow.Field<string>("Error Level")).FirstOrDefault();

                    if (string.IsNullOrEmpty(errorlevels))
                    {
                         errorlevels = (from errRow in dtErrors.AsEnumerable()
                                              where message.Contains(errRow.Field<string>("Error Message"))
                                              select errRow.Field<string>("Error Level")).FirstOrDefault();
                    }
                    int errorlevel = 0;
                    if (!string.IsNullOrEmpty(errorlevels))
                    {
                        errorlevel = Convert.ToInt32(errorlevels);
                    }
                    //tc = tc + $"(\"{tcId}\")";
                    dtResult.Rows.Add(index + 1, tcId, tc, stackTraceMessage, message, errorlevel);
                    dtStackTrace.Rows.Add(tc, stackTraceMessage);
                    dtChart.Rows.Add(tc, errorlevel);
                }
                DataView dv = dtStackTrace.DefaultView;
                dv.Sort = "StackTrace asc";
                dtStackTrace = dv.ToTable();

                DataView dvResut = dtResult.DefaultView;
                dvResut.Sort = "StackTrace asc";
                dtResult = dvResut.ToTable();

                var groupedData = (from b in dtChart.AsEnumerable()
                                  group b by b.Field<int>("Error Level") into g
                                  select new
                                  {
                                      ErrorTag = g.Key,
                                      Count = g.Count()
                                      //ChargeSum = g.Sum(x => x.Field<int>("Error Level"))
                                  }).ToList();
                for(int i=0;i<groupedData.Count; i++)
                {
                    var msg = (dtErrors.AsEnumerable()
                        .Where(row => Convert.ToInt32(row.Field<string>("Error Level").Trim()) == groupedData[i].ErrorTag)
                        .Select(row => row.Field<string>("Error Message"))).FirstOrDefault();
                    if (string.IsNullOrEmpty(msg))
                    {
                        msg = "Others";
                    }
                    dtPieChart.Rows.Add($"{msg} ({groupedData[i].Count})", groupedData[i].Count);
                }
                ExportExcel(Path.GetFileName(build));
            }
        }

        public void ExportExcel(string buildName)
        {
            var file = new FileInfo(Constants.exportFilePath);
            using (ExcelPackage excel = new ExcelPackage(file))
            {
                ExcelWorksheet sheetcreate;
                //Create a New csv file
                if (!File.Exists(Constants.exportFilePath))
                {
                    sheetcreate = excel.Workbook.Worksheets.Add(buildName);
                }
                //Update to existing file
                else
                {
                    sheetcreate = CreateExcelWorkSheet(excel, buildName);
                }
                sheetcreate.Cells["A1"].LoadFromDataTable(dtResult, true);

                int passedInitialRow = dtResult.Rows.Count + 5;
                int stackTraceCount = 0;
                if (dtPassed.Rows.Count > 0)
                {
                    sheetcreate.Cells[$"A{passedInitialRow}"].LoadFromDataTable(dtPassed, true);
                    stackTraceCount = passedInitialRow + dtPassed.Rows.Count + 5;
                }
                else
                {
                    stackTraceCount = passedInitialRow;
                }
                sheetcreate.Cells[$"A{stackTraceCount}"].LoadFromDataTable(dtStackTrace, true);

                //Testcases result
                sheetcreate.Cells["G2"].LoadFromDataTable(dtTotalCases, true);

                int pieChartRow = 6;
                sheetcreate.Cells[$"G{pieChartRow}"].LoadFromDataTable(dtPieChart, true);
                //Draw a chart
                var pieChart = sheetcreate.Drawings.AddChart("chart", eChartType.Pie);

                int chartRow = pieChartRow + dtPieChart.Rows.Count;

                // Define series for the chart
                var series = pieChart.Series.Add($"H{pieChartRow + 1}: H{pieChartRow + 1 + dtPieChart.Rows.Count}", $"G{pieChartRow + 1}: G{pieChartRow + 1 + dtPieChart.Rows.Count}");
                pieChart.Border.Fill.Color = System.Drawing.Color.Green;
                pieChart.Title.Text = "Jenkins Failures";
                pieChart.SetSize(650, 700);

                // Add to 6th row and to the 6th column
                pieChart.SetPosition(chartRow + 1,0, 7, 0);

                excel.Save();
            }
        }

        private ExcelWorksheet CreateExcelWorkSheet(ExcelPackage excel, string sheetName, bool IsChart = false)
        {
            if (sheetName.Length > 31)
            {
                if (IsChart)
                    sheetName = sheetName.ToUpper().Substring(0, 25);
                else
                    sheetName = sheetName.ToUpper().Substring(0, 31);
            }
            ExcelWorkbook excelWorkBook = excel.Workbook;
            var isSheet = (from sheet in excel.Workbook.Worksheets
                           where sheet.Name.ToUpper().Equals(sheetName.ToUpper())
                           select sheet.Name).FirstOrDefault();

            if (!string.IsNullOrEmpty(isSheet))
            {
                excel.Workbook.Worksheets.Delete(sheetName);
            }
            excel.Workbook.Worksheets.Add(sheetName);
            return excelWorkBook.Worksheets[excel.Workbook.Worksheets.Count - 1];
        }
        private bool CheckBuildIsAvailbale(ExcelPackage excel,string sheetName)
        {
            ExcelWorkbook excelWorkBook = excel.Workbook;
            var isSheet = (from sheet in excel.Workbook.Worksheets
                           where sheet.Name.ToUpper().Equals(sheetName.ToUpper())
                           select true).FirstOrDefault();
            return isSheet;
        }
        public DataTable GetDataTableFromExcel(string path, bool hasHeader = true)
        {
            using (var pck = new ExcelPackage())
            {
                using (var stream = File.OpenRead(path))
                {
                    pck.Load(stream);
                }
                var ws = pck.Workbook.Worksheets.First();
                DataTable tbl = new DataTable();
                foreach (var firstRowCell in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                {
                    tbl.Columns.Add(hasHeader ? firstRowCell.Text : string.Format("Column {0}", firstRowCell.Start.Column));
                }
                var startRow = hasHeader ? 2 : 1;
                for (int rowNum = startRow; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                    DataRow row = tbl.Rows.Add();
                    foreach (var cell in wsRow)
                    {
                        row[cell.Start.Column - 1] = cell.Text;
                    }
                }
                return tbl;
            }
        }
        private string GetAttachmentsDirectory()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            //// Get Destination Directory
            //if (attachmentDestDir)
            //{
            //    string testAttachmentsDir = directory.Parent.FullName;
            //    string attachmentDir = Path.Combine(testAttachmentsDir, "Jenkins");
            //    if (!Directory.Exists(attachmentDir))
            //    {
            //        Directory.CreateDirectory(attachmentDir);
            //    }
            //    return attachmentDir;
            //}
            string directoryPath = directory.FullName + Path.DirectorySeparatorChar + "ErrorCategorizationTool";
            return Directory.GetDirectories(directoryPath, $"Jenkins").ToList().FirstOrDefault();
        }
    }
}
