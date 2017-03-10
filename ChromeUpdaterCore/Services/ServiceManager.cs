using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeUpdater.Services
{
    public class ServiceManager
    {
        public static readonly ServiceManager Instance = new ServiceManager();

        private ServiceManager()
        {
            _serviceMap = new Dictionary<Type, object>();
            _serviceMapLock = new object();
        }

        public void AddService<TServiceContract>(TServiceContract implementation)
            where TServiceContract : class
        {
            lock (_serviceMapLock)
            {
                _serviceMap[typeof(TServiceContract)] = implementation;
            }
        }

        public TServiceContract GetService<TServiceContract>()
            where TServiceContract : class
        {
            object service;
            lock (_serviceMapLock)
            {
                _serviceMap.TryGetValue(typeof(TServiceContract), out service);
            }
            return service as TServiceContract;
        }

        public bool IsServiceExists<TServiceContract>()
            where TServiceContract : class
        {
            lock (_serviceMapLock)
            {
                return _serviceMap.ContainsKey(typeof(TServiceContract));
            }
        }

        readonly Dictionary<Type, object> _serviceMap;
        readonly object _serviceMapLock;
    }
}
