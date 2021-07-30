using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Las_to_SqLite
{
    public class AppSS : BindableSingleton<AppSS>
    {
        private AppSS() { }

        public override void SetOrReset()
        {
     
        }

        private string _workingDirectory;
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set => SetProperty(ref _workingDirectory, value);
        }

        public void WpfFunction_BrowseWorkingFolder()
        {
            FolderBrowserDialog fbDialog = new FolderBrowserDialog();

            fbDialog.Description = "Select the working folder";
            fbDialog.RootFolder = Environment.SpecialFolder.Desktop;
            fbDialog.ShowNewFolderButton = false;
            DialogResult result = fbDialog.ShowDialog();

            WorkingDirectory = fbDialog.SelectedPath;
        }

        public async void WpfFunction_ExecuteReadLasFilesAndWriteSqLiteDatabase()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                DirectoryInfo dir = new DirectoryInfo(WorkingDirectory);
                FileInfo[] lasFiles = dir.GetFiles("*.las");
                FileInfo[] sqliteFiles = dir.GetFiles("point cloud.sqlite");
                
                if (lasFiles.Length == 0) throw new InvalidOperationException("The directory does not contain any LAS file.");
                if (sqliteFiles.Length > 1) throw new InvalidOperationException("The directory contains more than one 'point cloud.sqlite' file.");
                
                BusyOverlayBindings.I.Title = $"Adding the data of the LAS files into the SqLite database.";
                BusyOverlayBindings.I.ShowOverlay();

                void lf_Work()
                {
                    // The SqLite file does not exist
                    if (sqliteFiles.Length == 0)
                    {
                        BusyOverlayBindings.I.SetIndeterminate("Initializing the SqLite file.");

                        string sqlFileName = Path.Combine(WorkingDirectory, "point cloud.sqlite");

                        SQLiteConnection.CreateFile(sqlFileName);

                        SQLiteConnectionStringBuilder connectionStringBuilder = new SQLiteConnectionStringBuilder {DataSource = sqlFileName};
                        using (SQLiteConnection sqliteConn = new SQLiteConnection(connectionStringBuilder.ConnectionString))
                        {
                            sqliteConn.Open();

                            // Creates the table
                            using (SQLiteCommand createTable = new SQLiteCommand(@"CREATE TABLE [Points](
  [X] DOUBLE NOT NULL, 
  [Y] DOUBLE NOT NULL, 
  [Z] DOUBLE NOT NULL);", sqliteConn))
                            {
                                createTable.ExecuteNonQuery();
                            }

                            // Creates the indexes
                            using (SQLiteCommand createTable = new SQLiteCommand("CREATE INDEX idx_X ON [Points](X);", sqliteConn))
                            {
                                createTable.ExecuteNonQuery();
                            }

                            // Creates the indexes
                            using (SQLiteCommand createTable = new SQLiteCommand("CREATE INDEX idx_Y ON [Points](Y);", sqliteConn))
                            {
                                createTable.ExecuteNonQuery();
                            }

                            // Creates the indexes
                            using (SQLiteCommand createTable = new SQLiteCommand("CREATE INDEX idx_Z ON [Points](Z);", sqliteConn))
                            {
                                createTable.ExecuteNonQuery();
                            }
                        }
                    }

                    for (int fileIndex = 0; fileIndex < lasFiles.Length; fileIndex++)
                    {
                        BusyOverlayBindings.I.SetDeterminate($"Working with LAS file {lasFiles[fileIndex].Name}. {fileIndex + 1}/{lasFiles.Length}");

                        // Opens the LAS file
                        LASReader lasreader = new LASReader(@"F:\sample_in.las");
                    }
                            int bufferCounter = 1;
                            BusyOverlayBindings.I.SetDeterminate("Saving the joint displacement table.", "Object");
                            SQLiteTransaction transaction = sqliteConn.BeginTransaction();
                            for (int i = 0; i < jointDisplacementDatas.Count; i++)
                            {
                                JointDisplacementData currDispData = jointDisplacementDatas[i];

                                using (SQLiteCommand insertCommand = new SQLiteCommand(currDispData.SQLite_InsertStatement, sqliteConn))
                                {
                                    insertCommand.ExecuteNonQuery();
                                }

                                if (bufferCounter % 1000 == 0)
                                {
                                    // Commits and add a new transaction
                                    transaction.Commit();
                                    transaction.Dispose();
                                    transaction = sqliteConn.BeginTransaction();
                                }

                                bufferCounter++;

                                BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currDispData.Obj);
                            }

                            // Commits last transaction
                            transaction.Commit();
                            transaction.Dispose();

                            bufferCounter = 1;
                            BusyOverlayBindings.I.SetDeterminate("Saving the joint coordinate table.", "Point");
                            transaction = sqliteConn.BeginTransaction();
                            for (int i = 0; i < selPoints.Count; i++)
                            {
                                SapPoint currPoint = selPoints[i];

                                using (SQLiteCommand insertCommand =
                                    new SQLiteCommand(currPoint.SQLite_InsertStatement, sqliteConn))
                                {
                                    insertCommand.ExecuteNonQuery();
                                }

                                if (bufferCounter % 1000 == 0)
                                {
                                    // Commits and add a new transaction
                                    transaction.Commit();
                                    transaction.Dispose();
                                    transaction = sqliteConn.BeginTransaction();
                                }

                                bufferCounter++;

                                BusyOverlayBindings.I.UpdateProgress(i, jointDisplacementDatas.Count, currPoint.Name);
                            }

                            // Commits last transaction
                            transaction.Commit();
                            transaction.Dispose();
                        }
                    }
                    else
                    {
                        // Creates the new file
                        string sqliteFile = Path.Combine(WorkingDirectory, "point cloud.sqlite");
                        SQLiteConnection.CreateFile(sqliteFile);
                        SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={sqliteFile};Version=3;");
                        m_dbConnection.Open();

                        string sql = "create table highscores (name varchar(20), score int)";

                        SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                        command.ExecuteNonQuery();

                        sql = "insert into highscores (name, score) values ('Me', 9001)";

                        command = new SQLiteCommand(sql, m_dbConnection);
                        command.ExecuteNonQuery();

                        m_dbConnection.Close();
                    }
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                // Closes the overlay
                BusyOverlayBindings.I.HideOverlayAndReset();
            }
        }
    }
}
