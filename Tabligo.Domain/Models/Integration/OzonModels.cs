using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

// Product Models
public class OzonProduct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;
    [JsonPropertyName("old_price")]
    public string? OldPrice { get; set; }
    [JsonPropertyName("premium_price")]
    public string? PremiumPrice { get; set; }
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "RUB";
    [JsonPropertyName("vat")]
    public string Vat { get; set; } = string.Empty;
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
    [JsonPropertyName("state")]
    public OzonProductState State { get; set; } = new();
}

public class OzonProductState
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("state_id")]
    public int StateId { get; set; }
}

// Posting Models
public class OzonPosting
{
    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }
    [JsonPropertyName("order_number")]
    public string OrderNumber { get; set; } = string.Empty;
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("delivery_method")]
    public OzonDeliveryMethod DeliveryMethod { get; set; } = new();
    [JsonPropertyName("warehouse")]
    public OzonWarehouse Warehouse { get; set; } = new();
    [JsonPropertyName("products")]
    public List<OzonPostingProduct> Products { get; set; } = new();
    [JsonPropertyName("in_posting_at")]
    public DateTime InPostingAt { get; set; }
    [JsonPropertyName("shipment_date")]
    public DateTime? ShipmentDate { get; set; }
    [JsonPropertyName("delivering_date")]
    public DateTime? DeliveringDate { get; set; }
    [JsonPropertyName("customer")]
    public OzonCustomer Customer { get; set; } = new();
    [JsonPropertyName("addressee")]
    public OzonAddressee Addressee { get; set; } = new();
    [JsonPropertyName("barcodes")]
    public OzonBarcodes Barcodes { get; set; } = new();
    [JsonPropertyName("analytics_data")]
    public OzonAnalyticsData AnalyticsData { get; set; } = new();
    [JsonPropertyName("financial_data")]
    public OzonFinancialData FinancialData { get; set; } = new();
}

public class OzonDeliveryMethod
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("warehouse")]
    public string Warehouse { get; set; } = string.Empty;
    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }
}

public class OzonWarehouse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class OzonPostingProduct
{
    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;
    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("sku")]
    public long Sku { get; set; }
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    [JsonPropertyName("mandatory_mark")]
    public List<string> MandatoryMark { get; set; } = new();
}

public class OzonCustomer
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class OzonAddressee
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}

public class OzonBarcodes
{
    [JsonPropertyName("upper_barcode")]
    public string UpperBarcode { get; set; } = string.Empty;
    [JsonPropertyName("lower_barcode")]
    public string LowerBarcode { get; set; } = string.Empty;
}

public class OzonAnalyticsData
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
    [JsonPropertyName("delivery_type")]
    public string DeliveryType { get; set; } = string.Empty;
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }
    [JsonPropertyName("payment_type_group_name")]
    public string PaymentTypeGroupName { get; set; } = string.Empty;
    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }
    [JsonPropertyName("warehouse_name")]
    public string WarehouseName { get; set; } = string.Empty;
    [JsonPropertyName("is_legal")]
    public bool IsLegal { get; set; }
}

public class OzonFinancialData
{
    [JsonPropertyName("products")]
    public List<OzonFinancialProduct> Products { get; set; } = new();
    [JsonPropertyName("posting_services")]
    public OzonPostingServices PostingServices { get; set; } = new();
}

public class OzonFinancialProduct
{
    [JsonPropertyName("commission_amount")]
    public decimal CommissionAmount { get; set; }
    [JsonPropertyName("commission_percent")]
    public decimal CommissionPercent { get; set; }
    [JsonPropertyName("payout")]
    public decimal Payout { get; set; }
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }
    [JsonPropertyName("old_price")]
    public decimal OldPrice { get; set; }
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    [JsonPropertyName("total_discount_value")]
    public decimal TotalDiscountValue { get; set; }
    [JsonPropertyName("total_discount_percent")]
    public decimal TotalDiscountPercent { get; set; }
    [JsonPropertyName("actions")]
    public List<string> Actions { get; set; } = new();
    [JsonPropertyName("picking")]
    public decimal Picking { get; set; }
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    [JsonPropertyName("client_price")]
    public decimal ClientPrice { get; set; }
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "RUB";
}

