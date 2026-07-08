using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Inventory module DbContext - Schema: inv
///
/// Master Data      : Units, Categories, Items, Warehouses, Bins
/// Item Detail      : Brand, Images, Barcodes, Prices, Attributes, Taxes, Comments
/// Item Extended    : Colors, Sizes, Variants, Shipping, SEO,
///                    ChannelListings, Suppliers, Bundles,
///                    Substitutions, Warranty, Discounts
/// Documents        : InventoryDocument, InventoryDocumentLine
/// Transactions     : InventoryTransaction (stock ledger)
/// Balances         : InventoryBalance (snapshot - variant-aware)
/// Valuation        : InventoryValuation, InventoryCostLayer (FIFO)
/// </summary>
public class InventoryDbContext : DbContext
{
    private const string DefaultSchema = "inventory";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    // Master Data
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<ItemCategory> ItemCategories { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<ItemUomConversion> ItemUomConversions { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Bin> Bins { get; set; } = null!;

    // Item Detail
    public DbSet<Brand> Brands { get; set; } = null!;
    public DbSet<ItemImage> ItemImages { get; set; } = null!;
    public DbSet<ItemBarcode> ItemBarcodes { get; set; } = null!;
    public DbSet<ItemPrice> ItemPrices { get; set; } = null!;
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; } = null!;
    public DbSet<ItemAttribute> ItemAttributes { get; set; } = null!;
    public DbSet<TaxDefinition> TaxDefinitions { get; set; } = null!;
    public DbSet<ItemTax> ItemTaxes { get; set; } = null!;
    public DbSet<ItemComment> ItemComments { get; set; } = null!;

    // Item Extended
    public DbSet<Color> Colors { get; set; } = null!;
    public DbSet<ItemColor> ItemColors { get; set; } = null!;
    public DbSet<Size> Sizes { get; set; } = null!;
    public DbSet<ItemSize> ItemSizes { get; set; } = null!;
    public DbSet<ItemVariant> ItemVariants { get; set; } = null!;
    public DbSet<ItemShipping> ItemShippings { get; set; } = null!;
    public DbSet<ItemSeo> ItemSeos { get; set; } = null!;
    public DbSet<ItemChannelListing> ItemChannelListings { get; set; } = null!;
    public DbSet<ItemSupplier> ItemSuppliers { get; set; } = null!;
    public DbSet<ItemBundle> ItemBundles { get; set; } = null!;
    public DbSet<ItemSubstitution> ItemSubstitutions { get; set; } = null!;
    public DbSet<ItemWarranty> ItemWarranties { get; set; } = null!;
    public DbSet<ItemDiscount> ItemDiscounts { get; set; } = null!;

    // Documents
    public DbSet<InventoryDocument> InventoryDocuments { get; set; } = null!;
    public DbSet<InventoryDocumentLine> InventoryDocumentLines { get; set; } = null!;

    // Transactions & Balances
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
    public DbSet<InventoryBalance> InventoryBalances { get; set; } = null!;

    // Unit-level tracking (Serial / Lot)
    public DbSet<ItemSerial> ItemSerials { get; set; } = null!;
    public DbSet<ItemSerialHistory> ItemSerialHistories { get; set; } = null!;
    public DbSet<ItemBatch> ItemBatches { get; set; } = null!;
    public DbSet<ItemLotStock> ItemLotStocks { get; set; } = null!;
    public DbSet<InventoryDocumentLineSerial> InventoryDocumentLineSerials { get; set; } = null!;

    // Valuation
    public DbSet<InventoryValuation> InventoryValuations { get; set; } = null!;
    public DbSet<InventoryCostLayer> InventoryCostLayers { get; set; } = null!;

    // Stored files (SQL-backed image storage)
    public DbSet<StoredFile> StoredFiles { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        ApplyUtcDateTimeConverters(modelBuilder);

        // Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(2000);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_Brand_Tenant_Code");
        });

