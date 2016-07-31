using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.IoC
{
    public interface IContainer : IDisposable
    {
        //
        // Summary:
        //     The parent of this container.
        IContainer Parent { get; }
        //
        // Summary:
        //     Create a child container.
        //
        // Returns:
        //     The new child container.
        //
        // Remarks:
        //     A child container shares the parent's configuration, but can be configured with
        //     different settings or lifetime.
        IContainer CreateChildContainer();

        //
        // Summary:
        //     Register a type mapping with the container, where the created instances will
        //     use the given LifetimeManager.
        //
        // Parameters:
        //   from:
        //     System.Type that will be requested.
        //
        //   to:
        //     System.Type that will actually be returned.
        //
        //   name:
        //     Name to use for registration, null if a default registration.
        //
        //   lifetimeManager:
        //     The Microsoft.Practices.Unity.LifetimeManager that controls the lifetime of the
        //     returned instance.
        //
        //   injectionMembers:
        //     Injection configuration objects.
        //
        // Returns:
        //     The Container object that this method was called
        //     on (this in C#, Me in Visual Basic).
        IContainer RegisterType(Type from, Type to, string name, Lifetime lifetime, params Injection[] injections);
        IContainer RegisterType(Type from, Type to, Lifetime lifetime, params Injection[] injections);
        IContainer RegisterType(Type from, Type to, string name = null, params Injection[] injections);
        IContainer RegisterType<TFrom, TTo>(string name, Lifetime lifetime, params Injection[] injections) where TTo : TFrom;
        IContainer RegisterType<TFrom, TTo>(params Injection[] injections) where TTo : TFrom;
        IContainer RegisterType<TFrom, TTo>(string name, params Injection[] injections) where TTo : TFrom;
        IContainer RegisterType<TFrom, TTo>(Lifetime lifetime, params Injection[] injections) where TTo : TFrom;

        //
        // Summary:
        //     Register an instance with the container.
        //
        // Parameters:
        //   t:
        //     Type of instance to register (may be an implemented interface instead of the
        //     full type).
        //
        //   instance:
        //     Object to returned.
        //
        //   name:
        //     Name for registration.
        //
        //   lifetime:
        //     LifetimeManager object that controls how this instance
        //     will be managed by the container.
        //
        // Returns:
        //     The Microsoft.Practices.Unity.UnityContainer object that this method was called
        //     on (this in C#, Me in Visual Basic).
        //
        // Remarks:
        //     Instance registration is much like setting a type as a singleton, except that
        //     instead of the container creating the instance the first time it is requested,
        //     the user creates the instance ahead of type and adds that instance to the container.
        IContainer RegisterInstance(Type t, string name, object instance, Lifetime lifetime = Lifetime.Singleton);
        IContainer RegisterInstance(Type t, object instance, Lifetime lifetime = Lifetime.Singleton);
        IContainer RegisterInstance<TInterface>(TInterface instance, Lifetime lifetime = Lifetime.Singleton);
        IContainer RegisterInstance<TInterface>(string name, TInterface instance, Lifetime lifetime = Lifetime.Singleton);

        //
        // Summary:
        //     Resolve an instance of the requested type with the given name from the container.
        //
        // Parameters:
        //   t:
        //     System.Type of object to get from the container.
        //
        //   name:
        //     Name of the object to retrieve.
        //
        //   resolverOverrides:
        //     Any overrides for the resolve call.
        //
        // Returns:
        //     The retrieved object.
        object Resolve(Type t, string name, params Parameter[] parameters);
        object Resolve(Type t, params Parameter[] parameters);
        T Resolve<T>(params Parameter[] overrides);
        T Resolve<T>(string name, params Parameter[] overrides);
        //
        // Summary:
        //     Return instances of all registered types requested.
        //
        // Parameters:
        //   container:
        //     Container to resolve from.
        //
        //   resolverOverrides:
        //     Any overrides for the resolve calls.
        //
        // Type parameters:
        //   T:
        //     The type requested.
        //
        // Returns:
        //     Set of objects of type T.
        //
        // Remarks:
        //     This method is useful if you've registered multiple types with the same System.Type
        //     but different names.
        //     Be aware that this method does NOT return an instance for the default (unnamed)
        //     registration.
        IEnumerable<object> ResolveAll(Type type, params Parameter[] parameters);
        IEnumerable<T> ResolveAll<T>(params Parameter[] parameters);
    }
}
