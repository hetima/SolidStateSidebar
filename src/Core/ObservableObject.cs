using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SSS.Core
{
    /// <summary>
    /// INotifyPropertyChanged の共通実装を提供する基底クラス。
    /// 既存コードが nameof(X) を明示する呼び方と、[CallerMemberName] による
    /// 引数なし呼び出しの両方をサポートする。
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// プロパティ変更を通知する。propertyName を省略すると呼び出し元のメンバー名が使われる。
        /// </summary>
        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 値が変化したときだけフィールドを更新し変更通知を行う。変化があれば true を返す。
        /// 副作用を伴う setter には使わず、単純な転送プロパティにのみ使うこと。
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
    }
}
