using LevelDB;
using System.IO;
using System.Text;
using System.Text.Json;

namespace QiDian.Helpers.LevelDBHelper
{
    /// <summary>
    /// 使用 LevelDB 持久化用户使用记录。只想记录一个key对应一个path vs-->xxxx/xxx/xxx.exe  意味着key的最优解 那么vs还可能对应vsCode
    /// 也就是key可能一样，但是value一定是不同的
    ///
    /// 改进：设计成复合key的方式， usage_{keyword}_{fullpath}
    /// 数据存储路径：%AppData%\EveryThingWpfApp\matchlog\
    /// </summary>
    public class LevelDbStore : IDisposable
    {
        private readonly DB _db;
        private readonly string _dbPath;
        private readonly object _lock = new object();

        public string KEY_PREFIX = "file_";
        //public int Max_Record_Count = 5; // 每个key最多记录5条历史，超过后删除count+时间权重最小的可以通过时间频次进行计算 假设5天内count=3计算

        public LevelDbStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dbPath = Path.Combine(appData, "EveryThingWpfApp", "matchlog");

            // 确保目录存在
            Directory.CreateDirectory(_dbPath);

            // 打开或创建 LevelDB 数据库
            var options = new Options
            {
                CreateIfMissing = true,
                ErrorIfExists = false,
                WriteBufferSize = 4 * 1024 * 1024,  // 4MB 写缓冲区
                BlockSize = 4 * 1024,
                Cache = new Cache(10*1024*1024),        // 10MB缓存
                CompressionLevel = CompressionLevel.NoCompression  //不压缩（读写更快）
            };

            try
            {
                _db = new DB(options, _dbPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LevelDb] Init failed: {ex.Message}");
                _db = null;
                throw; // 或者处理异常
            }
        }

        public void RecordUsage(string keyWord, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(keyWord) || string.IsNullOrWhiteSpace(fullPath))
                return;

            lock (_lock)
            {
                // 1. 检查数据库是否有效
                if (_db == null)
                {
                    Console.WriteLine("Database is not available");
                    return;
                }

                string compositeKey = $"{KEY_PREFIX}{keyWord}_{fullPath}";
                byte[] keyBytes = Encoding.UTF8.GetBytes(compositeKey);

                MatchRecord record = null;
                bool readSuccess = false;

                // 2. 尝试读取现有记录
                try
                {
                    var readOptions = new ReadOptions();
                    byte[]? existingValue = _db.Get(keyBytes, readOptions);

                    if (existingValue != null)
                    {
                        record = JsonSerializer.Deserialize<MatchRecord>(existingValue);
                        if (record != null)
                        {
                            record.Count++;
                            readSuccess = true;
                        }
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("Corruption"))
                {
                    // 读取时发现损坏，记录日志但不覆盖
                    Console.WriteLine($"Corruption detected when reading key: {compositeKey}, error: {ex.Message}");
                    // 可以选择删除损坏的键
                    // _db.Delete(keyBytes, new WriteOptions());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading key: {compositeKey}, error: {ex.Message}");
                }

                // 3. 如果读取失败或不存在，创建新记录
                if (record == null)
                {
                    record = new MatchRecord
                    {
                        fullPath = fullPath,
                        Count = 1,
                    };
                }

                // 4. 写入数据库
                try
                {
                    byte[] newValue = JsonSerializer.SerializeToUtf8Bytes(record);
                    var writeOptions = new WriteOptions();

                    // 确保写入同步到磁盘（重要！）
                    writeOptions.Sync = true;  // ⚠️ 性能会降低，但数据更安全

                    _db.Put(keyBytes, newValue, writeOptions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing key: {compositeKey}, error: {ex.Message}");
                    throw; // 或者重新抛出，让上层处理
                }
            }
        }

        // 查询某个keyword的所有记录
        public List<MatchRecord> GetRecordsByKeyword(string keyWord)
        {
            var results = new List<MatchRecord>();
            byte[] prefixBytes = Encoding.UTF8.GetBytes($"{KEY_PREFIX}{keyWord}_");

            using var iterator = _db.CreateIterator(new ReadOptions());
            try
            {
                iterator.Seek(prefixBytes);
            }
            catch
            {

            }

            while (iterator.IsValid())
            {
                byte[] keyBytes = iterator.Key();
                string currentKey = Encoding.UTF8.GetString(keyBytes);

                // 检查是否还在该keyword范围内
                if (!currentKey.StartsWith($"{KEY_PREFIX}{keyWord}_"))
                    break;

                byte[] valueBytes = iterator.Value();
                var record = JsonSerializer.Deserialize<MatchRecord>(valueBytes);
                results.Add(record);

                iterator.Next();
            }

            return results;
        }


        /// <summary>
        /// 清空所有记录（慎用）
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                using var iterator = _db.CreateIterator();
                var prefix = Encoding.UTF8.GetBytes(KEY_PREFIX);
                iterator.Seek(prefix);

                var keysToDelete = new List<byte[]>();
                while (iterator.IsValid())
                {
                    var keyBytes = iterator.Key();
                    if (keyBytes.Length < prefix.Length) break;

                    bool hasPrefix = true;
                    for (int i = 0; i < prefix.Length; i++)
                    {
                        if (keyBytes[i] != prefix[i])
                        {
                            hasPrefix = false;
                            break;
                        }
                    }
                    if (!hasPrefix) break;

                    keysToDelete.Add(keyBytes);
                    iterator.Next();
                }

                using var batch = new WriteBatch();
                foreach (var key in keysToDelete)
                {
                    batch.Delete(key);
                }
                _db.Write(batch, null);
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }

        public void Close()
        {
            Dispose();
        }

    }

    /// <summary>
    /// levelDB格式
    /// </summary>
    public class MatchRecord
    {
        public string fullPath { get; set; } = "";
        public int Count { get; set; } = 0;
        public long Timestamp { get; set; } = 0;  // 毫秒时间戳
    }
}