public class OzonPostingServices
{
    [JsonPropertyName("marketplace_service_item_fulfillment")]
    public decimal MarketplaceServiceItemFulfillment { get; set; }
    [JsonPropertyName("marketplace_service_item_pickup")]
    public decimal MarketplaceServiceItemPickup { get; set; }
    [JsonPropertyName("marketplace_service_item_dropoff_pvz")]
    public decimal MarketplaceServiceItemDropoffPvz { get; set; }
    [JsonPropertyName("marketplace_service_item_dropoff_sc")]
    public decimal MarketplaceServiceItemDropoffSc { get; set; }
    [JsonPropertyName("marketplace_service_item_dropoff_ff")]
    public decimal MarketplaceServiceItemDropoffFf { get; set; }
    [JsonPropertyName("marketplace_service_item_direct_flow_trans")]
    public decimal MarketplaceServiceItemDirectFlowTrans { get; set; }
    [JsonPropertyName("marketplace_service_item_return_flow_trans")]
    public decimal MarketplaceServiceItemReturnFlowTrans { get; set; }
    [JsonPropertyName("marketplace_service_item_deliv_to_customer")]
    public decimal MarketplaceServiceItemDelivToCustomer { get; set; }
    [JsonPropertyName("marketplace_service_item_return_not_deliv_to_customer")]
    public decimal MarketplaceServiceItemReturnNotDelivToCustomer { get; set; }
    [JsonPropertyName("marketplace_service_item_return_part_goods_customer")]
    public decimal MarketplaceServiceItemReturnPartGoodsCustomer { get; set; }
    [JsonPropertyName("marketplace_service_item_return_after_deliv_to_customer")]
    public decimal MarketplaceServiceItemReturnAfterDelivToCustomer { get; set; }
}

// Return Models
public class OzonReturn
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;
    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    [JsonPropertyName("return_reason_name")]
    public string ReturnReasonName { get; set; } = string.Empty;
    [JsonPropertyName("return_date")]
    public DateTime ReturnDate { get; set; }
    [JsonPropertyName("return_status")]
    public string ReturnStatus { get; set; } = string.Empty;
    [JsonPropertyName("return_type")]
    public string ReturnType { get; set; } = string.Empty;
    [JsonPropertyName("is_opened")]
    public bool IsOpened { get; set; }
    [JsonPropertyName("place")]
    public OzonReturnPlace Place { get; set; } = new();
}

public class OzonReturnPlace
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

// Action Models
public class OzonAction
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("start_at")]
    public DateTime StartAt { get; set; }
    [JsonPropertyName("finish_at")]
    public DateTime FinishAt { get; set; }
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }
    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "RUB";
}

public class OzonActionProduct
{
    [JsonPropertyName("action_id")]
    public long ActionId { get; set; }
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }
    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;
    [JsonPropertyName("discount_value")]
    public decimal DiscountValue { get; set; }
    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    [JsonPropertyName("old_price")]
    public decimal OldPrice { get; set; }
}

// Financial Report Models
public class OzonFinancialReport
{
    [JsonPropertyName("operation_id")]
    public long OperationId { get; set; }
    [JsonPropertyName("operation_type")]
    public string OperationType { get; set; } = string.Empty;
    [JsonPropertyName("operation_date")]
    public DateTime OperationDate { get; set; }
    [JsonPropertyName("operation_type_name")]
    public string OperationTypeName { get; set; } = string.Empty;
    [JsonPropertyName("delivery_charge")]
    public decimal DeliveryCharge { get; set; }
    [JsonPropertyName("return_delivery_charge")]
    public decimal ReturnDeliveryCharge { get; set; }
    [JsonPropertyName("accruals_for_sale")]
    public decimal AccrualsForSale { get; set; }
    [JsonPropertyName("sale_commission")]
    public decimal SaleCommission { get; set; }
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "RUB";
}

// API Response Models
public class OzonApiResponse<T>
{
    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

public class OzonProductListResponse
{
    [JsonPropertyName("items")]
    public List<OzonProduct> Items { get; set; } = new();
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }
}

public class OzonPostingListResponse
{
    [JsonPropertyName("result")]
    public List<OzonPosting> Result { get; set; } = new();
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}

public class OzonReturnListResponse
{
    [JsonPropertyName("result")]
    public List<OzonReturn> Result { get; set; } = new();
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}

public class OzonActionListResponse
{
    [JsonPropertyName("actions")]
    public List<OzonAction> Actions { get; set; } = new();
}

public class OzonActionProductListResponse
{
    [JsonPropertyName("products")]
    public List<OzonActionProduct> Products { get; set; } = new();
}

public class OzonFinancialReportListResponse
{
    [JsonPropertyName("result")]
    public List<OzonFinancialReport> Result { get; set; } = new();
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }
}
