using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using IFramework.Infrastructure;
using IFramework.Config;

namespace IFramework.Message.Impl
{
    public abstract class HandlerProvider<IHandler> : IHandlerProvider where IHandler : class
    {
        //protected abstract Type HandlerType { get; }

        private string[] Assemblies { get; set; }
        private readonly Dictionary<Type, List<IMessageHandler>> _Handlers;
        private readonly HashSet<Type> discardKeyTypes;
        public HandlerProvider(params string[] assemblies)
        {
            Assemblies = assemblies;
            _Handlers = new Dictionary<Type, List<IMessageHandler>>();
            discardKeyTypes = new HashSet<Type>();

            RegisterHandlers();
        }

        public void ClearRegistration()
        {
            _Handlers.Clear();
        }

        void RegisterHandlers()
        {
            var handlerElements = ConfigurationReader.Instance
                                                     .GetConfigurationSection<FrameworkConfigurationSection>()
                                                     .Handlers;
            if (handlerElements != null)
            {
                foreach (HandlerElement handlerElement in handlerElements)
                {
                    if (Assemblies == null || Assemblies.Contains(handlerElement.Name))
                    {
                        try
                        {
                            switch (handlerElement.SourceType)
                            {
                                case HandlerSourceType.Type:
                                    Type type = Type.GetType(handlerElement.Source);
                                    RegisterHandlerFromType(type);
                                    break;
                                case HandlerSourceType.Assembly:
                                    Assembly assembly = Assembly.Load(handlerElement.Source);
                                    RegisterHandlerFromAssembly(assembly);
                                    break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
        }

        #region Private Methods
        private void RegisterHandlerFromAssembly(Assembly assembly)
        {
            var exportedTypes = assembly.GetExportedTypes()
                                        .Where(x => x.IsInterface == false && x.IsAbstract == false
                                                && x.GetInterfaces()
                                                    .Any(y => y.IsGenericType 
                                                        && y.GetGenericTypeDefinition() == typeof(IHandler).GetGenericTypeDefinition()));
            foreach (var type in exportedTypes)
            {
                RegisterHandlerFromType(type);
            }
        }

        protected void RegisterHandlerFromType(Type handlerType)
        {
            var ihandlerTypes = handlerType.GetInterfaces().Where(x => x.IsGenericType 
                                                                    && x.GetGenericTypeDefinition() == typeof(IHandler).GetGenericTypeDefinition());
            foreach (var ihandlerType in ihandlerTypes)
            {
                var messageType = ihandlerType.GetGenericArguments().Single();
                var messageHandlerWrapperType = typeof(MessageHandler<>).MakeGenericType(messageType);
                var messageHandler = Activator.CreateInstance(handlerType);
                var messageHandlerWrapper = Activator.CreateInstance(messageHandlerWrapperType, new object[] { messageHandler }) as IMessageHandler;
                Register(messageType, messageHandlerWrapper);
            }
        }
        #endregion

        public void Register(Type messageType, IMessageHandler handlerInstance)
        {
            if (_Handlers.ContainsKey(messageType))
            {
                List<IMessageHandler> registeredDispatcherHandlers = _Handlers[messageType];
                if (registeredDispatcherHandlers != null)
                {
                    if (!registeredDispatcherHandlers.Contains(handlerInstance))
                        registeredDispatcherHandlers.Add(handlerInstance);
                }
                else
                {
                    registeredDispatcherHandlers = new List<IMessageHandler>();
                    _Handlers[messageType] = registeredDispatcherHandlers;
                    registeredDispatcherHandlers.Add(handlerInstance);
                }
            }
            else
            {
                var registeredDispatcherHandlers = new List<IMessageHandler>();
                registeredDispatcherHandlers.Add(handlerInstance);
                _Handlers.Add(messageType, registeredDispatcherHandlers);
            }
        }






        public IMessageHandler GetHandler(Type messageType)
        {
            return GetHandlers(messageType).FirstOrDefault();
        }

        public IList<IMessageHandler> GetHandlers(Type messageType)
        {
            var handlers = new List<IMessageHandler>();
            if (_Handlers.ContainsKey(messageType))
            {
                var handlerWrappers = _Handlers[messageType];
                if (handlerWrappers != null && handlerWrappers.Count > 0)
                {
                    handlers.AddRange(handlerWrappers);

                }
            }
            else if (!discardKeyTypes.Contains(messageType))
            {
                bool isDiscardKeyTypes = true;
                foreach (var handlerWrappers in _Handlers)
                {
                    if (messageType.IsSubclassOf(handlerWrappers.Key))
                    {
                        var messageDispatcherHandlers = _Handlers[handlerWrappers.Key];
                        if (messageDispatcherHandlers != null && messageDispatcherHandlers.Count > 0)
                        {
                            handlers.AddRange(messageDispatcherHandlers);
                            isDiscardKeyTypes = false;
                            _Handlers.Add(messageType, messageDispatcherHandlers);
                            break;
                        }
                    }
                }
                if (isDiscardKeyTypes)
                {
                    discardKeyTypes.Add(messageType);
                }
            }
            return handlers;
        }
    }
}
