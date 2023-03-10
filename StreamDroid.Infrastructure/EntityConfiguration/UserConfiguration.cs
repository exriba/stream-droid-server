using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamDroid.Core.Entities;
using StreamDroid.Core.Enums;

namespace StreamDroid.Infrastructure.EntityConfiguration
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(128)
                   .IsRequired();
            builder.Property(x => x.UserKey)
                   .IsRequired();
            builder.Property(x => x.AccessToken)
                   .IsRequired();
            builder.Property(x => x.RefreshToken)
                   .IsRequired();
            builder.Property(x => x.UserType)
                   .HasConversion(x => x.Value, 
                                  x => UserType.FromValue(x)) 
                   .IsRequired();

            builder.OwnsOne(x => x.Preferences, navigationBuilder =>
            {
                navigationBuilder.Property(x => x.DefaultVolume);
            });
        }
    }
}
