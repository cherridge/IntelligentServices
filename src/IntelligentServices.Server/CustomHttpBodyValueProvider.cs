using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace IntelligentServices.Server
{
    public class CustomHttpBodyValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
    {
        private readonly Newtonsoft.Json.Linq.JObject JObject;
        private PrefixContainer _prefixContainer;
        private readonly CultureInfo _culture;

        public CustomHttpBodyValueProvider(
            BindingSource bindingSource,
            Newtonsoft.Json.Linq.JObject
            jObject,
            CultureInfo culture) : base(bindingSource)
        {
            JObject = jObject;
            _culture = culture;
        }

        protected PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    var propNames = JObject.Properties().Select(prop => prop.Name).ToArray();

                    _prefixContainer = new PrefixContainer(
                    propNames);
                }

                return _prefixContainer;
            }
        }

        public override bool ContainsPrefix(string prefix)
        {
            return PrefixContainer.ContainsPrefix(prefix.ToLower());
        }

        public IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        public override ValueProviderResult GetValue(string key)
        {
            object rawValue = JObject.Property(key).First();
            var sv = new StringValues(rawValue.ToString());
            return new ValueProviderResult(sv, _culture);
        }

    }
}
