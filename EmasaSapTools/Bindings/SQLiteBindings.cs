using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library;

namespace EmasaSapTools.Bindings
{
    public class SQLiteBindings : BindableSingleton<SQLiteBindings>
    {
        private SQLiteBindings(){}
        public override void SetOrReset()
        {
            S2KFileName = string.Empty;
            SQLiteFileName = string.Empty;

            // Builds the regex and the table formats
            DataTable tempTableFormat = SQLiteDataTableFormat(SQLiteTable.FRAME_SECTION_ASSIGNMENTS);
            RegexTableDic.Add(SQLiteTable.FRAME_SECTION_ASSIGNMENTS,
                (BuildRegexFromTableFormat(tempTableFormat), tempTableFormat));

            tempTableFormat = SQLiteDataTableFormat(SQLiteTable.ELEMENT_FORCES_FRAMES);
            RegexTableDic.Add(SQLiteTable.ELEMENT_FORCES_FRAMES,
                (BuildRegexFromTableFormat(tempTableFormat), tempTableFormat));

            tempTableFormat = SQLiteDataTableFormat(SQLiteTable.PROGRAM_CONTROL);
            RegexTableDic.Add(SQLiteTable.PROGRAM_CONTROL,
                (BuildRegexFromTableFormat(tempTableFormat), tempTableFormat));

            
        }

        private bool _HasSQLite_IsEnabled;public bool HasSQLite_IsEnabled { get => _HasSQLite_IsEnabled; set => SetProperty(ref _HasSQLite_IsEnabled, value); }

        private string _SQLiteFileName;public string SQLiteFileName { get => _SQLiteFileName; 
            set
            {
                HasSQLite_IsEnabled = !string.IsNullOrWhiteSpace(value);
                SetProperty(ref _SQLiteFileName, value);
            }
        }

        private bool _ConvertS2KToSQLiteButton_IsEnabled;public bool ConvertS2KToSQLiteButton_IsEnabled { get => _ConvertS2KToSQLiteButton_IsEnabled; set => SetProperty(ref _ConvertS2KToSQLiteButton_IsEnabled, value); }

        private string _S2KFileName;public string S2KFileName { get => _S2KFileName; 
            set
            {
                ConvertS2KToSQLiteButton_IsEnabled = !string.IsNullOrWhiteSpace(value);
                SetProperty(ref _S2KFileName, value);
            }
        }

        private bool _TableImportFramesForces_IsChecked;public bool TableImportFramesForces_IsChecked { get => _TableImportFramesForces_IsChecked; set => SetProperty(ref _TableImportFramesForces_IsChecked, value); }

