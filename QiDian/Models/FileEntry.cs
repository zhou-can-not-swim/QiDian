using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.ComponentModel;
using System.IO;

namespace QiDian.Models
{
    public class FileEntry : INotifyPropertyChanged
    {
        private string _fullPath;
        private long _size;
        private DateTime _lastModified;

        public string FullPath
        {
            get => _fullPath;
            set { _fullPath = value; OnPropertyChanged(nameof(FullPath)); }
        }

        public string FileName => Path.GetFileName(FullPath);

        public string Directory => Path.GetDirectoryName(FullPath);

        public long Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); OnPropertyChanged(nameof(SizeFormatted)); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(nameof(LastModified)); OnPropertyChanged(nameof(LastModifiedFormatted)); }
        }

        public string SizeFormatted
        {
            get
            {
                if (Size >= 1073741824)
                    return $"{Size / 1073741824.0:F2} GB";
                if (Size >= 1048576)
                    return $"{Size / 1048576.0:F2} MB";
                if (Size >= 1024)
                    return $"{Size / 1024.0:F1} KB";
                return $"{Size} B";
            }
        }

        public string LastModifiedFormatted => LastModified.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>关联的软件名（注册表匹配时填写，用于打分）</summary>
        public string? SoftwareName
        {
            get => _softwareName;
            set { _softwareName = value; OnPropertyChanged(nameof(SoftwareName)); }
        }

        // 可以额外添加使用频率、点击次数等字段
        public int UsageCount
        {
            get => _usageCount;
            set { _usageCount = value; OnPropertyChanged(nameof(UsageCount)); }
        }

        public DateTime LastUsed
        {
            get => _lastUsed;
            set { _lastUsed = value; OnPropertyChanged(nameof(LastUsed)); }
        }

        private string? _softwareName;
        private int _usageCount;
        private DateTime _lastUsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}