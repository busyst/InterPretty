public class CollectionUtils
{
    public static T[] CaptureUntil<T>(IEnumerable<T> ienum, int startIndex, Predicate<T> condition)
    {
        List<T> capturedList = new List<T>();
        var count = ienum.Count();
        for (int i = startIndex; i < count; i++)
        {
            if(condition(ienum.ElementAt(i)))
            {
                break;
            }

            capturedList.Add(ienum.ElementAt(i));
        }

        return [.. capturedList];
    }
}