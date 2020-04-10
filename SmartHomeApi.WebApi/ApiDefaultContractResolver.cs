using System;
using Newtonsoft.Json.Serialization;

namespace SmartHomeApi.WebApi
{
    public class ApiDefaultContractResolver : DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type)
        {
            //It was intended not to cache plugin types in Newtonsoft (because if plugin type is cached then plugin can't be unloaded)
            //but it does not work for now because type still is cached in Newtonsoft.Json.Serialization.CachedAttributeGetter<T> 
            //and there is no possibility to disable it. So leave this ApiDefaultContractResolver here for best times  
            //when maybe it will be possible. Now these goal is obtained via hack. Look at ApiController.
            /*if (type.IsSubclassOf(typeof(ExecuteCommandResultAbstract)))
            {
                return base.CreateContract(type);
            }*/

            return base.ResolveContract(type);
        }
    }
}