using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Abbyy.CloudOcrSdk;
using System.Net;

namespace WpfApp1
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RestServiceClient restClient;
        private RestServiceClientAsync restClientAsync;


        public MainWindow()
        {
            InitializeComponent();

            restClient = new RestServiceClient();
            restClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
            restClient.ServerUrl = Properties.Settings.Default.ServerAddress;

            restClient.ApplicationId = Properties.Settings.Default.ApplicationId;
            restClient.Password = Properties.Settings.Default.Password;

            restClientAsync = new RestServiceClientAsync(restClient);

            restClientAsync.UploadFileCompleted += UploadCompleted;
            restClientAsync.TaskProcessingCompleted += ProcessingCompleted;
            restClientAsync.DownloadFileCompleted += DownloadCompleted;
            restClientAsync.ListTasksCompleted += TaskListObtained;
        }

        private void UploadCompleted(object sender, UploadCompletedEventArgs e)
        {
            UserTask task = e.UserState as UserTask;
            task.TaskStatus = "Processing";

            txtEditor.Text = "Processing";

            task.TaskId = e.Result.Id.ToString();
        }

        private void ProcessingCompleted(object sender, TaskEventArgs e)
        {
            UserTask task = e.UserState as UserTask;

            if (task.SourceIsTempFile)
            {
                File.Delete(task.SourceFilePath);
            }

            if (e.Error != null)
            {
                task.TaskStatus = "Processing error";

                txtEditor.Text = "Processing error";

                task.OutputFilePath = "<error>";
                task.ErrorMessage = e.Error.Message;
                if (task.IsFieldLevel)
                {
                    // ErrorMessage is not mapped into a column for
                    // field level tasks
                    task.RecognizedText = String.Format("<{0}>", task.ErrorMessage);
                }
                return;
            }

            if (e.Result.Status == Abbyy.CloudOcrSdk.TaskStatus.NotEnoughCredits)
            {
                task.TaskStatus = "Not enough credits";

                txtEditor.Text = "Not enough credits";

                task.OutputFilePath = "<not enough credits>";
                MessageBox.Show("Not enough credits to process the file.\nPlease add more pages to your application's account.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (e.Result.Status == Abbyy.CloudOcrSdk.TaskStatus.ProcessingFailed)
            {
                task.TaskStatus = "Internal server error";

                txtEditor.Text = "Internal server error";

                task.OutputFilePath = "<error>";
                task.ErrorMessage = e.Result.Error;
                //moveTaskToCompleted(task);
                return;
            }

            if (e.Result.Status != Abbyy.CloudOcrSdk.TaskStatus.Completed)
            {
                task.TaskStatus = task.ErrorMessage = e.Result.Status.ToString();
                task.OutputFilePath = "<error>";
                //moveTaskToCompleted(task);
                return;
            }

            task.TaskStatus = "Downloading";

            txtEditor.Text = "Downloading";

            // Start downloading
            restClientAsync.DownloadFileAsync(e.Result, task.OutputFilePath, task);
        }

        private void DownloadCompleted(object sender, TaskEventArgs e)
        {
            UserTask task = e.UserState as UserTask;
            if (e.Error != null)
            {
                task.TaskStatus = "Downloading error";

                txtEditor.Text = "Downloading error";

                task.OutputFilePath = "<error>";
                task.ErrorMessage = e.Error.Message;
                return;
            }

            //String result = FieldLevelXml.ReadText(task.OutputFilePath);
            //task.RecognizedText = result;

            //txtEditor.Text = result;

            task.TaskStatus = "Ready";

            txtEditor.Text = "Please open result file at:" + task.OutputFilePath;
        }

        private void TaskListObtained(object sender, ListTaskEventArgs e)
        {
            if (e.Error == null)
            {
                OcrSdkTask[] serverTasks = e.Result;

                // move to ServerTasks collection
                //ServerTasks.Clear();
                foreach (OcrSdkTask task in serverTasks.OrderByDescending(t => t.RegistrationTime))
                {
                    UserTask userTask = new UserTask(task);

                    //ServerTasks.Add(userTask);
                }
            }
            else
            {
                MessageBox.Show("Cannot obtain list of server tasks:\n" + e.Error.Message, "error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            string filePath = "";
            if (openFileDialog.ShowDialog() == true)
                filePath = openFileDialog.FileName;

            if (String.IsNullOrEmpty(filePath) == false)
            {
                Fire(filePath);
            }
        }


        private void Fire(string filePath)
        {
            txtEditor.Text = "Start";

            System.Drawing.Bitmap bitmap = GetBitMap(filePath);

            string tempFilePath = System.IO.Path.GetTempFileName();
            bitmap.Save(tempFilePath, System.Drawing.Imaging.ImageFormat.Tiff);

            string outputDir = getOutputDir();

            UserTask task = new UserTask(tempFilePath);
            task.TaskStatus = "Uploading";
            task.SourceIsTempFile = true;
            task.IsFieldLevel = true;
            task.SourceImage = bitmap;

            //TextFieldProcessingSettings settings = new TextFieldProcessingSettings();
            //restClientAsync.ProcessTextFieldAsync(tempFilePath, settings, task);


            ///
            ProcessingSettings settings = new ProcessingSettings();
            task.OutputFilePath = System.IO.Path.Combine(
                outputDir,
                System.IO.Path.GetFileNameWithoutExtension(filePath) + settings.GetOutputFileExt(settings.OutputFormats[0]));
            
            settings.Description = String.Format("{0} -> {1}",
                System.IO.Path.GetFileName(filePath),
                settings.GetOutputFileExt(settings.OutputFormats[0]));

            restClientAsync.ProcessImageAsync(filePath, settings, task);


        }

        private System.Drawing.Bitmap GetBitMap(string _sourceFile)
        {
            System.Drawing.Bitmap src = System.Drawing.Image.FromFile(_sourceFile) as System.Drawing.Bitmap;
            int newWidth = src.Width;
            int newHeight = src.Height;
            var target = new System.Drawing.Bitmap((int)newWidth, (int)newHeight);
            target.SetResolution(src.HorizontalResolution, src.VerticalResolution);

            using (var g = System.Drawing.Graphics.FromImage(target))
            {
                var rect = new System.Drawing.Rectangle((int)0, (int)0, (int)newWidth, (int)newHeight);
                g.DrawImage(src, new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
                                 rect,
                                 System.Drawing.GraphicsUnit.Pixel);
            }

            return target;
        }

        string getOutputDir()
        {
            string outputDir = Properties.Settings.Default.OutputDirectory;
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            return outputDir;
        }
    }
}
