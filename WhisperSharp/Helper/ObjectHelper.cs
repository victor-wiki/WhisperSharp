using Newtonsoft.Json;

namespace WhisperSharp.Helper
{
    public class ObjectHelper
    {
        public static T CloneObject<T>(object obj)
        {
            return (T)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj), typeof(T));
        }

        public static object GetValue(object obj, string propertyName)
        {
            if (obj == null || propertyName == null)
            {
                return null;
            }

            var property = obj.GetType().GetProperties().FirstOrDefault(item => item.Name == propertyName);

            if (property != null)
            {
                return property.GetValue(obj);
            }

            return null;
        }

        public static void SetValue(object obj, string propertyName, object value)
        {
            if (obj == null || propertyName == null)
            {
                return;
            }

            var property = obj.GetType().GetProperties().FirstOrDefault(item => item.Name == propertyName);

            if (property != null)
            {
                property.SetValue(obj, value, null);
            }
        }

        public static void CopyProperties(object source, object target, bool excluedNullValue = false, List<string> excludePropertyNames = null)
        {
            var sourceProps = source.GetType().GetProperties().Where(x => x.CanRead).ToList();
            var targetProps = target.GetType().GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (excludePropertyNames != null && excludePropertyNames.Contains(sourceProp.Name))
                {
                    continue;
                }

                if (targetProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = targetProps.FirstOrDefault(x => x.Name == sourceProp.Name);

                    if (p != null && p.CanWrite)
                    {
                        var value = sourceProp.GetValue(source, null);

                        if (excluedNullValue && value == null)
                        {
                            continue;
                        }

                        p.SetValue(target, value, null);
                    }
                }
            }
        }
    
        public static void CopyDictionaryToObject<T>(Dictionary<string,dynamic> source, T target, bool excluedNullValue = false, List<string> excludePropertyNames = null)
        {
            T restult = (T)JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));

            ObjectHelper.CopyProperties(restult, target, excluedNullValue, excludePropertyNames);
        }
    }
}
