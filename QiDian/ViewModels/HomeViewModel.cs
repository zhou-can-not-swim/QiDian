using QiDian.Data;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using QiDian.Common;

namespace QiDian.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;

        private string _welcomeText ="";
        public string WelcomeText
        {
            get => _welcomeText;
            set => this.RaiseAndSetIfChanged(ref _welcomeText, value);
        }

        [Reactive]
        public string DbStatus { get; set; }

        public HomeViewModel(AppDbContext db)
        {
            _db = db;
        }

        public override async Task OnNavigatedToAsync()
        {
            var count = await _db.NavigationMenus.CountAsync();
            DbStatus = $"数据库已就绪，共 {count} 条菜单记录";
        }
    }
}