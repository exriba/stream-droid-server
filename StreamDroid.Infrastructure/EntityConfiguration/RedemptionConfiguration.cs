using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamDroid.Core.Entities;

namespace StreamDroid.Infrastructure.EntityConfiguration
{
    /// <summary>
    /// Defines <see cref="Redemption"/> table configuration.
    /// </summary>
    internal class RedemptionConfiguration : IEntityTypeConfiguration<Redemption>
    {
        public void Configure(EntityTypeBuilder<Redemption> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .IsRequired();
            builder.Property(x => x.UserName) 
                   .IsRequired();
            builder.Property(x => x.DateTime);

            builder.HasIndex(x => x.DateTime);
        }
    }
}
