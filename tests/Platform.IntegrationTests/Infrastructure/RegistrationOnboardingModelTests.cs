using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Platform.Domain.Auth;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class RegistrationOnboardingModelTests
{
    [Fact]
    public void Registration_intent_is_mapped_as_pre_tenant_platform_bootstrap_table()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=test;Password=test")
            .Options;
        using var db = new ApplicationDbContext(options);

        var designModel = db.GetService<IDesignTimeModel>().Model;
        var entity = designModel.FindEntityType(typeof(RegistrationIntent));

        Assert.NotNull(entity);
        Assert.Equal("registration_intent", entity.GetTableName());
        Assert.NotNull(entity.FindProperty(nameof(RegistrationIntent.RegistrationTokenHash)));
        Assert.NotNull(entity.FindProperty(nameof(RegistrationIntent.ConsumedTenantId)));
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Any(property => property.Name == nameof(RegistrationIntent.RegistrationTokenHash)));
        var checkConstraintNames = entity.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToArray();
        Assert.Contains("ck_registration_intent_status", checkConstraintNames);
        Assert.Contains("ck_registration_intent_expiry", checkConstraintNames);
        Assert.Contains("ck_registration_intent_consumed_shape", checkConstraintNames);
    }
}