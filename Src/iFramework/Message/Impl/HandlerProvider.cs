using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using IFramework.Infrastructure;
using IFramework.Config;
using IFramework.Infrastructure.Unity.LifetimeManagers;

namespace IFramework.Message.Impl
{
    public abstract class HandlerProvider<IHandler> : IHandlerProvider where IHandler : class
    {
        //protected abstract Type HandlerType { get; }

        private string[] Assemblies { get; set; }
        private readonly Dictionary<Type, List<Type>> _HandlerTypes;
        private readonly HashSet<Type> discardKeyTypes;
        private readonly Dictionary<Type, ParameterInfo[]> _HandlerConstuctParametersInfo;

        public HandlerProvider(params string[] assemblies)
        {
            Assemblies = assemblies;
            _HandlerTypes = new Dictionary<Type, List<Type>>();
            _HandlerConstuctParametersInfo = new Dictionary<Type, ParameterInfo[]>();
            discardKeyTypes = new HashSet<Type>();

            RegisterHandlers();
        }

        public void ClearRegistration()
        {
            _HandlerTypes.Clear();
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
                //var messageHandlerWrapperType = typeof(MessageHandler<>).MakeGenericType(messageType);
                //var messageHandler = Activator.CreateInstance(handlerType);
                //var messageHandlerWrapper = Activator.CreateInstance(messageHandlerWrapperType, new object[] { messageHandler }) as IMessageHandler;
                Register(messageType, handlerType);
            }
        }
        #endregion

        public void Register(Type messageType, Type handlerType)
        {
            if (_HandlerTypes.ContainsKey(messageType))
            {
                var registeredDispatcherHandlerTypes = _HandlerTypes[messageType];
                if (registeredDispatcherHandlerTypes != null)
                {
                    if (!registeredDispatcherHandlerTypes.Contains(handlerType))
                        registeredDispatcherHandlerTypes.Add(handlerType);
                }
                else
                {
                    registeredDispatcherHandlerTypes = new List<Type>();
                    _HandlerTypes[messageType] = registeredDispatcherHandlerTypes;
                    registeredDispatcherHandlerTypes.Add(handlerType);
                }
            }
            else
            {
                var registeredDispatcherHandlerTypes = new List<Type>();
                registeredDispatcherHandlerTypes.Add(handlerType);
                _HandlerTypes.Add(messageType, registeredDispatcherHandlerTypes);
            }
            var parameterInfoes = handlerType.GetConstructors()
                                               .OrderByDescending(c => c.GetParameters().Length)
                                               .FirstOrDefault().GetParameters();
            _HandlerConstuctParametersInfo[handlerType] = parameterInfoes;
        }

        protected Type GetHandlerType(Type messageType)
        {
            return GetHandlerTypes(messageType).FirstOrDefault();
        }

        protected IList<Type> GetHandlerTypes(Type messageType)
        {
            var avaliableHandlerTypes = new List<Type>();
            if (_HandlerTypes.ContainsKey(messageType))
            {
                var handlerTypes = _HandlerTypes[messageType];
                if (handlerTypes != null && handlerTypes.Count > 0)
                {
                    avaliableHandlerTypes.AddRange(handlerTypes);
                }
            }
            else if (!discardKeyTypes.Contains(messageType))
            {
                bool isDiscardKeyTypes = true;
                foreach (var handlerTypes in _HandlerTypes)
                {
                    if (messageType.IsSubclassOf(handlerTypes.Key))
                    {
                        var messageDispatcherHandlerTypes = _HandlerTypes[handlerTypes.Key];
                        if (messageDispatcherHandlerTypes != null && messageDispatcherHandlerTypes.Count > 0)
                        {
                            avaliableHandlerTypes.AddRange(messageDispatcherHandlerTypes);
                            isDiscardKeyTypes = false;
                            _HandlerTypes.Add(messageType, messageDispatcherHandlerTypes);
                            break;
                        }
                    }
                }
                if (isDiscardKeyTypes)
                {
                    discardKeyTypes.Add(messageType);
                }
            }
            return avaliableHandlerTypes;
        }

        public object GetHandler(Type messageType)
        {
            object handler = null;
            var handlerType = GetHandlerType(messageType);
            if (handlerType != null)
            {
                var parameterInfoes = _HandlerConstuctParametersInfo[handlerType];
                List<object> parameters = new List<object>();
                parameterInfoes.ForEach(parameterInfo => {
                    object parameter = null;
                    if (parameterInfo.ParameterType == typeof(IMessageContext))
                    {
                        parameter = PerMessageContextLifetimeManager.CurrentMessageContext;
                    }
                    else
                    {
                        parameter = IoCFactory.Resolve(parameterInfo.ParameterType);
                    }
                    parameters.Add(parameter);
                });
                handler = Activator.CreateInstance(handlerType, parameters.ToArray());
            }
            return handler;
        }

        public IList<object> GetHandlers(Type messageType)
        {
            var handlerTypes = GetHandlerTypes(messageType);
            var handlers = new List<object>();
            handlerTypes.ForEach(handlerType => handlers.Add(IoCFactory.Resolve(handlerType)));
            return handlers;
        }
    }
}
