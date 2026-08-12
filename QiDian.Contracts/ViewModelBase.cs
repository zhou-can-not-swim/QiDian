using ReactiveUI;

namespace QiDian.Contracts
{
    public abstract class ViewModelBase : ReactiveObject
    {
        /// <summary>
        /// 导航到此页时调用（可在此加载数据）
        /// </summary>
        public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

        /// <summary>
        /// 离开此页时调用（可在此保存状态）
        /// </summary>
        public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}
