using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Pricing;
using MarketApp.Application.DTOs.StockTransfers;
using MarketApp.Application.Services;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Enums;

namespace MarketApp.Application.Tests;

public class TransferReceiptPricingTests
{
    [Fact]
    public async Task Receive_applies_baseline_preserves_minimum_and_records_actor()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        var saves = f.Work.SaveCount;

        var result = await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(StockTransferResultStatus.Success, result.Status);
        Assert.Equal(80m, f.Price!.BaselineUnitPrice);
        Assert.Equal(200m, f.Price.MinimumSellingPrice);
        Assert.Equal(3, f.Price.Revision);
        Assert.Equal(3, f.Price.BaselineRevision);
        Assert.Equal(15m, f.DestinationBalance.Quantity);
        Assert.Equal(StockTransferStatus.Received, transfer.Status);
        Assert.Equal(saves + 1, f.Work.SaveCount);
        var history = Assert.Single(f.ReceiptHistory);
        Assert.Equal(f.Actor, history.ChangedByUserId);
        Assert.Equal(transfer.Lines.Single().Id, history.StockTransferLineId);
        Assert.Equal(100m, history.OldBaselineUnitPrice);
        Assert.Equal(80m, history.NewBaselineUnitPrice);
        Assert.Equal(200m, history.OldMinimumSellingPrice);
        Assert.Equal(200m, history.NewMinimumSellingPrice);
    }

    [Fact]
    public async Task Receive_keeps_newer_150_but_receives_stock_from_100_transfer()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(100m);
        await f.SetBaseline(150m);

        var result = await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(StockTransferResultStatus.Success, result.Status);
        Assert.Equal(150m, f.Price!.BaselineUnitPrice);
        Assert.Equal(100m, transfer.Lines.Single().BaselineUnitPrice);
        Assert.Equal(15m, f.DestinationBalance.Quantity);
        Assert.Empty(f.ReceiptHistory);
    }

    [Fact]
    public async Task Minimum_only_change_does_not_block_transfer_baseline()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        await f.SetMinimum(240m);
        Assert.Equal(1, f.Price!.BaselineRevision);

        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(80m, f.Price.BaselineUnitPrice);
        Assert.Equal(240m, f.Price.MinimumSellingPrice);
        Assert.Equal(4, f.Price.Revision);
        Assert.Equal(4, f.Price.BaselineRevision);
    }

    [Fact]
    public async Task Baseline_changed_and_changed_back_still_protects_newer_decision()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        await f.SetBaseline(150m);
        await f.SetBaseline(100m);

        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(100m, f.Price!.BaselineUnitPrice);
        Assert.Empty(f.ReceiptHistory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Receipt_initializes_missing_baseline_including_zero(bool hasMinimum)
    {
        var f = await Fixture.Create(baseline: null, hasMinimum: hasMinimum);
        var transfer = await f.Dispatch(0m);
        Assert.Equal(0, transfer.Lines.Single().BaselineRevisionAtCreation);

        var result = await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(StockTransferResultStatus.Success, result.Status);
        Assert.Equal(0m, f.Price!.BaselineUnitPrice);
        Assert.Equal(hasMinimum ? (decimal?)200m : null, f.Price.MinimumSellingPrice);
        Assert.Single(f.ReceiptHistory);
    }

    [Fact]
    public async Task Repeated_receipt_does_not_duplicate_stock_history_or_save()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);
        var saves = f.Work.SaveCount;
        var movements = f.Work.Repo<StockMovement>().Items.Count;

        var result = await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(StockTransferResultStatus.Success, result.Status);
        Assert.Equal(15m, f.DestinationBalance.Quantity);
        Assert.Single(f.ReceiptHistory);
        Assert.Equal(saves, f.Work.SaveCount);
        Assert.Equal(movements, f.Work.Repo<StockMovement>().Items.Count);
    }

    [Fact]
    public async Task Same_product_lines_at_one_price_produce_one_price_event()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m, lines: 2);

        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(20m, f.DestinationBalance.Quantity);
        Assert.Single(f.ReceiptHistory);
        Assert.Equal(2, f.Work.Repo<StockMovement>().Items
            .Count(m => m.MovementType == StockMovementType.TransferIn));
    }

    [Fact]
    public async Task Conflicting_baselines_for_same_product_are_rejected_at_creation()
    {
        var f = await Fixture.Create();
        var request = f.Request(80m, 2);
        request.Lines[1].BaselineUnitPrice = 90m;
        var saves = f.Work.SaveCount;

        var result = await f.Transfers.CreateAsync(request);

        Assert.Equal(StockTransferResultStatus.InvalidRequest, result.Status);
        Assert.Empty(f.Work.Repo<StockTransfer>().Items);
        Assert.Equal(saves, f.Work.SaveCount);
    }

    [Fact]
    public async Task Legacy_transfer_receives_stock_without_overwriting_baseline()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        transfer.Lines.Single().BaselineRevisionAtCreation = null;

        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(100m, f.Price!.BaselineUnitPrice);
        Assert.Equal(15m, f.DestinationBalance.Quantity);
        Assert.Empty(f.ReceiptHistory);
    }

    [Fact]
    public async Task Later_validation_failure_leaves_all_tracked_stock_and_prices_unchanged()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        var other = new Product();
        f.Work.Repo<Product>().Items.Add(other);
        var otherPrice = new BranchProductPrice
        {
            BranchId = f.Branch.Id, ProductId = other.Id,
            BaselineUnitPrice = 100m, BaselineRevision = 1, Revision = int.MaxValue
        };
        f.Work.Repo<BranchProductPrice>().Items.Add(otherPrice);
        transfer.Lines.Add(new StockTransferLine
        {
            StockTransferId = transfer.Id, LineNumber = 2, ProductId = other.Id,
            Quantity = 2m, BaselineUnitPrice = 80m, BaselineRevisionAtCreation = 1
        });
        var saves = f.Work.SaveCount;
        var movements = f.Work.Repo<StockMovement>().Items.Count;

        var result = await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(StockTransferResultStatus.Conflict, result.Status);
        Assert.Equal(StockTransferStatus.InTransit, transfer.Status);
        Assert.Equal(10m, f.DestinationBalance.Quantity);
        Assert.Equal(100m, f.Price!.BaselineUnitPrice);
        Assert.Equal(100m, otherPrice.BaselineUnitPrice);
        Assert.Empty(f.ReceiptHistory);
        Assert.Equal(saves, f.Work.SaveCount);
        Assert.Equal(movements, f.Work.Repo<StockMovement>().Items.Count);
    }

    [Fact]
    public async Task Receipt_requires_authenticated_actor_before_mutating_stock()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(80m);
        var saves = f.Work.SaveCount;

        var result = await f.Transfers.ReceiveAsync(transfer.Id, Guid.Empty);

        Assert.Equal(StockTransferResultStatus.InvalidRequest, result.Status);
        Assert.Equal(StockTransferStatus.InTransit, transfer.Status);
        Assert.Equal(10m, f.DestinationBalance.Quantity);
        Assert.Equal(saves, f.Work.SaveCount);
    }

    [Fact]
    public async Task Same_baseline_receipt_does_not_create_artificial_price_revision()
    {
        var f = await Fixture.Create();
        var transfer = await f.Dispatch(100m);

        await f.Transfers.ReceiveAsync(transfer.Id, f.Actor);

        Assert.Equal(2, f.Price!.Revision);
        Assert.Equal(1, f.Price.BaselineRevision);
        Assert.Equal(15m, f.DestinationBalance.Quantity);
        Assert.Empty(f.ReceiptHistory);
    }

    private sealed class Fixture
    {
        public FakeUnitOfWork Work { get; } = new();
        public Guid Actor { get; } = Guid.NewGuid();
        public Branch Branch { get; } = new();
        public Product Product { get; } = new();
        public InventoryLocation Source { get; } = new();
        public InventoryLocation Destination { get; }
        public StockBalance DestinationBalance { get; }
        public StockTransferService Transfers { get; }
        private BranchProductPriceService Pricing { get; }
        public BranchProductPrice? Price => Work.Repo<BranchProductPrice>().Items
            .SingleOrDefault(p => p.ProductId == Product.Id);
        public IEnumerable<BranchProductPriceHistory> ReceiptHistory =>
            Work.Repo<BranchProductPriceHistory>().Items
                .Where(h => h.ChangeType == BranchPriceChangeType.TransferReceipt);

        private Fixture()
        {
            Destination = new() { BranchId = Branch.Id, Branch = Branch };
            DestinationBalance = new()
            {
                ProductId = Product.Id, InventoryLocationId = Destination.Id, Quantity = 10m
            };
            Work.Repo<Branch>().Items.Add(Branch);
            Work.Repo<Product>().Items.Add(Product);
            Work.Repo<InventoryLocation>().Items.AddRange([Source, Destination]);
            Work.Repo<StockBalance>().Items.AddRange([
                new() { ProductId = Product.Id, InventoryLocationId = Source.Id, Quantity = 100m },
                DestinationBalance
            ]);
            Transfers = new(Work);
            Pricing = new(Work);
        }

        public static async Task<Fixture> Create(decimal? baseline = 100m, bool hasMinimum = true)
        {
            var fixture = new Fixture();
            if (baseline.HasValue) await fixture.SetBaseline(baseline.Value);
            if (hasMinimum) await fixture.SetMinimum(200m);
            return fixture;
        }

        private UpdateBranchPriceDto PriceRequest(decimal value) => new()
        {
            Price = value, ExpectedRevision = Price?.Revision ?? 0, Reason = "Test price decision"
        };

        public async Task SetBaseline(decimal value) => Assert.Equal(BranchPriceResultStatus.Success,
            (await Pricing.SetBaselineAsync(Branch.Id, Product.Id, PriceRequest(value), Actor)).Status);

        public async Task SetMinimum(decimal value) => Assert.Equal(BranchPriceResultStatus.Success,
            (await Pricing.SetMinimumSellingPriceAsync(Branch.Id, Product.Id, PriceRequest(value), Actor)).Status);

        public CreateStockTransferDto Request(decimal value, int lines) => new()
        {
            SourceLocationId = Source.Id, DestinationLocationId = Destination.Id,
            Lines = Enumerable.Range(0, lines).Select(_ => new CreateStockTransferLineDto
            {
                ProductId = Product.Id, Quantity = 5m, BaselineUnitPrice = value
            }).ToList()
        };

        public async Task<StockTransfer> Dispatch(decimal value, int lines = 1)
        {
            var created = await Transfers.CreateAsync(Request(value, lines));
            Assert.Equal(StockTransferResultStatus.Success, created.Status);
            var transfer = Work.Repo<StockTransfer>().Items.Single(t => t.Id == created.Transfer!.Id);
            var shipped = await Transfers.ShipAsync(transfer.Id);
            Assert.Equal(StockTransferResultStatus.Success, shipped.Status);
            Assert.Empty(ReceiptHistory);
            return transfer;
        }
    }
}
