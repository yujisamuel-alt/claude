using System;
using System.Collections.Generic;

namespace Enxada.Core
{
    /// <summary>
    /// Registro central de serviços. O GameBootstrap registra tudo na cena Boot;
    /// os sistemas pegam o que precisam em vez de usar singletons espalhados.
    /// Lógica pura (sem UnityEngine) para poder ser testada fora da Unity.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (Services.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"Service {typeof(T).Name} is already registered.");

            Services[typeof(T)] = service;
        }

        public static bool Unregister<T>() where T : class => Services.Remove(typeof(T));

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service))
                return service;
            throw new InvalidOperationException(
                $"Service {typeof(T).Name} is not registered. Did the game start from the Boot scene?");
        }

        public static void Clear() => Services.Clear();
    }
}
