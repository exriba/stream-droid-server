using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamDroid.Core.Entities;
using StreamDroid.Core.ValueObjects;

namespace StreamDroid.Infrastructure.EntityConfiguration
{
    internal class RewardConfiguration : IEntityTypeConfiguration<Reward>
    {
        private const string ID = "Id";
        private const string REWARD_ID = "RewardId";

        public void Configure(EntityTypeBuilder<Reward> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Title)
                   .IsRequired();
            builder.Property(x => x.Prompt)
                   .IsRequired();
            builder.Property(x => x.StreamerId)
                   .IsRequired();
            builder.Property(x => x.BackgroundColor)
                   .IsRequired();
            builder.HasMany(x => x.Redemptions)
                   .WithOne(x => x.Reward)
                   .IsRequired();

            builder.OwnsOne(x => x.Speech, navigationBuilder =>
            {
                navigationBuilder.Property(x => x.Enabled)
                                 .IsRequired();
                navigationBuilder.Property(x => x.VoiceIndex)
                                 .IsRequired();  
            });

            builder.OwnsMany(x => x.Assets, navigationBuilder =>
            {
                navigationBuilder.WithOwner().HasForeignKey(REWARD_ID);
                navigationBuilder.Property<int>(ID).ValueGeneratedOnAdd();
                navigationBuilder.HasKey(ID);
                navigationBuilder.Property(x => x.Volume)
                                 .IsRequired();
                navigationBuilder.Property(x => x.FileName)
                                 .HasConversion(x => x.ToString(), 
                                                x => FileName.FromString(x))   
                                 .IsRequired();
            });
        }
    }
}
