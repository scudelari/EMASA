using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmasaSapTools.Resources
{
    public static class ExcelHelper
    {
        public static DataSet GetDataSetFromExcel(string ExcelFile, int[] indexesToGet = null)
        {
            try
            {
                // Reads the Excel
                DataSet fromExcel = default;
                using (FileStream stream = File.Open(ExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Auto-detect format, supports:
                    //  - Binary Excel files (2.0-2003 format; *.xls)
                    //  - OpenXml Excel files (2007 format; *.xlsx)
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        // 2. Use the AsDataSet extension method
                        fromExcel = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            // Gets or sets a value indicating whether to set the DataColumn.DataType 
                            // property in a second pass.
                            UseColumnDataType = true,

                            // Gets or sets a callback to determine whether to include the current sheet
                            // in the DataSet. Called once per sheet before ConfigureDataTable.
                            FilterSheet = (tableReader, sheetIndex) =>
                            {
                                if (indexesToGet == null) return true;
                                if (indexesToGet.Contains(sheetIndex)) return true;
                                else return false;
                            },

                            // Gets or sets a callback to obtain configuration options for a DataTable. 
                            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                            {
                                EmptyColumnNamePrefix = "Column",

                                // Gets or sets a value indicating whether to use a row from the 
                                // data as column names.
                                UseHeaderRow = true,

                                // Gets or sets a callback to determine which row is the header row. 
                                // Only called when UseHeaderRow = true.
                                ReadHeaderRow = (rowReader) =>
                                {
                                    // F.ex skip the first row and use the 2nd row as column headers:
                                    //rowReader.Read();
                                },

                                // Gets or sets a callback to determine whether to include the 
                                // current row in the DataTable.
                                FilterRow = (rowReader) => { return true; },

                                // Gets or sets a callback to determine whether to include the specific
                                // column in the DataTable. Called once per column after reading the 
                                // headers.
                                FilterColumn = (rowReader, columnIndex) => { return true; }
                            }
                        });
                    }
                }

                return fromExcel;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Could not read excel file.", ex);
            }
        }
    }
}