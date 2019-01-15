using System;
using Autofac;
using JetBrains.Annotations;
using ContainerBuilder = Autofac.ContainerBuilder;

namespace PatchKit.Apps.Updating
{
public static class DependencyResolver
{
    private static IContainer _container;

    public static readonly ContainerBuilder ContainerBuilder =
        new ContainerBuilder();

    public static void Build()
    {
        if (_container != null)
        {
            throw new InvalidOperationException();
        }

        _container = ContainerBuilder.Build();
    }

    [NotNull]
    public static T Resolve<T>()
    {
        if (_container == null)
        {
            throw new InvalidOperationException();
        }

        return _container.Resolve<T>();
    }
}
}