        public readonly Regex tableStartRegex = new Regex(@"^TABLE:\s*\""(?<tableName>.*)\""");

        public readonly Dictionary<SQLiteTable, (Regex regex, DataTable table)> RegexTableDic =
            new Dictionary<SQLiteTable, (Regex regex, DataTable table)>();

        public static SQLiteTable MatchTableType(string inStringName)
        {
            switch (inStringName)
            {
                case "PROGRAM CONTROL":
                    return SQLiteTable.PROGRAM_CONTROL;
                case "FRAME SECTION ASSIGNMENTS":
                    return SQLiteTable.FRAME_SECTION_ASSIGNMENTS;
                case "ELEMENT FORCES - FRAMES":
                    return SQLiteTable.ELEMENT_FORCES_FRAMES;
                default:
                    throw new S2KHelperException($"The S2K table name {inStringName} is still not supported.");
            }
        }

        private static DataTable SQLiteDataTableFormat(SQLiteTable inTableType)
        {
            DataTable dt;
            switch (inTableType)
            {
                case SQLiteTable.PROGRAM_CONTROL:
                    dt = new DataTable(inTableType.ToString());
                    dt.Columns.Add("ProgramName", typeof(string));
                    dt.Columns.Add("Version", typeof(string));
                    dt.Columns.Add("ProgLevel", typeof(string));
                    dt.Columns.Add("LicenseNum", typeof(string));
                    dt.Columns.Add("LicenseOS", typeof(string));
                    dt.Columns.Add("LicenseSC", typeof(string));
                    dt.Columns.Add("LicenseHT", typeof(string));
                    dt.Columns.Add("CurrUnits", typeof(string));
                    dt.Columns.Add("SteelCode", typeof(string));
                    dt.Columns.Add("ConcCode", typeof(string));
                    dt.Columns.Add("AlumCode", typeof(string));
                    dt.Columns.Add("ColdCode", typeof(string));
                    dt.Columns.Add("RegenHinge", typeof(string));
                    break;
                case SQLiteTable.FRAME_SECTION_ASSIGNMENTS:
                    dt = new DataTable(inTableType.ToString());
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("SectionType", typeof(string));
                    dt.Columns.Add("AutoSelect", typeof(string));
                    dt.Columns.Add("AnalSect", typeof(string));
                    dt.Columns.Add("DesignSect", typeof(string));
                    dt.Columns.Add("MatProp", typeof(string));
                    break;
                case SQLiteTable.ELEMENT_FORCES_FRAMES:
                    dt = new DataTable(inTableType.ToString());
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("Station", typeof(double));
                    dt.Columns.Add("OutputCase", typeof(string));
                    dt.Columns.Add("CaseType", typeof(string));
                    dt.Columns.Add("StepType", typeof(string));
                    dt.Columns.Add("P", typeof(double));
                    dt.Columns.Add("V2", typeof(double));
                    dt.Columns.Add("V3", typeof(double));
                    dt.Columns.Add("T", typeof(double));
                    dt.Columns.Add("M2", typeof(double));
                    dt.Columns.Add("M3", typeof(double));
                    dt.Columns.Add("FrameElem", typeof(string));
                    dt.Columns.Add("ElemStation", typeof(double));
                    break;
                default:
                    throw new S2KHelperException(
                        $"The S2K table name {inTableType.ToString()} is still not supported.");
            }

            return dt;
        }

        private static Regex BuildRegexFromTableFormat(DataTable inTable)
        {
            StringBuilder sb = new StringBuilder(@"^\s*");

            for (int i = 0; i < inTable.Columns.Count; i++)
                if (i == inTable.Columns.Count - 1)
                    sb.Append($@"{inTable.Columns[i].ColumnName}=""?(?<{inTable.Columns[i].ColumnName}>.*)""?");
                else sb.Append($@"{inTable.Columns[i].ColumnName}=""?(?<{inTable.Columns[i].ColumnName}>.+?)""?\s*");

            return new Regex(sb.ToString());
        }

        public static void AddS2KLineToTable(Match inLine, SQLiteTable inTableType, DataTable inTable)
        {
            DataRow newRow = inTable.NewRow();

            try
            {
                for (int i = 0; i < inTable.Columns.Count; i++)
                {
                    DataColumn col = inTable.Columns[i];
                    dynamic convertedData = Convert.ChangeType(inLine.Groups[i + 1].Value, col.DataType);
                    newRow[i] = convertedData;
                }
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("Could not convert the data from the table to the expected format!", ex);
            }

            inTable.Rows.Add(newRow);
        }

        public static string GetSQLiteCommandFromRow(DataRow inRow)
        {
            StringBuilder sb = new StringBuilder("INSERT INTO ");
            sb.Append(inRow.Table.TableName.ToString());
            sb.Append(" ( ");

            foreach (DataColumn col in inRow.Table.Columns)
            {
                sb.Append(col.ColumnName);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);

            sb.Append(" ) VALUES ( ");

            for (int i = 0; i < inRow.ItemArray.Length; i++)
            {
                switch (inRow.ItemArray[i])
                {
                    case string _:
                        sb.Append($"\"{inRow.ItemArray[i]}\"");
                        break;
                    case double _:
                        sb.Append(inRow.ItemArray[i]);
                        break;
                    default:
                        throw new S2KHelperException($"The specified column data format is not allowed.");
                }

                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(");");

            return sb.ToString();
        }

        public void SQLiteCommand_CreateTable(SQLiteConnection inConn, SQLiteTable inTableType)
        {
            StringBuilder sb = new StringBuilder("CREATE TABLE IF NOT EXISTS ");
            sb.Append(inTableType.ToString());
            sb.Append(" ( ");

            foreach (DataColumn column in RegexTableDic[inTableType].table.Columns)
            {
                sb.Append(column.ColumnName);

                if (column.DataType == typeof(double))
                    sb.Append(" DOUBLE ,");
                else if (column.DataType == typeof(string))
                    sb.Append(" TEXT ,");
                else
                    throw new S2KHelperException($"The specified column data format is not allowed.");
            }

            // Deletes the last comma
            sb.Remove(sb.Length - 1, 1);

            sb.Append(" ); ");

            SQLiteCommand command = new SQLiteCommand(inConn);
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }

        public void SQLiteCommand_AddS2KLinesToTable(SQLiteConnection inConn, List<Match> inLines,
            SQLiteTable inTableType)
        {
            if (inLines.Count == 0) throw new S2KHelperException("Number of lines cannot be 0.");

            using (SQLiteTransaction transaction = inConn.BeginTransaction())
            {
                foreach (Match match in inLines)
                {
                    StringBuilder sb = new StringBuilder("INSERT INTO ");
                    sb.Append(inTableType.ToString());
                    sb.Append(" ( ");

                    foreach (DataColumn col in RegexTableDic[inTableType].table.Columns)
                    {
                        sb.Append(col.ColumnName);
                        sb.Append(",");
                    }

                    sb.Remove(sb.Length - 1, 1);

                    sb.Append(" ) VALUES ");

                    sb.Append("(");
                    foreach (DataColumn col in RegexTableDic[inTableType].table.Columns)
                    {
                        if (col.DataType == typeof(double))
                            sb.Append(match.Groups[col.ColumnName]);
                        else if (col.DataType == typeof(string))
                            sb.Append($"\"{match.Groups[col.ColumnName]}\"");
                        else
                            throw new S2KHelperException($"The specified column data format is not allowed.");

                        sb.Append(",");
                    }

                    sb.Remove(sb.Length - 1, 1);
                    sb.Append(");");

                    using (SQLiteCommand sqlitecommand = new SQLiteCommand(sb.ToString(), inConn))
                    {
                        sqlitecommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }
    }

    public enum SQLiteTable
    {
        PROGRAM_CONTROL,
        FRAME_SECTION_ASSIGNMENTS,
        ELEMENT_FORCES_FRAMES
    }
}