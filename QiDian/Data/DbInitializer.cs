using Microsoft.EntityFrameworkCore;

namespace QiDian.Data
{
    public class DbInitializer
    {
        private readonly AppDbContext _db;

        public DbInitializer(AppDbContext db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await _db.Database.EnsureCreatedAsync();

            if (!await _db.NavigationMenus.AnyAsync())
                await SeedNavigationMenusAsync();

        }

        /// <summary>
        /// 导航菜单种子数据：每新增一个页面，需要：
        ///  1. 在 App.xaml.cs RegisterViewModels 中注册 View↔ViewModel；
        ///  2. 在 ConfigureServices 中注册 View/ViewModel 到 DI；
        ///  3. 在下面菜单列表里加一行。
        /// </summary>
        private async Task SeedNavigationMenusAsync()
        {
            var menus = new List<NavigationMenu>
            {
                new() { Title = "首页",     ViewKey = "Home", Icon = "", Order = 1, IsEnabled = true },
            };
            _db.NavigationMenus.AddRange(menus);
            await _db.SaveChangesAsync();
        }

    }
}