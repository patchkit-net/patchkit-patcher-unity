using UniRx;

namespace PatchKit.Unity.Utilities
{
    public static class ObservableExtensions
    {
        public static IObservable<TR> SelectOrNull<T, TR>(this IObservable<T> source, System.Func<T, TR> selector)
        {
            return SelectOrDefault(source, selector, default(TR));
        }

        public static IObservable<TR> SelectSwitchOrNull<T, TR>(this IObservable<T> source,
            System.Func<T, IObservable<TR>> selector) where T : class
        {
            return SelectSwitchOrDefault(source, selector, default(TR));
        }

        public static IObservable<TR> SelectOrDefault<T, TR>(this IObservable<T> source,
            System.Func<T, TR> selector, TR defaultValue)
        {
            return source.Select(t => t == null ? defaultValue : selector(t));
        }

        public static IObservable<TR> SelectSwitchOrDefault<T, TR>(this IObservable<T> source,
            System.Func<T, IObservable<TR>> selector, TR defaultValue) where T : class
        {
            return source.Select(t => t == null ? Observable.Return(defaultValue) : selector(t)).Switch();
        }

        public static IObservable<string> SelectFormat<T>(this IObservable<T> source, string format)
        {
            return source.Select(v => string.Format(format, v));
        }

        public static IObservable<T> WhereNotNull<T>(this IObservable<T> source)
        {
            return source.Where(t => t != null);
        }
    }
}