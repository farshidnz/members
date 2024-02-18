using Microsoft.EntityFrameworkCore;
using SettingsAPI.EF;

namespace SettingsAPI.Data
{
    public partial class ShopGoContext : DbContext
    {
        public ShopGoContext(DbContextOptions<ShopGoContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Member> Member { get; set; }

        public virtual DbSet<MemberBalanceView> MemberBalanceView { get; set; }

        public virtual DbSet<MerchantTier> MerchantTier { get; set; }

        public virtual DbSet<Merchant> Merchant { get; set; }

        public virtual DbSet<TransactionTier> TransactionTier { get; set; }

        public virtual DbSet<Transaction> Transaction { get; set; }

        public virtual DbSet<TransactionView> TransactionView { get; set; }

        public virtual DbSet<Community> Community { get; set; }

        public virtual DbSet<CommunityMemberMap> CommunityMemberMap { get; set; }

        public virtual DbSet<MemberBankAccount> MemberBankAccount { get; set; }

        public virtual DbSet<MemberPaymentMethodHistory> MemberPaymentMethodHistory { get; set; }

        public virtual DbSet<MemberClicks> MemberClicks { get; set; }

        public virtual DbSet<MemberPaypalAccount> MemberPaypalAccount { get; set; }

        public virtual DbSet<MemberRedeem> MemberRedeem { get; set; }

        public virtual DbSet<CognitoMember> CognitoMember { get; set; }

        public virtual DbSet<Person> Person { get; set; }

        public virtual DbSet<MemberFavourite> MemberFavourite { get; set; }

        public virtual DbSet<MemberFavouriteCategory> MemberFavouriteCategory { get; set; }

        public virtual DbSet<MerchantView> MerchantView { get; set; }

        public virtual DbSet<OfferView> OfferView { get; set; }

        public virtual DbSet<EntityAudit> EntityAudit { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>(entity =>
            {
                entity.Property(e => e.AccessCode).HasMaxLength(200);

                entity.Property(e => e.ActivateBy).HasColumnType("datetime");

                entity.Property(e => e.AutoCreated).HasDefaultValueSql("((0))");

                entity.Property(e => e.ClickWindowActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CommunicationsEmail).HasMaxLength(200);

                entity.Property(e => e.CookieIpAddress).HasMaxLength(200);

                entity.Property(e => e.DateDeletedByMember).HasColumnType("datetime");

                entity.Property(e => e.DateJoined)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateReceiveNewsLetter).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.FacebookUsername).HasMaxLength(500);

                entity.Property(e => e.FirstName).HasMaxLength(50);

                entity.Property(e => e.HashedEmail).HasMaxLength(500);

                entity.Property(e => e.HashedMemberNewId).HasMaxLength(500);

                entity.Property(e => e.HashedMobile).HasMaxLength(500);

                entity.Property(e => e.LastLogon).HasColumnType("datetime");

                entity.Property(e => e.LastName).HasMaxLength(50);

                entity.Property(e => e.MailChimpListEmailId)
                    .HasColumnName("MailChimpListEmailID")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.MemberNewId).HasDefaultValueSql("(newid())");

                entity.Property(e => e.Mobile).HasMaxLength(50);

                entity.Property(e => e.MobileSha256)
                    .HasColumnName("MobileSHA256")
                    .HasMaxLength(200);

                entity.Property(e => e.PaypalEmail).HasMaxLength(200);

                entity.Property(e => e.PopUpActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.PostCode).HasMaxLength(50);

                entity.Property(e => e.ReceiveNewsLetter)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RequiredLogin)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RiskDescription)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RowVersion)
                    .IsRequired()
                    .IsRowVersion()
                    .IsConcurrencyToken();

                entity.Property(e => e.SaltKey)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.SysEndTime)
                    .HasDefaultValueSql("(CONVERT([datetime2],'9999-12-31 23:59:59.9999999'))");

                entity.Property(e => e.SysStartTime).HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.TwoFactorAuthActivateBy).HasColumnType("datetime");

                entity.Property(e => e.TwoFactorAuthActivationCountryCode).HasMaxLength(50);

                entity.Property(e => e.TwoFactorAuthActivationMobile).HasMaxLength(50);

                entity.Property(e => e.TwoFactorAuthActivationToken).HasMaxLength(50);

                entity.Property(e => e.TwoFactorAuthyId).HasMaxLength(50);
               
            }).Entity<MemberBalanceView>(entity =>
            {
                entity.Property(e => e.TotalBalance).HasMaxLength(8);
                entity.Property(e => e.LifetimeRewards).HasMaxLength(8);
                entity.Property(e => e.AvailableBalance).HasMaxLength(8);
                entity.Property(e => e.RedeemBalance).HasMaxLength(8);
            }).Entity<MemberBankAccount>(entity =>
            {
                entity.Property(t => t.BankAccountId)
                    .ValueGeneratedOnAdd();
            }).Entity<MerchantView>(entity => entity.HasNoKey())
            .Entity<OfferView>(entity => 
            {
                entity.HasNoKey();
            
            })
            .Entity<CognitoMember>(entity =>
            {
                entity.HasOne<Member>().WithOne().HasForeignKey<Member>(x=>x.MemberId);                
                entity.HasOne<Person>(sc => sc.Person)
                   .WithMany(s => s.CognitoMember)
                   .HasForeignKey(sc => sc.PersonId);
            })
            .Entity<Person>(entity =>
            {
                entity.Property(e => e.PersonId);
                entity.Property(e => e.PremiumStatus);

                entity.HasMany<CognitoMember>(sc => sc.CognitoMember)
                   .WithOne(s => s.Person)
                   .HasForeignKey(sc => sc.PersonId);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}