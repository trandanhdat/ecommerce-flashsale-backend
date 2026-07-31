using System.Threading.Tasks;
using FlashSale.Application.CQRS.Payments.Commands.InitiatePayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce_flashsale_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var command = new InitiatePaymentCommand(request.OrderId, ipAddress);
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                return Ok(new { PaymentUrl = result.PaymentUrl });
            }

            return BadRequest(new { Message = result.ErrorMessage });
        }

        [HttpGet("vnpay-callback")]
        [AllowAnonymous] // Cực kỳ quan trọng: VNPay gọi về không có JWT Token
        public async Task<IActionResult> VnPayCallback()
        {
            // Parse toàn bộ query string thành Dictionary (Trigger rebuild)
            var callbackParams = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                callbackParams[key] = Request.Query[key];
            }

            var command = new FlashSale.Application.CQRS.Payments.Commands.ConfirmPaymentCallback.ConfirmPaymentCallbackCommand(callbackParams);
            var result = await _mediator.Send(command);

            // Redirect thẳng về Front-end (hiển thị trang thành công/thất bại)
            // Không trả JSON vì đây là luồng trình duyệt redirect từ VNPay về
            return Redirect(result.RedirectUrl);
        }
    }

    public class InitiatePaymentRequest
    {
        public System.Guid OrderId { get; set; }
    }
}
