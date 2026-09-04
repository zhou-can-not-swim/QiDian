using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QiDian.Services;
using QiDian.Services.SearchLogic;
using System.Diagnostics;
using System.Windows.Forms;

namespace QiDian.BackGroundServices
{
    public class ExecuteEverythingService : BackgroundService
    {

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var everythingPath = System.IO.Path.Combine(baseDirectory, "Everything/Everything.exe");

            EverythingSearchService _es = new EverythingSearchService();
            while (!_es.IsAvailable())
            {
                try {
                    Process.Start(new ProcessStartInfo { 
                        FileName = everythingPath,
                        Arguments = "-startup",  // 后台启动，不显示窗口
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    });
                }
                catch (Exception ex)
                {

                }

            }


            return Task.CompletedTask;
        }
    }
}