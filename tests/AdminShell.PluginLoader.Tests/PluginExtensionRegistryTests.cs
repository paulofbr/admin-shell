using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.PluginSystem;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AdminShell.PluginLoader.Tests;

public class PluginExtensionRegistryTests
{
    [Fact]
    public void GetExtensionFieldsForEntity_ReturnsOrderedFields_ForEntityAndPluralTableName()
    {
        var registry = new PluginExtensionRegistry(
            new IPluginComponent[]
            {
                new UserExtensionFieldPlugin(),
                new RoleExtensionFieldPlugin(),
            },
            NullLogger<PluginExtensionRegistry>.Instance);

        var userFields = registry.GetExtensionFieldsForEntity("User");

        userFields.Select(field => field.Name).Should().Equal("Department", "Manager");
        userFields.Select(field => field.ColumnName).Should().Contain("CDU_Manager");
        userFields.Should().OnlyContain(field => field.EntityName == "User");
    }

    [Fact]
    public void GetExtensionFieldsForEntity_MatchesPluralEntityName_ToSingularDefinition()
    {
        var registry = new PluginExtensionRegistry(
            new IPluginComponent[] { new UserExtensionFieldPlugin() },
            NullLogger<PluginExtensionRegistry>.Instance);

        var fields = registry.GetExtensionFieldsForEntity("Users");

        fields.Should().HaveCount(2);
        fields.Select(field => field.Name).Should().Equal("Department", "Manager");
    }

    [Fact]
    public void Refresh_WhenPluginLoaderProvided_ExcludesDisabledPluginComponents()
    {
        var activeComponent = new ActiveUserExtensionFieldPlugin();
        var disabledComponent = new DisabledUserExtensionFieldPlugin();
        var loader = Substitute.For<IPluginLoader>();

        loader.LoadedPlugins.Returns(new[]
        {
            new PluginDescriptor { Id = "active", Status = PluginStatus.Active },
            new PluginDescriptor { Id = "disabled", Status = PluginStatus.Disabled },
        });
        loader.GetPluginComponents().Returns(new IPluginComponent[]
        {
            activeComponent,
            disabledComponent,
        });

        var registry = new PluginExtensionRegistry(
            Array.Empty<IPluginComponent>(),
            NullLogger<PluginExtensionRegistry>.Instance,
            loader);

        registry.GetExtensionFields().Should().ContainSingle();
        registry.GetExtensionFields().Single().Name.Should().Be("ActiveDepartment");
    }

    [PluginComponent("user-department")]
    private sealed class UserExtensionFieldPlugin : IExtensionFieldPlugin
    {
        public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
        {
            yield return new EntityExtensionFieldDefinition(
                "User",
                "Manager",
                EntityExtensionFieldType.String,
                Required: false,
                Label: "Manager",
                Order: 200);

            yield return new EntityExtensionFieldDefinition(
                "User",
                "Department",
                EntityExtensionFieldType.String,
                Required: true,
                Label: "Department",
                Order: 100);
        }
    }

    [PluginComponent("role-audit")]
    private sealed class RoleExtensionFieldPlugin : IExtensionFieldPlugin
    {
        public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
            => [new EntityExtensionFieldDefinition("Role", "ApprovalStatus", EntityExtensionFieldType.String)];
    }

    [PluginComponent("active")]
    private sealed class ActiveUserExtensionFieldPlugin : IExtensionFieldPlugin
    {
        public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
            => [new EntityExtensionFieldDefinition("User", "ActiveDepartment", EntityExtensionFieldType.String)];
    }

    [PluginComponent("disabled")]
    private sealed class DisabledUserExtensionFieldPlugin : IExtensionFieldPlugin
    {
        public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
            => [new EntityExtensionFieldDefinition("User", "DisabledDepartment", EntityExtensionFieldType.String)];
    }
}
