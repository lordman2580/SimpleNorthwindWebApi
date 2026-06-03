using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace SimpleNorthwind.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly Assembly DomainAssembly =
        typeof(SimpleNorthwind.Domain.Common.Result).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(SimpleNorthwind.Application.DependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_OtherLayers()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SimpleNorthwind.Application",
                "SimpleNorthwind.Infrastructure",
                "SimpleNorthwind.WebApi")
            .GetResult();

        var failingTypes = result.FailingTypeNames is not null
            ? string.Join(", ", result.FailingTypeNames)
            : "(none)";

        result.IsSuccessful.ShouldBeTrue(
            $"Domain layer must not depend on Application, Infrastructure, or WebApi. " +
            $"Failing types: {failingTypes}");
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrWebApi()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SimpleNorthwind.Infrastructure",
                "SimpleNorthwind.WebApi")
            .GetResult();

        var failingTypes = result.FailingTypeNames is not null
            ? string.Join(", ", result.FailingTypeNames)
            : "(none)";

        result.IsSuccessful.ShouldBeTrue(
            $"Application layer must not depend on Infrastructure or WebApi. " +
            $"Failing types: {failingTypes}");
    }
}
