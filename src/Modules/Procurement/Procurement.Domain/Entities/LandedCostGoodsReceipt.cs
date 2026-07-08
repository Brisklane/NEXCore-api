using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Join entity — links a Landed Cost to the Goods Receipts it covers.
/// A single landed cost (e.g. one freight invoice) can span multiple GRNs
/// when a shipment covers several purchase orders.
/// </summary>
public class LandedCostGoodsReceipt : BaseEntity
{
    public Guid LandedCostId { get; set; }
    public LandedCost LandedCost { get; set; } = null!;

    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
}
