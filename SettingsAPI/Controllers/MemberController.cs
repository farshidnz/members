using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SettingsAPI.Common;
using SettingsAPI.Error;
using SettingsAPI.Model;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using SettingsAPI.Model.Rest.UpdateEmail;
using SettingsAPI.Model.Rest.UpdateMobile;
using SettingsAPI.Model.Rest.VerifyEmail;
using SettingsAPI.Service;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SettingsAPI.Controllers
{
    [EnableCors]
    [Authorize]
    public class MemberController : BaseController
    {
        private readonly IMemberService _memberService;
        private readonly ITransactionService _transactionService;
        private readonly IMemberBankAccountService _memberBankAccountService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberClicksHistoryService _memberClicksHistoryService;
        private readonly IMemberRedeemService _memberRedeemService;
        private readonly IMemberPaypalAccountService _memberPaypalAccountService;
        private readonly ILambdaContext _lambdaContext;
        private readonly IValidationService _validationService;
        private readonly IMemberFavouriteService _memberFavouriteService;
        private readonly IMemberFavouriteCategoryService _memberFavouriteCategoryService;
        private readonly IRequestContext _requestContext;
        private readonly IMapper _mapper;

        public MemberController(IMemberService memberService, ITransactionService transactionService,
            IMemberBankAccountService memberBankAccountService, IHttpContextAccessor httpContextAccessor,
            IMemberClicksHistoryService memberClicksHistoryService, IMemberRedeemService memberRedeemService,
            IMemberPaypalAccountService memberPaypalAccountService, IValidationService validationService
            , IMemberFavouriteService memberFavouriteService
            , IMemberFavouriteCategoryService memberFavouriteCategoryService
            , IRequestContext requestContext,
            IMapper mapper)
        {
            _memberService = memberService;
            _transactionService = transactionService;
            _memberBankAccountService = memberBankAccountService;
            _httpContextAccessor = httpContextAccessor;
            _memberClicksHistoryService = memberClicksHistoryService;
            _memberRedeemService = memberRedeemService;
            _memberPaypalAccountService = memberPaypalAccountService;
            _validationService = validationService;
            _memberFavouriteService = memberFavouriteService;
            _memberFavouriteCategoryService = memberFavouriteCategoryService;
            _lambdaContext = (ILambdaContext)_httpContextAccessor.HttpContext.Items[AbstractAspNetCoreFunction.LAMBDA_CONTEXT];
            _requestContext = requestContext;
            _mapper = mapper;
        }

        [HttpOptions]
        public IActionResult PreflightRoute()
        {
            return NoContent();
        }

        private async Task<int> GetMemberIdFromContext()
        {
            ClaimsPrincipal cp = _httpContextAccessor.HttpContext.User;
            var cognitoIdClaim = cp.FindFirst(Constant.CognitoIdClaimPropertyName);
            if (cognitoIdClaim != null)
            {
                var cognitoMember = await _memberService.GetCashrewardsCognitoMember(cognitoIdClaim.Value);
                return cognitoMember.MemberId;
            }

            return int.Parse(cp.FindFirst(Constant.MemberIdClaimPropertyName)?.Value);
        }

        private async Task<(int?, int)> GetPersonIdAndMemberIdFromContext()
        {
            ClaimsPrincipal cp = _httpContextAccessor.HttpContext.User;
            var cognitoIdClaim = cp.FindFirst(Constant.CognitoIdClaimPropertyName);
            if (cognitoIdClaim != null)
            {
                var cognitoMember = await _memberService.GetCashrewardsCognitoMember(cognitoIdClaim.Value);
                return (cognitoMember.PersonId, cognitoMember.MemberId);
            }

            return (null, int.Parse(cp.FindFirst(Constant.MemberIdClaimPropertyName)?.Value));
        }

        // GET api/member
        [HttpGet]
        public async Task<ActionResult<MemberDetails>> Get()
        {
            try
            {
                (var personId, var memberId) = await GetPersonIdAndMemberIdFromContext();
                return Ok(await _memberService.GetMember(personId, memberId));
            }
            catch (MemberNotFoundException ex)
            {
                var apiMessageResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                };
                return BadRequest(apiMessageResponse);
            }
        }

        [HttpPatch]
        public async Task<ActionResult<ApiMessageResponse>> Update(
            [FromBody] MemberDetailsRequest memberRequest)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                (var personId, var memberId) = await GetPersonIdAndMemberIdFromContext();
                await _memberService.UpdateDetails(personId, memberId, memberRequest.MobileOtp,
                    memberRequest.DateOfBirth, memberRequest.Gender, memberRequest.FirstName, memberRequest.LastName,
                    memberRequest.PostCode);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
                apiResponse.Message = string.Format(AppMessage.UpdateObjectSuccessful, "Member details");

                return Ok(apiResponse);
            }
            catch (InvalidNameException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, ex.Message.StartsWith("First name") ? "FIRST_NAME" : "LAST_NAME");

                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidDateOfBirthException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "DATE_OF_BIRTH");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidGenderException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "GENDER");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidPostCodeException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "POSTCODE");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidMobileOtpException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "MOBILE_OTP");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPatch("mobile")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdatePhone(
            [FromBody] UpdateMobileNumberRequest updateMobileRequest)
        {
            (int? personId, int memberId) = await _requestContext.GetPersonIdAndMemberIdFromContext();
            var member = Mapper.Map<MemberModel>(updateMobileRequest, opts =>
            {
                opts.Items[Constant.Mapper.MemberId] = memberId;
                opts.Items[Constant.Mapper.PersonId] = personId;
            });

            await _memberService.UpdateMobileNumber(member);
            return Ok(new ApiMessageResponse()
            {
                Code = (int)HttpStatusCode.OK,
                Message = string.Format(AppMessage.UpdateObjectSuccessful, "Mobile"),
                Status = AppMessage.ApiResponseStatusUpdated
            });
        }

        [HttpPatch("email")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdateEmail(
            [FromBody] EmailUpdateRequest updateEmailRequest)
        {
            (int? personId, int memberId) = await _requestContext.GetPersonIdAndMemberIdFromContext();
            var member = Mapper.Map<MemberModel>(updateEmailRequest, opts =>
            {
                opts.Items[Constant.Mapper.MemberId] = memberId;
                opts.Items[Constant.Mapper.PersonId] = personId;
            });

            await _memberService.UpdateEmail(member);
            return Ok(new ApiMessageResponse()
            {
                Code = (int)HttpStatusCode.OK,
                Status = AppMessage.ApiResponseStatusUpdated,
                Message = string.Format(AppMessage.UpdateObjectSuccessful, "Email")
            });
        }

        /*Note API parameter: dateFrom, dateTo format: "yyyy-MM-dd", ex: 2015-05-05
                         orderBy is contains by list ['Date', 'Name', 'Amount', 'Status'], default = 'Date'
                         sortDirection is contains by list ['Desc', 'Asc'], default = 'Desc'
                         limit default = 20
                         offset default = 0
                         searchText default empty
                         */

        [HttpGet("transactions")]
        public async Task<ActionResult<Paging<TransactionResult>>> GetTransactions(string searchText, string dateFrom,
            string dateTo,
            int limit = Constant.DefaultLimit, int offset = Constant.DefaultOffset,
            string orderBy = Constant.DefaultOrderByField, string sortDirection = Constant.DefaultSortDirection)
        {
            try
            {
                var pagingResults = await _transactionService.GetTransactions(await GetMemberIdFromContext(), limit, offset,
                    searchText, dateFrom, dateTo, orderBy, sortDirection);

                return pagingResults;
            }
            catch (InvalidQueryConditionException ex)
            {
                var mapStatus = new Dictionary<string, string>
                {
                    {Util.GetDescriptionFromEnum(QueryConditionFields.Limit), "LIMIT"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.Offset), "OFFSET"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom), "DATE_FROM"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateTo), "DATE_TO"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.OrderBy), "ORDER_BY"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.SorDirection), "SORT_DIRECTION"}
                };
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Message = ex.Message
                };

                foreach (var pair in mapStatus.Where(pair => apiResponse.Message.StartsWith(pair.Key)))
                {
                    apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, pair.Value);
                    break;
                }

                return BadRequest(apiResponse);
            }
        }

        /*Note API parameter: dateFrom, dateTo format: "yyyy-MM-dd", ex: 2015-05-05
                              searchText default empty
                       */

        [HttpGet("transactions/count")]
        public async Task<ActionResult<TotalCountResponse>> GetTransactionsTotalCount(string searchText,
            string dateFrom, string dateTo)
        {
            try
            {
                return await _transactionService.GetTransactionsTotalCount(await GetMemberIdFromContext(), searchText,
                    dateFrom, dateTo);
            }
            catch (InvalidQueryConditionException ex)
            {
                var mapStatus = new Dictionary<string, string>
                {
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom), "DATE_FROM"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateTo), "DATE_TO"}
                };
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Message = ex.Message
                };

                foreach (var pair in mapStatus.Where(pair => apiResponse.Message.StartsWith(pair.Key)))
                {
                    apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, pair.Value);
                    break;
                }

                return BadRequest(apiResponse);
            }
        }

        [HttpPost("bank")]
        public async Task<ActionResult<ApiMessageResponse>> SaveMemberBankAccount(
            [FromBody] MemberBankAccountRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberBankAccountService.SaveBankAccount(await GetMemberIdFromContext(), request.AccountName,
                    request.Bsb,
                    request.AccountNumber, request.MobileOtp);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
                apiResponse.Message = string.Format(AppMessage.UpdateObjectSuccessful, "Member bank account");

                return Ok(apiResponse);
            }
            catch (BankAccountValidationException ex)
            {
                var mapStatus = new Dictionary<string, string>
                {
                    {Util.GetDescriptionFromEnum(BankAccountValidationFields.AccountNumber), "ACCOUNT_NUMBER"},
                    {Util.GetDescriptionFromEnum(BankAccountValidationFields.AccountName), "ACCOUNT_NAME"},
                    {Util.GetDescriptionFromEnum(BankAccountValidationFields.Bsb), "BSB"}
                };

                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Message = ex.Message;

                foreach (var pair in mapStatus.Where(pair => apiResponse.Message.StartsWith(pair.Key)))
                {
                    apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, pair.Value);
                    break;
                }

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidMobileOtpException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "MOBILE_OTP");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (DuplicateBankAccountException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusDuplicated;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("bank")]
        public async Task<ActionResult<MemberBankAccountInfo>> GetMemberBankAccountInfo()
        {
            try
            {
                var response = await _memberBankAccountService.GetBankAccountMasked(await GetMemberIdFromContext());

                return Ok(response);
            }
            catch (MemberBankAccountNotFoundException ex)
            {
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                };

                return BadRequest(apiResponse);
            }
        }

        [HttpDelete("bank")]
        public async Task<ActionResult<ApiMessageResponse>> DisconnectBankAccount()
        {
            var apiResponse = new ApiMessageResponse();

            try
            {
                await _memberBankAccountService.DisconnectBankAccount(await GetMemberIdFromContext());

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusDeleted;
                apiResponse.Message = AppMessage.BankAccountDisconnectSuccessful;

                return Ok(apiResponse);
            }
            catch (MemberBankAccountNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPatch("marketing-ref")]
        public async Task<ActionResult<ApiMessageResponse>> UpdateCommsPreferences(
            [FromBody] CommsPreferencesRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
                var UpdateCommsPreferencesModel = _mapper.Map<UpdateCommsPreferencesModel>(request, opts =>
                {
                    opts.Items[Constant.Mapper.PersonId] = personId;
                    opts.Items[Constant.Mapper.MemberId] = memberId;
                });

                await _memberService.UpdateCommsPreferences(UpdateCommsPreferencesModel);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
                apiResponse.Message = AppMessage.GenericPreferencesUpdated;

                return Ok(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (Exception e)
            {
                _lambdaContext.Logger.LogLine("Exception thrown during UpdateCommsPreferences: " + e.ToString());

                return StatusCode(500);
            }
        }

        [HttpGet("marketing-ref")]
        public async Task<ActionResult<MemberCommsPreferencesInfo>> GetMemberSubscribeNewsLetters()
        {
            try
            {
                var response = await _memberService.GetCommsPreferences(await GetMemberIdFromContext());
                return response;
            }
            catch (MemberNotFoundException ex)
            {
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                };

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("otp")]
        public async Task<ActionResult<ApiMessageResponse>> SendOtp()
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                var mobile = await _memberService.SendMemberMobileOtp(await GetMemberIdFromContext());

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusSent;
                apiResponse.Message = string.Format(AppMessage.OtpSuccessMessage, mobile);

                return Ok(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        /*Note API parameter: dateFrom, dateTo format: "yyyy-MM-dd", ex: 2015-05-05
                           orderBy is contains by list ['Date'], default = 'Date'
                           sortDirection is contains by list ['Desc', 'Asc'], default = 'Desc'
                           limit default = 20
                           offset default = 0
                           searchText default empty
                           */

        [HttpGet("click-history")]
        public async Task<ActionResult<Paging<MemberClicksHistoryResult>>> GetMemberClicksHistory(string searchText,
            string dateFrom,
            string dateTo, int limit = Constant.DefaultLimit, int offset = Constant.DefaultOffset,
            string orderBy = Constant.DefaultOrderByField, string sortDirection = Constant.DefaultSortDirection)
        {
            try
            {
                var pagingResults = await _memberClicksHistoryService.GetMemberClicksHistory(await GetMemberIdFromContext(),
                    limit, offset,
                    searchText, dateFrom, dateTo, orderBy, sortDirection);

                return pagingResults;
            }
            catch (InvalidQueryConditionException ex)
            {
                var mapStatus = new Dictionary<string, string>
                {
                    {Util.GetDescriptionFromEnum(QueryConditionFields.Limit), "LIMIT"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.Offset), "OFFSET"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom), "DATE_FROM"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateTo), "DATE_TO"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.OrderBy), "ORDER_BY"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.SorDirection), "SORT_DIRECTION"}
                };
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Message = ex.Message
                };

                foreach (var pair in mapStatus.Where(pair => apiResponse.Message.StartsWith(pair.Key)))
                {
                    apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, pair.Value);
                    break;
                }

                return BadRequest(apiResponse);
            }
        }

        /*Note API parameter: dateFrom, dateTo format: "yyyy-MM-dd", ex: 2015-05-05
                              searchText default empty
                         */

        [HttpGet("click-history/count")]
        public async Task<ActionResult<TotalCountResponse>> GetMemberClicksHistoryTotalCount(string searchText,
            string dateFrom, string dateTo)
        {
            try
            {
                return await _memberClicksHistoryService.GetMemberClicksHistoryTotalCount(await GetMemberIdFromContext(),
                    searchText, dateFrom, dateTo);
            }
            catch (InvalidQueryConditionException ex)
            {
                var mapStatus = new Dictionary<string, string>
                {
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateFrom), "DATE_FROM"},
                    {Util.GetDescriptionFromEnum(QueryConditionFields.DateTo), "DATE_TO"}
                };
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Message = ex.Message
                };

                foreach (var pair in mapStatus.Where(pair => apiResponse.Message.StartsWith(pair.Key)))
                {
                    apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, pair.Value);
                    break;
                }

                return BadRequest(apiResponse);
            }
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<ApiMessageResponse>> Withdraw([FromBody] WithdrawRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberRedeemService.Withdraw(await GetMemberIdFromContext(), request.Amount, request.PaymentMethod,
                    request.MobileOtp);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusInserted;
                apiResponse.Message = AppMessage.WithdrawSuccess;

                return Ok(apiResponse);
            }
            catch (InvalidPaymentMethodException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "PAYMENT_METHOD");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNoAvailableBalanceException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNoAvailableBalance;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidMobileOtpException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "MOBILE_OTP");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidAmountException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "AMOUNT");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNotRedeemException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusMemberNotRedeem;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberPaypalException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "PAYPAL");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<ApiMessageResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
                await _memberService.ChangePassword(personId, memberId, request.NewPassword, request.MobileOtp);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
                apiResponse.Message = string.Format(AppMessage.UpdateObjectSuccessful, "Password");

                return Ok(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidPasswordException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "PASSWORD");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidMobileOtpException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "MOBILE_OTP");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpDelete("close-account")]
        public async Task<ActionResult<ApiMessageResponse>> CloseAccount()
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
                await _memberService.CloseMemberAccount(new CloseMemberAccountModel { PersonId = personId, MemberId = memberId });

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusDeleted;
                apiResponse.Message = AppMessage.AccountCloseSuccessful;

                return Ok(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("paypal")]
        public async Task<ActionResult<LinkedPaypalAccountInfo>> GetLinkedPaypalAccount()
        {
            try
            {
                return await _memberPaypalAccountService.GetLinkedPaypalAccount(await GetMemberIdFromContext());
            }
            catch (MemberPaypalException ex)
            {
                var apiResponse = new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                };
                return BadRequest(apiResponse);
            }
        }

        [HttpDelete("paypal")]
        public async Task<ActionResult<ApiMessageResponse>> UnlinkMemberPaypalAccount()
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberPaypalAccountService.UnlinkMemberPaypalAccount(await GetMemberIdFromContext());

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusDeleted;
                apiResponse.Message = AppMessage.PaypalAccountUnlinkSuccessful;

                return Ok(apiResponse);
            }
            catch (MemberPaypalException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPost("paypal")]
        public async Task<ActionResult<ApiMessageResponse>> LinkMemberPaypalAccount(
            [FromBody] Dictionary<string, string> requests)
        {
            requests.TryGetValue("code", out string code);
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberPaypalAccountService.LinkMemberPaypalAccount(await GetMemberIdFromContext(), code);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusLinked, "PAYPAL");
                apiResponse.Message = AppMessage.PaypalAccountLinkSuccessful;

                return Ok(apiResponse);
            }
            catch (PaypalAuthorizationCodeUnauthorizedException ex)
            {
                _lambdaContext.Logger.LogLine($"ERROR: {ex.Message}");
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.Unauthorized;
                apiResponse.Message = AppMessage.PaypalAuthorizationCodeUnauthorized;

                return BadRequest(apiResponse);
            }
            catch (DuplicatePaypalAccountException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusDuplicated;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (PaypalAccountHasNotBeenVerifiedException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusUnverified, "PAYPAL");
                apiResponse.Message = $"{string.Format(AppMessage.ApiResponseStatusUnverified, "PAYPAL")} - {ex.Message}";

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("paypal-connect-url")]
        public ActionResult<PaypalConnectUrlInfo> PaypalConnectUrl(string redirectUri,
            string state = "cr-paypal-link")
        {
            try
            {
                return _memberPaypalAccountService.GetPaypalConnectUrl(redirectUri, state);
            }
            catch (InvalidUriException ex)
            {
                return BadRequest(new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = string.Format(AppMessage.ApiResponseStatusInvalid, "REDIRECT_URI"),
                    Message = ex.Message
                });
            }
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            return Ok(new ApiMessageResponse()
            {
                Code = (int)HttpStatusCode.OK,
                Status = string.Format(AppMessage.ApiResponseStatusVerified, "EMAIL"),
                Message = await _memberService.VerifyEmail(request.Code)
            });
        }

        [HttpGet("send-verification-email")]
        public async Task<ActionResult<ApiMessageResponse>> SendVerificationEmail()
        {
            var apiResponse = new ApiMessageResponse();

            try
            {
                if (await _memberService.SendVerificationEmail(await GetMemberIdFromContext()))
                {
                    apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                    apiResponse.Status = AppMessage.ApiResponseStatusSent;
                    apiResponse.Message = AppMessage.EmailVerificationSent;

                    return Ok(apiResponse);
                }

                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusVerified, "EMAIL");
                apiResponse.Message = AppMessage.EmailVerified;

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("send-update-mobile-link")]
        public async Task<ActionResult<ApiMessageResponse>> SendUpdateMobileLink()
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberService.SendUpdateMobileLink(await GetMemberIdFromContext());

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusSent;
                apiResponse.Message = AppMessage.EmailOrphanMobileUpdatingSent;

                return Ok(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPost("update-mobile-with-code")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiMessageResponse>> UpdateMobileWithCode(
            [FromBody] UpdateMobileWithCodeRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberService.UpdateMobileWithCode(request.Code, request.Mobile);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUpdated;
                apiResponse.Message = string.Format(AppMessage.UpdateObjectSuccessful, "Your mobile");

                return Ok(apiResponse);
            }
            catch (TokenExpiredException ex)
            {
                apiResponse.Code = HttpStatusCode.Unauthorized.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusExpired, "TOKEN");
                apiResponse.Message = ex.Message;

                return Unauthorized(apiResponse);
            }
            catch (InvalidMobileNumberException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "MOBILE_OTP");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (UnauthorizedException ex)
            {
                apiResponse.Code = HttpStatusCode.Unauthorized.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusUnauthorized;
                apiResponse.Message = ex.Message;

                return Unauthorized(apiResponse);
            }
            catch (DuplicateMobileNumberException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusAlreadyInUsed, "MOBILE");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("check-update-mobile-link/{code}")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> CheckMobileLinkWithCode([FromRoute] string code)
        {
            await _memberService.CheckMobileLinkWithCode(code);
            return Ok();
        }

        [HttpPost("check-bsb")]
        public async Task<ActionResult<ApiMessageResponse>> CheckBsb([FromBody] CheckBsbRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _validationService.ValidateBsb(request.Bsb);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusValid, "BSB");
                apiResponse.Message = "Ok";

                return Ok(apiResponse);
            }
            catch (BankAccountValidationException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "BSB");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpPatch("notifier-status")]
        public async Task<ActionResult<ApiMessageResponse>> UpdateInstallNotifier(
            [FromBody] InstallNotifierRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
            var installNotifierModel = _mapper.Map<InstallNotifierModel>(request, opts =>
            {
                opts.Items[Constant.Mapper.PersonId] = personId;
                opts.Items[Constant.Mapper.MemberId] = memberId;
            });

            await _memberService.UpdateInstallNotifier(installNotifierModel);
            return Ok();
        }

        [HttpPost("feedback")]
        public async Task<ActionResult<ApiMessageResponse>> Feedback([FromBody] FeedbackRequest request)
        {
            var apiResponse = new ApiMessageResponse();
            try
            {
                await _memberService.FeedBack(await GetMemberIdFromContext(), request.Feedback, request.AppVersion,
                    request.DeviceModel, request.OperatingSystem, request.BuildNumber);

                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusSent;
                apiResponse.Message = AppMessage.FeedbackSent;

                return Ok(apiResponse);
            }
            catch (InvalidFeedbackException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "FEEDBACK");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidAppVersionException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "APP_VERSION");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidDeviceModelException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "DEVICE_MODEL");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidOperatingSystemException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "OPERATING_SYSTEM");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (InvalidBuildNumberException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInvalid, "BUILD_NUMBER");
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (MemberNotFoundException ex)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = AppMessage.ApiResponseStatusNotFound;
                apiResponse.Message = ex.Message;

                return BadRequest(apiResponse);
            }
            catch (NullReferenceException)
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpGet("category/fav")]
        public async Task<ActionResult<List<int>>> GetFavouriteCategories()
        {
            return await _memberFavouriteCategoryService.GetFavouriteCategoriesAsync(await GetMemberIdFromContext());
        }

        [HttpPost("category/fav")]
        public async Task<ActionResult<ApiMessageResponse>> SetFavouriteCategories([FromBody] int[] categoryIds)
        {
            var apiResponse = new ApiMessageResponse();

            if (categoryIds != null)
            {
                await _memberFavouriteCategoryService.SetFavouriteCategoriesAsync(await GetMemberIdFromContext(), categoryIds);
                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInserted);
                apiResponse.Message = string.Format(AppMessage.FavAdded);
                return Ok(apiResponse);
            }
            else
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpPost("fav")]
        public async Task<ActionResult<ApiMessageResponse>> AddFav([FromBody] MemberFavouriteRequestMerchant request)
        {
            var apiResponse = new ApiMessageResponse();

            if (request != null)
            {
                await _memberFavouriteService.AddFavouriteAsync(await GetMemberIdFromContext(), request);
                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInserted);
                apiResponse.Message = string.Format(AppMessage.FavAdded);
                return Ok(apiResponse);
            }
            else
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpPost("favs")]
        public async Task<ActionResult<ApiMessageResponse>> AddFavs([FromBody] MemberFavouriteRequest request)
        {
            var apiResponse = new ApiMessageResponse();

            if (request != null && request.Merchants != null && request.Merchants.Any())
            {
                await _memberFavouriteService.SetFavouritesAsync(await GetMemberIdFromContext(), request);
                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusInserted);
                apiResponse.Message = string.Format(AppMessage.FavAdded);
                return Ok(apiResponse);
            }
            else
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpGet("favs")]
        public async Task<ActionResult<List<MerchantResult>>> Favs()
        {
            return await _memberFavouriteService.GetAllFavouriteAsync(await _requestContext.GetMemberIdFromContext(), _requestContext.CognitoId);
        }

        [HttpDelete("fav")]
        public async Task<ActionResult<ApiMessageResponse>> RemoveFav([FromBody] Dictionary<string, int> requests)
        {
            var apiResponse = new ApiMessageResponse();

            if (requests != null && requests.TryGetValue("merchantId", out var merchantId) && requests.Count == 1)
            {
                await _memberFavouriteService.RemoveFavouriteAsync(await GetMemberIdFromContext(), merchantId);
                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = string.Format(AppMessage.ApiResponseStatusDeleted);
                apiResponse.Message = string.Format(AppMessage.FavDeleted);
                return Ok(apiResponse);
            }
            else
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpGet("fav/{merchantId}")]
        public async Task<ActionResult<ApiMessageResponse>> IsFav([FromRoute] string merchantId)
        {
            var apiResponse = new ApiMessageResponse();

            if (merchantId != null && int.TryParse(merchantId, out var mId))
            {
                var fav = await _memberFavouriteService.IsFavouriteMarchantAsync(await GetMemberIdFromContext(), mId);
                apiResponse.Code = HttpStatusCode.OK.GetHashCode();
                apiResponse.Status = fav ? "FAVOURITE_YES" : "FAVOURITE_NO";
                apiResponse.Message = "Ok";
                return Ok(apiResponse);
            }
            else
            {
                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();
                apiResponse.Status = "INVALID_REQUEST";
                apiResponse.Message = "Bad request";
                return BadRequest(apiResponse);
            }
        }

        [HttpPost("communications-prompt-shown")]
        public async Task<ActionResult<ApiMessageResponse>> CommunicationsPromptShown([FromBody] CommsPromptShownRequest request)
        {
            var apiResponse = new ApiMessageResponse();

            try

            {
                if (Enum.TryParse(typeof(CommsPromptDismissalAction), request.Action, out var parsedAction))

                {
                    var (personId, memberId) = await GetPersonIdAndMemberIdFromContext();
                    var action = (parsedAction as CommsPromptDismissalAction?).Value;

                    var model = _mapper.Map<CommsPromptShownModel>(request, opts =>
                    {
                        opts.Items[Constant.Mapper.PersonId] = personId;
                        opts.Items[Constant.Mapper.MemberId] = memberId;
                        opts.Items[Constant.Mapper.CommsPromptDismissalAction] = action;
                    });

                    await _memberService.CommsPromptShown(model);

                    apiResponse.Code = HttpStatusCode.OK.GetHashCode();

                    apiResponse.Status = AppMessage.ApiResponseStatusUpdated;

                    apiResponse.Message = AppMessage.CommsPromptShown;

                    return Ok(apiResponse);
                }
                else

                {
                    apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();

                    apiResponse.Status = "INVALID_REQUEST";

                    apiResponse.Message = "Bad request";

                    return BadRequest(apiResponse);
                }
            }
            catch (Exception e)

            {
                _lambdaContext.Logger.LogLine(e.ToString());

                apiResponse.Code = HttpStatusCode.BadRequest.GetHashCode();

                apiResponse.Status = "INVALID_REQUEST";

                apiResponse.Message = e.Message;

                return BadRequest(apiResponse);
            }
        }

        [HttpGet("getmaskedmobile")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public async Task<ActionResult<string>> GetMaskedMobile()
        {
            try
            {
                var maskedMobile = await _memberService.GetMaskedMobileNumber(await GetMemberIdFromContext());
                return Ok(new ApiMessageResponse()
                {
                    Code = (int)HttpStatusCode.OK,
                    Message = maskedMobile,
                    Status = string.Format(AppMessage.ApiResponseStatusValid, "MASKEDMOBILE")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("gethashedemail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        public async Task<ActionResult<string>> GetHashedEmail()
        {
            try
            {
                var hashedEmail = await _memberService.GetHashedSurveyEmail(await GetMemberIdFromContext());
                return Ok(new ApiMessageResponse()
                {
                    Code = (int)HttpStatusCode.OK,
                    Message = hashedEmail,
                    Status = string.Format(AppMessage.ApiResponseStatusValid, "HASHEDEMAIL")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageResponse
                {
                    Code = HttpStatusCode.BadRequest.GetHashCode(),
                    Status = AppMessage.ApiResponseStatusNotFound,
                    Message = ex.Message
                });
            }
        }
    }
}