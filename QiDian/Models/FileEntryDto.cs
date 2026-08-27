using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiDian.Models
{
    public class FileEntryDto
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public int UsageCount { get; set; }
    }

    // 转换方法
    public static class FileEntryMapper
    {
        public static FileEntryDto ToDto(this FileEntry entry)
        {
            return new FileEntryDto
            {
                FileName = entry.FileName,
                FullPath = entry.FullPath,
                UsageCount = entry.UsageCount
            };
        }

        public static FileEntry ToModel(this FileEntryDto dto)
        {
            return new FileEntry
            {
                FullPath = dto.FullPath,
                UsageCount = dto.UsageCount
            };
        }
    }
}
