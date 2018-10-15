using Abbyy.CloudOcrSdk;
using System;
using System.ComponentModel;

namespace WpfApp1
{
    public class UserTask : INotifyPropertyChanged
    {
        public UserTask(string filePath)
        {
            SourceFilePath = filePath;
            TaskId = "<unknown>";
            TaskStatus = "<initializing>";
            SourceIsTempFile = false;
        }

        public UserTask(OcrSdkTask task)
        {
            SourceFilePath = null;
            TaskId = task.Id.ToString();
            TaskStatus = task.Status.ToString();
            FilesCount = task.FilesCount;
            Description = task.Description;
            RegistrationTime = task.RegistrationTime;
            StatusChangeTime = task.StatusChangeTime;

            SourceIsTempFile = false;
        }

        public bool SourceIsTempFile
        {
            get;
            set;
        }

        public string SourceFilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                if (!String.IsNullOrEmpty(value))
                {
                    _fileName = System.IO.Path.GetFileName(_filePath);
                }
                else
                {
                    _fileName = null;
                }
            }
        }


        public string SourceFileName { get { return _fileName; } }
        public string TaskId
        {
            get
            {
                return _taskId;
            }
            set
            {
                _taskId = value;
                NotifyPropertyChanged("TaskId");
            }
        }

        public string TaskStatus
        {
            get
            {
                return _taskStatus;
            }
            set
            {
                _taskStatus = value;
                NotifyPropertyChanged("TaskStatus");
            }
        }


        public string OutputFilePath
        {
            get
            {
                return _outputFilePath;
            }
            set
            {
                _outputFilePath = value;
                NotifyPropertyChanged("OutputFilePath");
            }
        }

        public int FilesCount
        {
            get { return _filesCount; }
            set { _filesCount = value; NotifyPropertyChanged("FilesCount"); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }

        public DateTime RegistrationTime
        {
            get { return _registrationTime; }
            set
            {
                _registrationTime = value;
                NotifyPropertyChanged("RegistrationTime");
            }
        }

        public DateTime StatusChangeTime
        {
            get { return _statusChangeTime; }
            set
            {
                _statusChangeTime = value;
                NotifyPropertyChanged("StatusChangeTime");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public bool IsFieldLevel
        {
            get;
            set;
        }

        public string RecognizedText
        {
            get { return _recognizedText; }
            set { _recognizedText = value; NotifyPropertyChanged("RecognizedText"); }
        }

        public System.Drawing.Image SourceImage
        {
            get { return _sourceImage; }
            set { _sourceImage = value; NotifyPropertyChanged("SourceImage"); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; NotifyPropertyChanged("ErrorMessage"); }
        }

        private string _filePath;
        private string _fileName;
        private string _taskId;
        private string _taskStatus;
        private string _outputFilePath;

        private int _filesCount;
        private string _description;


        private DateTime _registrationTime;
        private DateTime _statusChangeTime;

        private string _recognizedText = null;
        private System.Drawing.Image _sourceImage;

        private string _errorMessage;
    }
}