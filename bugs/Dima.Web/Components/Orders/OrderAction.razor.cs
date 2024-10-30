using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Orders;
using Dima.Core.Requests.Stripe;
using Dima.Web.Pages.Orders;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dima.Web.Components.Orders;

public partial class OrderActionComponent : ComponentBase
{
    #region Parameters

    [Parameter, EditorRequired] public Order Order { get; set; } = null!;

    [CascadingParameter] public DetailsPage Parent { get; set; } = null!;

    #endregion Parameters

    #region Services

    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject] public IStripeHandler StripeHandler { get; set; } = null!;

    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Inject] private IOrderHandler OrderHandler { get; set; } = null!;

    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    #endregion Services

    #region Public Methods

    public async void OnCancelButtonClicked()
    {
        bool? result = await DialogService.ShowMessageBox(
            "ATENÇÃO",
            "Deseja realmente cancelar este pedido?",
            yesText: "SIM", cancelText: "NÃO");

        if (result is true)
            await CancelOrderAsync();
    }

    public async void OnPayButtonClicked()
    {
        await PayOrderAsync();
    }

    public async void OnRefundButtonClicked()
    {
        bool? result = await DialogService.ShowMessageBox(
            "ATENÇÃO",
            "Deseja realmente solicitar o estorno deste pedido?",
            yesText: "SIM", cancelText: "NÃO");

        if (result is true)
            await RefundOrderAsync();
    }

    #endregion Public Methods

    #region Private Methods

    private async Task CancelOrderAsync()
    {
        var request = new CancelOrderRequest
        {
            Id = Order.Id
        };

        var result = await OrderHandler.CancelAsync(request);
        if (result.IsSuccess)
            Parent.RefreshState(result.Data!);
        else
            Snackbar.Add(result.Message, Severity.Error);
    }

    private async Task PayOrderAsync()
    {
        var request = new CreateSessionRequest
        {
            OrderNumber = Order.Number,
            OrderTotal = (int)(Math.Round(Order.Total, 2) * 100),
            ProductTitle = Order.Product.Title,
            ProductDescription = Order.Product.Description
        };

        try
        {
            var result = await StripeHandler.CreateSessionAsync(request);
            if (result.Data is not null)
                await JsRuntime.InvokeVoidAsync("checkout", Configuration.StripePublicKey, result.Data);
            else
                Snackbar.Add("Não foi possível iniciar seu pagamento", Severity.Error);
        }
        catch
        {
            Snackbar.Add("Não foi possível iniciar seu pagamento", Severity.Error);
        }
    }

    private async Task RefundOrderAsync()
    {
        var request = new RefundOrderRequest
        {
            Id = Order.Id
        };

        var result = await OrderHandler.RefundAsync(request);
        if (result.IsSuccess)
            Parent.RefreshState(result.Data!);
        else
            Snackbar.Add(result.Message, Severity.Error);
    }

    #endregion Private Methods
}