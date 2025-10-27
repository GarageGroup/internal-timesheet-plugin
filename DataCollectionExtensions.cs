using Microsoft.Xrm.Sdk;

namespace GarageGroup.Internal.Timesheet.Plugin
{
    internal static class DataCollectionExtensions
    {
        public static T GetDataOrNull<T>(this DataCollection<string, T> collection, string key)
            where T : class
        {
            if (collection.TryGetValue(key, out var data))
            {
                return data;
            }

            return null;
        }
    }
}