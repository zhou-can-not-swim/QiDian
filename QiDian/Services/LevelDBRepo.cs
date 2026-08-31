using LevelDB;
using QiDian.Models;
using System.Text.Json;
using Zhou.LevelDB.Services;

namespace QiDian.Services
{
    public class LevelDBRepo
    {
        private LevelDBService _levelDBService { get; set; }
        public LevelDBRepo(LevelDBService l) {
            _levelDBService = l;
        }

        /// <summary>
        /// 保存一个文件到数据库
        /// </summary>
        /// <param name="key"></param>
        /// <param name="f"></param>
        public void Save(string key, FileEntry f) {
            FileEntryDto file = FileEntryMapper.ToDto(f);
            string js = JsonSerializer.Serialize(file);
            try
            {
                _levelDBService.Put(key, js);
            }
            catch {}
        }


        public void Delete() { }

    }

}
