using System;

namespace KustoTest2.DocDb
{
    using Models;

    public class DocDbSettings
    {
        public string Account { get; set; }
        public string Db { get; set; }
        public string Collection { get; set; }
        public string AuthKeySecret { get; set; }
        public bool CollectMetrics { get; set; }
        public Uri AccountUri => new Uri($"https://{Account}.documents.azure.com:443/");
    }

    public class DocDbData
    {
        [ModelBind(typeof(DeviceRelation))]
        public DocDbSettings DeviceRelation { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ModelBindAttribute : Attribute
    {
        public Type ModelType { get; set; }

        public ModelBindAttribute(Type modelType)
        {
            ModelType = modelType;
        }
    }
}
