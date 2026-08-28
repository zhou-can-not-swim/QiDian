using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace QiDian.Models
{
    public class FileEntry : ReactiveObject
    {

        [Reactive]
        public string FullPath { get; set; }

        public string FileName => Path.GetFileName(FullPath);

        public string Directory => Path.GetDirectoryName(FullPath);


        [Reactive]
        public int UsageCount {  get; set; }

        /// <summary>
        /// 图标（后台线程异步提取后填充，null 时界面显示默认图标）。
        /// 注意：必须在后台线程用 FilePathToIconConverter.ExtractIcon 填充，
        /// 不要在 UI 线程同步提取，否则数据量多时会卡死界面。
        /// </summary>
        [Reactive]
        public ImageSource? Icon { get; set; }
    }
}