        // Unit
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("Units", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasMany(e => e.ConversionsFrom)
                .WithOne(c => c.FromUnit).HasForeignKey(c => c.FromUnitId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.ConversionsTo)
                .WithOne(c => c.ToUnit).HasForeignKey(c => c.ToUnitId).OnDelete(DeleteBehavior.NoAction);
        });

        // Item Category
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.ToTable("ItemCategories", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.OnlineImageUrl).HasMaxLength(2000);
            entity.HasOne(e => e.ParentCategory)
                .WithMany(c => c.ChildCategories).HasForeignKey(e => e.ParentCategoryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Items)
                .WithOne(i => i.Category).HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // Item
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ItemType).HasMaxLength(50).HasDefaultValue("Inventory");
            entity.Property(e => e.Condition).HasMaxLength(50).HasDefaultValue("New");
            entity.Property(e => e.CostingMethod).HasMaxLength(50).HasDefaultValue("MovingAverage");
            entity.Property(e => e.TrackingType).HasMaxLength(50).HasDefaultValue("None");
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ReorderLevel).HasPrecision(18, 2);
            entity.Property(e => e.MaxStockLevel).HasPrecision(18, 2);
            entity.Property(e => e.EconomicOrderQuantity).HasPrecision(18, 2);

            entity.HasOne(e => e.BaseUnit)
                .WithMany(u => u.BaseUnitItems).HasForeignKey(e => e.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Items).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.Items).HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.DisplayColor)
                .WithMany(c => c.DisplayColorItems).HasForeignKey(e => e.DisplayColorId).OnDelete(DeleteBehavior.SetNull);

            // 1:1 owned detail entities
            entity.HasOne(e => e.Shipping).WithOne(s => s.Item)
                .HasForeignKey<ItemShipping>(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Seo).WithOne(s => s.Item)
                .HasForeignKey<ItemSeo>(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Warranty).WithOne(w => w.Item)
                .HasForeignKey<ItemWarranty>(w => w.ItemId).OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UomConversions).WithOne(c => c.Item).HasForeignKey(c => c.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Images).WithOne(i => i.Item).HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Barcodes).WithOne(b => b.Item).HasForeignKey(b => b.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Prices).WithOne(p => p.Item).HasForeignKey(p => p.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Attributes).WithOne(a => a.Item).HasForeignKey(a => a.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Taxes).WithOne(t => t.Item).HasForeignKey(t => t.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Comments).WithOne(c => c.Item).HasForeignKey(c => c.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Colors).WithOne(c => c.Item).HasForeignKey(c => c.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Sizes).WithOne(s => s.Item).HasForeignKey(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Variants).WithOne(v => v.Item).HasForeignKey(v => v.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ChannelListings).WithOne(l => l.Item).HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Suppliers).WithOne(s => s.Item).HasForeignKey(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Discounts).WithOne(d => d.Item).HasForeignKey(d => d.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.BundleComponents).WithOne(b => b.BundleItem).HasForeignKey(b => b.BundleItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.BundleParents).WithOne(b => b.ComponentItem).HasForeignKey(b => b.ComponentItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Substitutions).WithOne(s => s.Item).HasForeignKey(s => s.ItemId).OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_Item_Tenant_Code");
            entity.HasIndex(e => new { e.IsActive, e.IsPublished }).HasDatabaseName("IX_Item_Status");
            entity.HasIndex(e => e.ItemType).HasDatabaseName("IX_Item_Type");
        });

        // Stored File (SQL-backed image bytes)
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired().HasColumnType("varbinary(max)");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId })
                .HasDatabaseName("IX_StoredFile_Tenant");
        });

        // Item Image
        modelBuilder.Entity<ItemImage>(entity =>
        {
            entity.ToTable("ItemImages", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Resolution).IsRequired().HasMaxLength(50).HasDefaultValue("Original");
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.AltText).HasMaxLength(500);
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Images).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemId, e.IsPrimary })
                .HasFilter("[IsPrimary] = 1").IsUnique().HasDatabaseName("IX_ItemImage_Item_Primary");
            entity.HasIndex(e => new { e.ItemId, e.Resolution })
                .HasDatabaseName("IX_ItemImage_Item_Resolution");
        });

        // Item Barcode
        modelBuilder.Entity<ItemBarcode>(entity =>
        {
            entity.ToTable("ItemBarcodes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Barcode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BarcodeType).IsRequired().HasMaxLength(50).HasDefaultValue("EAN13");
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Barcodes).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Unit)
                .WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.SetNull);
            // Barcode must be unique within a tenant (company + branch + business unit)
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Barcode })
                .IsUnique().HasDatabaseName("IX_ItemBarcode_TenantBarcode");
            // One primary barcode per item
            entity.HasIndex(e => new { e.ItemId, e.IsPrimary })
                .HasFilter("[IsPrimary] = 1").IsUnique().HasDatabaseName("IX_ItemBarcode_Item_Primary");
        });

        // Item Price
        modelBuilder.Entity<ItemPrice>(entity =>
        {
            entity.ToTable("ItemPrices", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceList).IsRequired().HasMaxLength(50).HasDefaultValue("Default");
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10).HasDefaultValue("USD");

            // ?? entered prices
            entity.Property(e => e.SalePrice).HasPrecision(18, 4);
            entity.Property(e => e.MinSalePrice).HasPrecision(18, 4);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 4);

            // ?? computed sale fields
            entity.Property(e => e.SalePriceExcludingTax).HasPrecision(18, 4);
            entity.Property(e => e.SaleTaxAmount).HasPrecision(18, 4);
            entity.Property(e => e.SalePriceIncludingTax).HasPrecision(18, 4);

            // ?? computed purchase fields
            entity.Property(e => e.PurchasePriceExcludingTax).HasPrecision(18, 4);
            entity.Property(e => e.PurchaseTaxAmount).HasPrecision(18, 4);
            entity.Property(e => e.PurchasePriceIncludingTax).HasPrecision(18, 4);

            // ?? tax meta
            entity.Property(e => e.EffectiveTaxRate).HasPrecision(18, 4);

            entity.HasOne(e => e.Item).WithMany(i => i.Prices).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ItemId, e.UnitId, e.PriceList, e.CurrencyCode })
                .IsUnique().HasDatabaseName("IX_ItemPrice_Item_Unit_PriceList_Currency");
            entity.HasIndex(e => new { e.ItemId, e.PriceList }).HasDatabaseName("IX_ItemPrice_Item_PriceList");
        });

        // Attribute Definition
        modelBuilder.Entity<AttributeDefinition>(entity =>
        {
            entity.ToTable("AttributeDefinitions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50).HasDefaultValue("Text");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.AllowedValues).HasMaxLength(2000);
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_AttributeDefinition_Tenant_Code");
        });

        // Item Attribute
        modelBuilder.Entity<ItemAttribute>(entity =>
        {
            entity.ToTable("ItemAttributes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Attributes).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AttributeDefinition)
                .WithMany(a => a.ItemAttributes).HasForeignKey(e => e.AttributeDefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ItemId, e.AttributeDefinitionId })
                .IsUnique().HasDatabaseName("IX_ItemAttribute_Item_Attribute");
        });

        // Tax Definition
        modelBuilder.Entity<TaxDefinition>(entity =>
        {
            entity.ToTable("TaxDefinitions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TaxType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.InclusionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Rate).HasPrecision(18, 4);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_TaxDefinition_Tenant_Code");
            entity.HasMany(e => e.ItemTaxes).WithOne(t => t.TaxDefinition)
                .HasForeignKey(t => t.TaxDefinitionId).OnDelete(DeleteBehavior.Restrict);
        });

        // Item Tax
        modelBuilder.Entity<ItemTax>(entity =>
        {
            entity.ToTable("ItemTaxes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverrideRate).HasPrecision(18, 4);
            entity.Property(e => e.EffectiveRate).HasPrecision(18, 4);
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Taxes).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TaxDefinition)
                .WithMany(t => t.ItemTaxes).HasForeignKey(e => e.TaxDefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ItemId, e.TaxDefinitionId })
                .IsUnique().HasDatabaseName("IX_ItemTax_Item_Tax");
        });

        // Color
        modelBuilder.Entity<Color>(entity =>
        {
            entity.ToTable("Colors", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HexCode).HasMaxLength(7);   // #RRGGBB
            entity.Property(e => e.ColorFamily).HasMaxLength(100);
            entity.Property(e => e.SwatchImageUrl).HasMaxLength(2000);

            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_Color_Tenant_Code");
            entity.HasIndex(e => e.ColorFamily).HasDatabaseName("IX_Color_Family");
        });

        // Item Color
        modelBuilder.Entity<ItemColor>(entity =>
        {
            entity.ToTable("ItemColors", DefaultSchema);
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Colors).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Color)
                .WithMany(c => c.ItemColors).HasForeignKey(e => e.ColorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ItemBarcode)
                .WithMany().HasForeignKey(e => e.ItemBarcodeId).OnDelete(DeleteBehavior.ClientSetNull);

            // One color per item - no duplicates
            entity.HasIndex(e => new { e.ItemId, e.ColorId })
                .IsUnique().HasDatabaseName("IX_ItemColor_Item_Color");
            // One default color per item
            entity.HasIndex(e => new { e.ItemId, e.IsDefault })
                .HasFilter("[IsDefault] = 1").IsUnique().HasDatabaseName("IX_ItemColor_Item_Default");
        });

        // Documents
        modelBuilder.Entity<InventoryDocument>(entity =>
        {
            entity.ToTable("InventoryDocuments", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Draft");
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TotalQuantity).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.HasOne(e => e.FromWarehouse).WithMany().HasForeignKey(e => e.FromWarehouseId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.ToWarehouse).WithMany().HasForeignKey(e => e.ToWarehouseId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasMany(e => e.Lines).WithOne(l => l.Document).HasForeignKey(l => l.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.DocumentNumber, e.DocumentType })
                .IsUnique().HasDatabaseName("IX_InventoryDocument_Tenant_Number_Type");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_InventoryDocument_Status");
            entity.HasIndex(e => e.DocumentDate).HasDatabaseName("IX_InventoryDocument_Date");
        });

        // Inventory Document Line
        modelBuilder.Entity<InventoryDocumentLine>(entity =>
        {
            entity.ToTable("InventoryDocumentLines", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Document).WithMany(d => d.Lines).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany().HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Transactions).WithOne(t => t.DocumentLine).HasForeignKey(t => t.DocumentLineId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.DocumentId, e.LineNumber }).HasDatabaseName("IX_InventoryDocumentLine_Document_LineNumber");
        });

        // Inventory Transaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.HasOne(e => e.Item).WithMany(i => i.Transactions).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse).WithMany(w => w.Transactions).HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany(b => b.Transactions).HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Document).WithMany().HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DocumentLine).WithMany(l => l.Transactions).HasForeignKey(e => e.DocumentLineId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CostLayer).WithMany().HasForeignKey(e => e.CostLayerId).OnDelete(DeleteBehavior.SetNull);
            // NoAction: ItemSerial/ItemBatch are cascade-reachable from Item, so a SetNull here would add
            // parallel cascade-action paths into InventoryTransactions (error 1785). The ledger is
            // historical and never hard-deleted, so restricting is correct.
            entity.HasOne(e => e.ItemSerial).WithMany().HasForeignKey(e => e.ItemSerialId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ItemBatch).WithMany().HasForeignKey(e => e.ItemBatchId).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.ItemId, e.WarehouseId }).HasDatabaseName("IX_InventoryTransaction_Item_Warehouse");
            entity.HasIndex(e => e.TransactionDate).HasDatabaseName("IX_InventoryTransaction_Date");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_InventoryTransaction_Document");
        });

        // Inventory Balance
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("InventoryBalances", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityOnHand).HasPrecision(18, 4);
            entity.Property(e => e.QuantityReserved).HasPrecision(18, 4);
            entity.Property(e => e.QuantityAvailable).HasPrecision(18, 4);
            entity.Property(e => e.AverageCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.CostingMethod).HasMaxLength(50).HasDefaultValue("MovingAverage");
            entity.HasOne(e => e.Item).WithMany(i => i.Balances).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Warehouse).WithMany(w => w.Balances).HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany(b => b.Balances).HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Variant).WithMany(v => v.Balances).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.ClientSetNull);
            // No filter (HasFilter(null)) so SQL Server treats NULL Bin/Variant as equal — enforcing
            // exactly ONE balance row per (Item, Warehouse, Bin, Variant), including the common
            // warehouse-level row where Bin and Variant are both NULL. (EF's default would otherwise
            // add `WHERE BinId IS NOT NULL AND VariantId IS NOT NULL`, leaving that row unconstrained.)
            entity.HasIndex(e => new { e.ItemId, e.WarehouseId, e.BinId, e.VariantId }).IsUnique().HasFilter(null).HasDatabaseName("IX_InventoryBalance_Item_Warehouse_Bin_Variant");
            entity.HasIndex(e => e.QuantityOnHand).HasDatabaseName("IX_InventoryBalance_QuantityOnHand");
        });

        // Inventory Valuation
        modelBuilder.Entity<InventoryValuation>(entity =>
        {
            entity.ToTable("InventoryValuations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.ValuationMethod).HasMaxLength(50).HasDefaultValue("MovingAverage");
            entity.Property(e => e.PeriodReference).HasMaxLength(50);
            entity.HasOne(e => e.Item).WithMany(i => i.Valuations).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ValuationDate, e.PeriodReference }).HasDatabaseName("IX_InventoryValuation_Date_Period");
        });

        // Inventory Cost Layer
        modelBuilder.Entity<InventoryCostLayer>(entity =>
        {
            entity.ToTable("InventoryCostLayers", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalQuantity).HasPrecision(18, 4);
            entity.Property(e => e.RemainingQuantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.HasOne(e => e.Item).WithMany(i => i.CostLayers).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReceiptTransaction).WithMany().HasForeignKey(e => e.ReceiptTransactionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ItemId, e.WarehouseId, e.IsExhausted, e.ReceiptDate }).HasDatabaseName("IX_InventoryCostLayer_Item_Warehouse_Status");
        });

        // Item Comment
        modelBuilder.Entity<ItemComment>(entity =>
        {
            entity.ToTable("ItemComments", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommentType).IsRequired().HasMaxLength(50).HasDefaultValue("Internal");
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.Item).WithMany(i => i.Comments).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemId, e.IsPinned }).HasDatabaseName("IX_ItemComment_Item_Pinned");
        });

        // Size
        modelBuilder.Entity<Size>(entity =>
        {
            entity.ToTable("Sizes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SizeChart).IsRequired().HasMaxLength(50).HasDefaultValue("Apparel");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code, e.SizeChart })
                .IsUnique().HasDatabaseName("IX_Size_Tenant_Code_Chart");
        });

        // Item Size
        modelBuilder.Entity<ItemSize>(entity =>
        {
            entity.ToTable("ItemSizes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Item).WithMany(i => i.Sizes).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Size).WithMany(s => s.ItemSizes).HasForeignKey(e => e.SizeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ItemBarcode).WithMany().HasForeignKey(e => e.ItemBarcodeId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasIndex(e => new { e.ItemId, e.SizeId }).IsUnique().HasDatabaseName("IX_ItemSize_Item_Size");
            entity.HasIndex(e => new { e.ItemId, e.IsDefault })
                .HasFilter("[IsDefault] = 1").IsUnique().HasDatabaseName("IX_ItemSize_Item_Default");
        });

        // Item Variant
        modelBuilder.Entity<ItemVariant>(entity =>
        {
            entity.ToTable("ItemVariants", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VariantCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VariantName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(2000);
            entity.Property(e => e.ExtraDimension).HasMaxLength(200);
            entity.Property(e => e.SalePriceOverride).HasPrecision(18, 4);
            entity.Property(e => e.PurchasePriceOverride).HasPrecision(18, 4);
            entity.Property(e => e.WeightKg).HasPrecision(18, 4);
            entity.HasOne(e => e.Item).WithMany(i => i.Variants).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Color).WithMany().HasForeignKey(e => e.ColorId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Size).WithMany(s => s.Variants).HasForeignKey(e => e.SizeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.VariantCode).IsUnique().HasDatabaseName("IX_ItemVariant_Code");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.VariantCode })
                .IsUnique().HasDatabaseName("IX_ItemVariant_Tenant_Code");
            entity.HasIndex(e => new { e.ItemId, e.ColorId, e.SizeId }).HasDatabaseName("IX_ItemVariant_Item_Color_Size");
        });

        // Item Shipping
        modelBuilder.Entity<ItemShipping>(entity =>
        {
            entity.ToTable("ItemShippings", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WeightKg).HasPrecision(18, 4);
            entity.Property(e => e.LengthCm).HasPrecision(18, 2);
            entity.Property(e => e.WidthCm).HasPrecision(18, 2);
            entity.Property(e => e.HeightCm).HasPrecision(18, 2);
            entity.Property(e => e.VolumetricWeightKg).HasPrecision(18, 4);
            entity.Property(e => e.CartonWeightKg).HasPrecision(18, 4);
            entity.Property(e => e.CartonLengthCm).HasPrecision(18, 2);
            entity.Property(e => e.CartonWidthCm).HasPrecision(18, 2);
            entity.Property(e => e.CartonHeightCm).HasPrecision(18, 2);
            entity.Property(e => e.CountryOfOrigin).HasMaxLength(5);
            entity.Property(e => e.HsCode).HasMaxLength(20);
            entity.Property(e => e.HandlingNotes).HasMaxLength(500);
        });

        // Item SEO
        modelBuilder.Entity<ItemSeo>(entity =>
        {
            entity.ToTable("ItemSeos", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MetaTitle).HasMaxLength(255);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.Property(e => e.MetaKeywords).HasMaxLength(1000);
            entity.Property(e => e.Slug).HasMaxLength(500);
            entity.Property(e => e.CanonicalUrl).HasMaxLength(2000);
            entity.HasIndex(e => e.Slug).IsUnique()
                .HasFilter("[Slug] IS NOT NULL").HasDatabaseName("IX_ItemSeo_Slug");
        });

        // Item Channel Listing
        modelBuilder.Entity<ItemChannelListing>(entity =>
        {
            entity.ToTable("ItemChannelListings", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ListingStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Draft");
            entity.Property(e => e.ChannelTitle).HasMaxLength(255);
            entity.Property(e => e.ChannelDescription).HasMaxLength(8000);
            entity.Property(e => e.ChannelPrice).HasPrecision(18, 4);
            entity.Property(e => e.ChannelDiscount).HasPrecision(18, 4);
            entity.Property(e => e.DiscountType).HasMaxLength(20);
            entity.Property(e => e.ExternalProductId).HasMaxLength(100);
            entity.Property(e => e.ExternalSku).HasMaxLength(100);
            entity.Property(e => e.ListingUrl).HasMaxLength(2000);
            entity.HasOne(e => e.Item).WithMany(i => i.ChannelListings).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemId, e.Channel }).IsUnique().HasDatabaseName("IX_ItemChannelListing_Item_Channel");
            entity.HasIndex(e => new { e.Channel, e.ListingStatus }).HasDatabaseName("IX_ItemChannelListing_Channel_Status");
        });

        // Item Supplier
        modelBuilder.Entity<ItemSupplier>(entity =>
        {
            entity.ToTable("ItemSuppliers", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierItemCode).HasMaxLength(100);
            entity.Property(e => e.SupplierItemName).HasMaxLength(255);
            entity.Property(e => e.LastPurchasePrice).HasPrecision(18, 4);
            entity.Property(e => e.MinOrderQuantity).HasPrecision(18, 4);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10).HasDefaultValue("USD");
            entity.HasOne(e => e.Item).WithMany(i => i.Suppliers).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemId, e.SupplierId }).IsUnique().HasDatabaseName("IX_ItemSupplier_Item_Supplier");
            entity.HasIndex(e => new { e.ItemId, e.IsPrimary })
                .HasFilter("[IsPrimary] = 1").IsUnique().HasDatabaseName("IX_ItemSupplier_Item_Primary");
        });

        // Item Bundle
        modelBuilder.Entity<ItemBundle>(entity =>
        {
            entity.ToTable("ItemBundles", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.HasOne(e => e.BundleItem).WithMany(i => i.BundleComponents).HasForeignKey(e => e.BundleItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ComponentItem).WithMany(i => i.BundleParents).HasForeignKey(e => e.ComponentItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.BundleItemId, e.ComponentItemId }).IsUnique().HasDatabaseName("IX_ItemBundle_Bundle_Component");
        });

        // Item Substitution
        modelBuilder.Entity<ItemSubstitution>(entity =>
        {
            entity.ToTable("ItemSubstitutions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasOne(e => e.Item).WithMany(i => i.Substitutions).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SubstituteItem).WithMany().HasForeignKey(e => e.SubstituteItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.ItemId, e.SubstituteItemId }).IsUnique().HasDatabaseName("IX_ItemSubstitution_Item_Substitute");
        });

        // Item Warranty
        modelBuilder.Entity<ItemWarranty>(entity =>
        {
            entity.ToTable("ItemWarranties", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WarrantyType).IsRequired().HasMaxLength(50).HasDefaultValue("Seller");
            entity.Property(e => e.PolicyDescription).HasMaxLength(2000);
            entity.Property(e => e.ProviderName).HasMaxLength(255);
            entity.Property(e => e.ProviderContact).HasMaxLength(500);
        });

        // Item Discount
        modelBuilder.Entity<ItemDiscount>(entity =>
        {
            entity.ToTable("ItemDiscounts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(50).HasDefaultValue("Percentage");
            entity.Property(e => e.DiscountValue).HasPrecision(18, 4);
            entity.Property(e => e.MinQuantity).HasPrecision(18, 4);
            entity.Property(e => e.MaxQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ApplicableChannels).HasMaxLength(200);
            entity.HasOne(e => e.Item).WithMany(i => i.Discounts).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemId, e.IsActive, e.ValidFrom, e.ValidTo }).HasDatabaseName("IX_ItemDiscount_Item_Active_Period");
        });

        // Inventory Balance (variant-aware)
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("InventoryBalances", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityOnHand).HasPrecision(18, 4);
            entity.Property(e => e.QuantityReserved).HasPrecision(18, 4);
            entity.Property(e => e.QuantityAvailable).HasPrecision(18, 4);
            entity.Property(e => e.AverageCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.CostingMethod).HasMaxLength(50).HasDefaultValue("MovingAverage");
            entity.HasOne(e => e.Item).WithMany(i => i.Balances).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Warehouse).WithMany(w => w.Balances).HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany(b => b.Balances).HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(e => e.Variant).WithMany(v => v.Balances).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.ClientSetNull);
            // No filter (HasFilter(null)) so SQL Server treats NULL Bin/Variant as equal — enforcing
            // exactly ONE balance row per (Item, Warehouse, Bin, Variant), including the common
            // warehouse-level row where Bin and Variant are both NULL. (EF's default would otherwise
            // add `WHERE BinId IS NOT NULL AND VariantId IS NOT NULL`, leaving that row unconstrained.)
            entity.HasIndex(e => new { e.ItemId, e.WarehouseId, e.BinId, e.VariantId }).IsUnique().HasFilter(null).HasDatabaseName("IX_InventoryBalance_Item_Warehouse_Bin_Variant");
            entity.HasIndex(e => e.QuantityOnHand).HasDatabaseName("IX_InventoryBalance_QuantityOnHand");
        });

        // Inventory Document Line — lot capture + serial children
        modelBuilder.Entity<InventoryDocumentLine>(entity =>
        {
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.HasMany(e => e.LineSerials).WithOne(s => s.DocumentLine)
                .HasForeignKey(s => s.DocumentLineId).OnDelete(DeleteBehavior.Cascade);
        });

        // Inventory Document Line Serial
        modelBuilder.Entity<InventoryDocumentLineSerial>(entity =>
        {
            entity.ToTable("InventoryDocumentLineSerials", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Imei).HasMaxLength(50);
            entity.Property(e => e.Imei2).HasMaxLength(50);
            entity.Property(e => e.MacAddress).HasMaxLength(50);
            entity.HasIndex(e => e.DocumentLineId).HasDatabaseName("IX_InventoryDocumentLineSerial_Line");
        });

        // Item Serial (per-unit registry)
        modelBuilder.Entity<ItemSerial>(entity =>
        {
            entity.ToTable("ItemSerials", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Imei).HasMaxLength(50);
            entity.Property(e => e.Imei2).HasMaxLength(50);
            entity.Property(e => e.MacAddress).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("InStock");
            entity.Property(e => e.SalesReference).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);

            entity.HasOne(e => e.Item).WithMany(i => i.Serials).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            // NoAction on Variant: Item→ItemVariant is already Cascade, so a SetNull here would form a
            // second cascade-action path Item→ItemVariant→ItemSerial on top of the direct Item→ItemSerial
            // cascade — SQL Server rejects multiple cascade paths (error 1785).
            entity.HasOne(e => e.Variant).WithMany().HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.NoAction);
            // Restrict/ClientSetNull (not SetNull) for the location FKs — matches the InventoryBalance/
            // InventoryTransaction convention and keeps ItemSerials out of the multiple-cascade-paths
            // count (SetNull is a cascade action; Restrict/ClientSetNull are not).
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany().HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasMany(e => e.History).WithOne(h => h.ItemSerial).HasForeignKey(h => h.ItemSerialId).OnDelete(DeleteBehavior.Cascade);

            // Serial number unique per item within a tenant.
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.ItemId, e.SerialNumber })
                .IsUnique().HasDatabaseName("IX_ItemSerial_Tenant_Item_Serial");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Imei })
                .HasFilter("[Imei] IS NOT NULL").HasDatabaseName("IX_ItemSerial_Tenant_Imei");
            entity.HasIndex(e => new { e.ItemId, e.Status }).HasDatabaseName("IX_ItemSerial_Item_Status");
            entity.HasIndex(e => new { e.WarehouseId, e.Status }).HasDatabaseName("IX_ItemSerial_Warehouse_Status");
        });

        // Item Serial History
        modelBuilder.Entity<ItemSerialHistory>(entity =>
        {
            entity.ToTable("ItemSerialHistories", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FromStatus).HasMaxLength(50);
            entity.Property(e => e.ToStatus).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasOne(e => e.ItemSerial).WithMany(s => s.History).HasForeignKey(e => e.ItemSerialId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ItemSerialId, e.EventDate }).HasDatabaseName("IX_ItemSerialHistory_Serial_Date");
        });

        // Item Batch (lot registry)
        modelBuilder.Entity<ItemBatch>(entity =>
        {
            entity.ToTable("ItemBatches", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
            entity.Property(e => e.ReceivedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.RemainingQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ReservedQuantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);

            entity.HasOne(e => e.Item).WithMany(i => i.Batches).HasForeignKey(e => e.ItemId).OnDelete(DeleteBehavior.Cascade);
            // NoAction on Variant (same reason as ItemSerial): avoids a second cascade path
            // Item→ItemVariant→ItemBatch alongside the direct Item→ItemBatch cascade (error 1785).
            entity.HasOne(e => e.Variant).WithMany().HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.LotStocks).WithOne(s => s.Batch).HasForeignKey(s => s.ItemBatchId).OnDelete(DeleteBehavior.Cascade);

            // Batch number unique per item within a tenant.
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.ItemId, e.BatchNumber })
                .IsUnique().HasDatabaseName("IX_ItemBatch_Tenant_Item_Batch");
            entity.HasIndex(e => new { e.ItemId, e.Status }).HasDatabaseName("IX_ItemBatch_Item_Status");
            entity.HasIndex(e => e.ExpiryDate).HasDatabaseName("IX_ItemBatch_Expiry");
        });

        // Item Lot Stock (per-warehouse batch qty)
        modelBuilder.Entity<ItemLotStock>(entity =>
        {
            entity.ToTable("ItemLotStocks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            // NoAction (not Cascade) on the batch FK: an Item already cascade-deletes its ItemBatches,
            // and ItemLotStock also has a restricted Warehouse FK — a two-level cascade to this table
            // trips SQL Server's multiple-cascade-paths guard (error 1785). Lot stocks are cleaned up
            // explicitly; hard deletes are rare anyway (the module uses soft delete).
            entity.HasOne(e => e.Batch).WithMany(b => b.LotStocks).HasForeignKey(e => e.ItemBatchId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bin).WithMany().HasForeignKey(e => e.BinId).OnDelete(DeleteBehavior.ClientSetNull);
            // Exactly one row per (Batch, Warehouse, Bin) — NULL bin treated as a distinct value.
            entity.HasIndex(e => new { e.ItemBatchId, e.WarehouseId, e.BinId })
                .IsUnique().HasFilter(null).HasDatabaseName("IX_ItemLotStock_Batch_Warehouse_Bin");
        });
    }

    protected static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utc = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcN = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
        foreach (var e in modelBuilder.Model.GetEntityTypes())
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(DateTime))  p.SetValueConverter(utc);
                if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcN);
            }
    }
}
