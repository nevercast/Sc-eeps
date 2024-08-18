using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bot;

/// <summary>
/// ServiceCollection registers and tracks types for the lifetime
/// of the bot. The bot gets reset occasionally and instances
/// will be lost, this service collection helps keep the latest
/// instances of services.
/// </summary>
public static class ServiceCollection
{
  private static readonly Dictionary<Type, object> InstantiatedServices = new();
  private static readonly Dictionary<Type, Func<object>> Factories = new();

  /// <summary>
  /// Registers a service with the service collection.
  /// </summary>
  /// <param name="factory">An optional factory method to create the service.</param>
  /// <typeparam name="T">The type of service to register.</typeparam>
  public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(Func<T>? factory = null) where T : class
  {
    if (Factories.ContainsKey(typeof(T)))
    {
      throw new InvalidOperationException($"Service of type {typeof(T).Name} is already registered.");
    }

    Factories.Add(typeof(T), factory ?? Activator.CreateInstance<T>);
  }

  /// <summary>
  /// Registers a service with the service collection using an interface and a concrete type.
  /// </summary>
  /// <typeparam name="TService">The interface or base type of the service.</typeparam>
  /// <typeparam name="TImplementation">The concrete type of the service.</typeparam>
  public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TService, TImplementation>()
    where TService : class
    where TImplementation : class, TService, new()
  {
    if (Factories.ContainsKey(typeof(TService)))
    {
      throw new InvalidOperationException($"Service of type {typeof(TService).Name} is already registered.");
    }

    Factories.Add(typeof(TService), Activator.CreateInstance<TImplementation>);
  }

  /// <summary>
  /// Gets a service from the service collection.
  /// </summary>
  /// <typeparam name="T">The type of service to get.</typeparam>
  /// <returns>The service instance.</returns>
  public static T Inject<T>()
  {
    if (InstantiatedServices.ContainsKey(typeof(T))) return (T)InstantiatedServices[typeof(T)];
    if (!Factories.ContainsKey(typeof(T)))
    {
      throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    InstantiatedServices.Add(typeof(T), Factories[typeof(T)]());
    return (T) InstantiatedServices[typeof(T)];
  }
}