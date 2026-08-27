using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.ComponentModel;
using System.IO;

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
    }
}