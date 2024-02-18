using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SettingsAPI.Common;
using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Error;
using SettingsAPI.Model;
using SettingsAPI.Model.Dto;
using SettingsAPI.Model.Enum;
using SettingsAPI.Model.Rest;
using SettingsAPI.Service.Interface;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class MemberService : IMemberService
    {
        private readonly IOptions<Settings> _settings;
        private readonly ShopGoContext _context;
        private readonly ReadOnlyShopGoContext _readOnlyContext;
        private readonly IEncryptionService _encryptionService;
        private readonly IMemberBalanceService _memberBalanceService;
        private readonly IMobileOptService _mobileOptService;
        private readonly IValidationService _validationService;
        private readonly IEmailService _emailService;
        private readonly ITimeService _timeService;
        private readonly IAwsService _awsService;
        private readonly IDatabase _redisDb;
        private readonly IMapper _mapper;
        private readonly IEntityAuditService _entityAuditService;
        private readonly IFieldAuditService _fieldAuditService;
        private readonly IFeatureToggleService _featureToggleService;

        public MemberService(IOptions<Settings> settings, 
            ShopGoContext context, 
            ReadOnlyShopGoContext readOnlyContext, 
            IEncryptionService encryptionService,
            IMemberBalanceService memberBalanceService, 
            IMobileOptService mobileOptService,
            IValidationService validationService, 
            IEmailService emailService, 
            ITimeService timeService,
            IAwsService awsService,
            IDatabase redisDb,
            IMapper mapper,
            IEntityAuditService entityAuditService, 
            IFieldAuditService fieldAuditService, 
            IFeatureToggleService featureToggleService)
        {
            _settings = settings;
            _context = context;
            _readOnlyContext = readOnlyContext;
            _encryptionService = encryptionService;
            _memberBalanceService = memberBalanceService;
            _mobileOptService = mobileOptService;
            _validationService = validationService;
            _emailService = emailService;
            _timeService = timeService;
            _awsService = awsService;
            _redisDb = redisDb;
            _mapper = mapper;
            _entityAuditService = entityAuditService;
            _fieldAuditService = fieldAuditService;
            _featureToggleService = featureToggleService;
        }


        public async Task UpdateDetails(int? personId, int memberId, string mobileOtp, string dob, string gender,
            string firstName, string lastName, string postCode)
        {
            DateTime? dateOfBirth = null;

            var dateOfBirthStr = dob;
            if (dateOfBirthStr != null)
                dateOfBirth = DateTime.ParseExact(dateOfBirthStr, Constant.DateOfBirthFormat,
                    CultureInfo.InvariantCulture);

            var (cRMember, cRMemberBeforUpdate) = personId.HasValue ?
                await UpdateMembersDetailByPersonId(personId.Value, mobileOtp, dateOfBirth, gender, firstName, lastName, postCode) :
                await UpdateMemberDetailByMemberId(memberId, mobileOtp, dateOfBirth, gender, firstName, lastName, postCode);

            //Send sns
            await SendMemberUpdatedEvent(cRMember, cRMemberBeforUpdate);

            //Send sqs
            await SendQueueCognito(cRMember, string.Empty);

            //Log audit fields 
            var fieldAudits = _fieldAuditService.GetUpdateMemberFieldAudits(cRMember, cRMemberBeforUpdate);
            if (fieldAudits != null && fieldAudits.Any())
            await _entityAuditService.LogEntityAudit(cRMember.MemberId, "Member", AuditActionType.SettingsUpdated, fieldAudits);

        }
        private async Task<(Member CrMember, Member CrMemberBeforeUpdate)> UpdateMembersDetailByPersonId(int personId, string mobileOtp, DateTime? dateOfBirth, string gender,
            string firstName, string lastName, string postCode)
        {
            var (members, cRMember) = await GetMembersByPersonId(personId);

            _validationService.ValidateOtp(cRMember.Mobile, mobileOtp, cRMember.Email);
            var oldMember = _mapper.Map<Member>(cRMember);
            
            members.ToList().ForEach(member =>
                       AssignDetailsToMember(member, dateOfBirth, gender, firstName, lastName, postCode));
            await _context.SaveChangesAsync();

            return (cRMember, oldMember);
        }

        private async Task<(Member CrMember, Member CrMemberBeforeUpdate)> UpdateMemberDetailByMemberId(int memberId, string mobileOtp, DateTime? dateOfBirth, string gender,
            string firstName, string lastName, string postCode)
        {
            var member = await ValidateAndGetCurrentMember(memberId);
            _validationService.ValidateOtp(member.Mobile, mobileOtp, member.Email);
            var oldMember = _mapper.Map<Member>(member);
            AssignDetailsToMember(member, dateOfBirth, gender, firstName, lastName, postCode);
            await _context.SaveChangesAsync();

            return (member, oldMember);
        }

        private Member AssignDetailsToMember(Member member, DateTime? dateOfBirth, string gender,
            string firstName, string lastName, string postCode)
        {
            member.FirstName = firstName;
            member.LastName = lastName;
            member.PostCode = postCode;

            if (dateOfBirth.HasValue)
                member.DateOfBirth = dateOfBirth;

            var genderEnum = Util.GetEnumFromDescription<Gender>(gender);
            member.Gender = genderEnum.GetHashCode();

            return member;
        }



        /// <summary>
        /// Update mobile number
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="mobileOtp"></param>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        public async Task UpdateMobileNumber(MemberModel model)
        {
            model.MobileNumber = Util.SanitizeMobilePhone(model.MobileNumber);

            IEnumerable<Member> members = await MembersByPersonId(model.PersonId);
            Member CRMember = members.FirstOrDefault(m => m.ClientId == Constant.Clients.CashRewards);
            _validationService.ValidateOtp(CRMember.Mobile, model.MobileOtp, CRMember.Email);

            // Check phone number use by other member
            await CheckPhoneNumberUseByOtherMember(model.MemberId, model.MobileNumber, model.PersonId);

            var oldMember = _mapper.Map<Member>(members.FirstOrDefault());
            
            //updating mobile to same mobile
            if (members.All(m => m.Mobile.Equals(model.MobileNumber)))
            {
                throw new BadRequestException("User mobile is the same");
            }

            string hashedMobile = _encryptionService.EncryptWithSalt(model.MobileNumber, members.FirstOrDefault().SaltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2).Replace(@"\", Constant.SaltCharacterReplace3);
            string mobileSha256 = _encryptionService.ComputeSha256Hash(model.MobileNumber);

            foreach (var memberList in members)
            {
                memberList.HashedMobile = hashedMobile;
                memberList.Mobile = model.MobileNumber;
                memberList.MobileSha256 = mobileSha256;
            }

            await _context.SaveChangesAsync();
            //Send sns
            await SendMemberUpdatedEvent(CRMember, oldMember);
            //Send sqs

            //Log audit fields 
            var fieldAudit = _fieldAuditService.GetUpdateMobileFieldAudit(model.MobileNumber, oldMember.Mobile);
            if (fieldAudit != null)
                await _entityAuditService.LogEntityAudit(model.MemberId, "Member", 
                AuditActionType.SettingsUpdated, 
                new List<FieldAudit> { fieldAudit });
        }

        public async Task UpdateEmail(MemberModel model)
        {
            var regex = new Regex(Constant.RegexEmailWhiteList);
            IEnumerable<Member> members = await MembersByPersonId(model.PersonId);
            Member CRMember = members.FirstOrDefault(m => m.ClientId == Constant.Clients.CashRewards);

            _validationService.ValidateOtp(CRMember.Mobile, model.MobileOtp, CRMember.Email);

            //Check email use by other member
            await CheckEmailUseByOtherMember(model);

            var oldMember = new Member
            {
                Email = CRMember.Email,
                FacebookUsername = CRMember.FacebookUsername
            };

            //Email exists
            if (members.All(members => members.Email == model.Email))
            {
                throw new BadRequestException("User email is the same");
            }

            var hashedEmail = _encryptionService.EncryptWithSalt(model.Email, CRMember.SaltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2)
                .Replace(@"\", Constant.SaltCharacterReplace3);

            foreach (var memberList in members)
            {
                memberList.Email = model.Email;
                memberList.HashedEmail = hashedEmail;
                // ReSharper disable once SimplifyConditionalTernaryExpression
                memberList.IsValidated = _featureToggleService.IsEnable(FeatureFlags.WHITE_LIST_TEST_MEMBERS) ? regex.IsMatch(memberList.Email.ToLower()) : false;
            }

            await _context.SaveChangesAsync();
            //Sendmail
            await _emailService.SendVerificationEmail(CRMember.MemberId, CRMember.FirstName, model.Email, hashedEmail);
            //Send sns
            await SendMemberUpdatedEvent(CRMember, oldMember);
            //Send sqs
            await SendQueueCognito(CRMember, string.Empty);
        }

        public async Task<MemberDetails> GetMember(int? personId, int memberId)
        {
            var member = await ValidateAndGetCurrentMemberReadOnly(memberId);

            var person = await GetPerson(personId);

            var membershipDetail = await GetMembershipInfo(memberId);

            var memberBalanceViews = await _memberBalanceService.GetBalanceViews(membershipDetail?.Items?.Select(p => p.MemberId).ToArray(), useCache: true);

            var shouldShowCommunicationsPrompt = await ShouldShowCommunicationsPrompt(member);

            var memberDto = new MemberDetails
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                Balance = Math.Round(memberBalanceViews?.Sum(p => p.TotalBalance) ?? 0, 2),
                MemberId = member.MemberId,
                Email = member.Email,
                IsRisky = member.IsRisky ? 1 : 0,
                LifetimeRewards = memberBalanceViews?.Sum(p => p.LifetimeRewards),
                NewMemberId = member.MemberNewId.ToString().ToUpper(),
                Mobile = Util.ConvertPhoneToInternationFormat(member.Mobile),
                Gender = member.Gender == null
                    ? null
                    : Util.GetDescriptionFromEnum((Gender)member.Gender),
                Comment = member.Comment,
                ClientId = member.ClientId,
                AvailableBalance = Math.Round(memberBalanceViews?.Sum(p => p.AvailableBalance) ?? 0, 2),
                RedeemBalance = Math.Round(memberBalanceViews?.Sum(p => p.RedeemBalance) ?? 0, 2),
                PostCode = member.PostCode,
                DateOfBirth = member.DateOfBirth?.ToString(Constant.DateOfBirthFormat, CultureInfo.InvariantCulture),
                ReceiveNewsLetter = member.ReceiveNewsLetter,
                IsValidated = member.IsValidated,
                InstallNotifier = member.InstallNotifier,
                ShowCommunicationsPrompt = shouldShowCommunicationsPrompt,
                IsPremium = person?.PremiumStatus == Constant.PremiumStatus.Enrolled,
                PremiumStatus = person?.PremiumStatus ?? 0
            };

            if (member.DateJoinedUtc != null)
                memberDto.DaysFromJoined = (int)(DateTime.UtcNow - member.DateJoinedUtc.Value).TotalDays;

            var welcomeBonusTransactionDto = GetMemberWelcomeBonus(memberId).FirstOrDefault();
            if (welcomeBonusTransactionDto != null)

                memberDto.WelComeBonus = new MemberInfoWelcomeBonus
                {
                    Amount = welcomeBonusTransactionDto.Amount,
                    IsRedeemed = welcomeBonusTransactionDto.TransactionStatus ==
                                 TransactionStatus.Confirmed.GetHashCode()
                };

            return memberDto;
        }

        public async Task<CognitoMember> GetCashrewardsCognitoMember(string cognitoId)
        {
            var cognitoMembers = await _readOnlyContext.CognitoMember
                .Where(cm => cm.CognitoId == cognitoId && cm.Status)
                .ToListAsync();

            var memberIds = cognitoMembers.Select(cm => cm.MemberId);

            var cashrewardsMember = await _readOnlyContext.Member
                .Where(m => memberIds.Contains(m.MemberId) && m.ClientId == Constant.Clients.CashRewards)
                .FirstOrDefaultAsync();

            return cognitoMembers.FirstOrDefault(cm => cm.MemberId == cashrewardsMember?.MemberId);
        }

        public async Task UpdateCommsPreferences(UpdateCommsPreferencesModel model)
        {
            var members = model.PersonId.HasValue ? await GetAllMembersByPersonId(model.PersonId.Value) :
                                        await GetMembersById(model.MemberId);

            var member = members.Where(m => m.MemberId == model.MemberId).FirstOrDefault();
            if (member == null)
                throw new MemberNotFoundException();

            var CrMemberBeforeUpdate = member == null ? throw new MemberNotFoundException() :
                                                    _mapper.Map<Member>(member);
            members.ToList().ForEach(m => AssignCommsPreferencesToMember(m, model));

            await _context.SaveChangesAsync();
            await SendMemberUpdatedEvent(member, CrMemberBeforeUpdate);
            await SendQueueCognito(member, string.Empty);
        }

        private void AssignCommsPreferencesToMember(Member member, UpdateCommsPreferencesModel model)
        {
            member.SmsConsent = model.SubscribeSMS ?? member.SmsConsent;
            member.ReceiveNewsLetter = model.SubscribeNewsletters ?? member.ReceiveNewsLetter;
            member.AppNotificationConsent = model.SubscribeAppNotifications ?? member.AppNotificationConsent;
            member.CommsPromptShownCount = Constant.MaxCommsPromptShownCount;
        }

        public async Task<MemberCommsPreferencesInfo> GetCommsPreferences(int memberId)
        {
            var member = await ValidateAndGetCurrentMemberReadOnly(memberId);
            return new MemberCommsPreferencesInfo
            {
                SubscribeNewsletters = member.ReceiveNewsLetter,
                SubscribeSMS = member.SmsConsent,
                SubscribeAppNotifications = member.AppNotificationConsent
            };
        }

        public async Task CommsPromptShown(CommsPromptShownModel model)
        {
            var members = (await GetMembers(model.PersonId, model.MemberId)).ToList();
            var member = members.FirstOrDefault(m => m.MemberId == model.MemberId);
            if (member == null)
                throw new MemberNotFoundException();

            var delayUntilNextPrompt = TimeSpan.FromHours(24);

            members.ToList().ForEach(async m =>
          {
              switch (model.Action)
              {
                  case CommsPromptDismissalAction.Close:
                      m.CommsPromptShownCount += 1;
                      await _redisDb.StringSetAsync($"comms_prompt_shown:{model.MemberId}", "true", delayUntilNextPrompt);
                      break;
                  case CommsPromptDismissalAction.Review:
                      m.CommsPromptShownCount = Constant.MaxCommsPromptShownCount;
                      break;
              }

          });

            await _context.SaveChangesAsync();
        }

        public async Task<string> SendMemberMobileOtp(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMemberReadOnly(memberId);

            _mobileOptService.SendMobileOtp(memberStore.Mobile, memberStore.Email);
            return Util.ToMaskedMobileNumber(memberStore.Mobile);
        }

        public async Task ChangePassword(int? personId, int memberId, string newPassword,
            string mobileOtp)
        {
            var cRMember = personId.HasValue ?
                        await ChangePasswordByPersonId(personId.Value, newPassword, mobileOtp) :
                        await ChangePasswordByMemberId(memberId, newPassword, mobileOtp);


            await _emailService.SendPasswordUpdateEmail(cRMember.Email, cRMember.FirstName);
            //Send Sqs message
            await SendQueueCognito(cRMember, newPassword);
        }

        private async Task<Member> ChangePasswordByPersonId(int personId, string newPassword, string mobileOtp)
        {
            var (members, cRMember) = await GetMembersByPersonId(personId);
            _validationService.ValidateOtp(cRMember.Mobile, mobileOtp, cRMember.Email);

            var userPassword = _encryptionService.EncryptWithSalt(newPassword, members.FirstOrDefault().SaltKey);
            members.ToList().ForEach(m =>
                        m.UserPassword = userPassword);
            await _context.SaveChangesAsync();

            return cRMember;
        }

        private async Task<Member> ChangePasswordByMemberId(int memberId, string newPassword, string mobileOtp)
        {
            var cRMember = await ValidateAndGetCurrentMember(memberId);
            _validationService.ValidateOtp(cRMember.Mobile, mobileOtp, cRMember.Email);
            cRMember.UserPassword = _encryptionService.EncryptWithSalt(newPassword, cRMember.SaltKey);
            await _context.SaveChangesAsync();
            return cRMember;
        }

        public async Task CloseMemberAccount(CloseMemberAccountModel model)
        {
            var members = await GetMembers(model.PersonId, model.MemberId);
            var member = members.FirstOrDefault(m => m.MemberId == model.MemberId);
            if (member == null)
                throw new MemberNotFoundException();

            var oldMember = _mapper.Map<Member>(member);

            members.ToList().ForEach(m =>
            {
                m.Status = StatusType.Deleted.GetHashCode();
                m.DateDeletedByMember = DateTime.Now;
                m.DateDeletedByMemberUtc = DateTime.UtcNow;
                m.ReceiveNewsLetter = false;
            });


            await _context.SaveChangesAsync();
            //Send sns
            await SendMemberUpdatedEvent(member, oldMember);
            //Send sqs
            await SendQueueCognito(member, string.Empty);
        }

        public async Task<string> VerifyEmail(string code)
        {
            _validationService.ValidateEmailVerificationCode(code, out var memberIdStr, out var hashedEmail);

            var memberId = int.Parse(memberIdStr);
            var memberStore = await ValidateAndGetCurrentMember(memberId);
            if (!memberStore.HashedEmail.Equals(hashedEmail))
                throw new InvalidEmailVerificationCodeException();

            var oldMember = new Member
            {
                Email = memberStore.Email,
                FacebookUsername = memberStore.FacebookUsername
            };
            IEnumerable<Member> members = memberStore.PersonId != null ? await GetAllMembersByPersonId(memberStore.PersonId.Value) : new List<Member> { memberStore };

            if (members.All(member => member.IsValidated)) return memberStore.Email;

            foreach (var memberList in members)
            {
                memberList.IsValidated = true;
            }

            await _context.SaveChangesAsync();
            //Send sns
            await SendMemberUpdatedEvent(memberStore, oldMember);
            //Send sqs
            await SendQueueCognito(memberStore, string.Empty);

            return memberStore.Email;
        }

        private string GetHashedEmail(Member memberStore)
        {
            return _encryptionService.EncryptWithSalt(memberStore.Email, memberStore.SaltKey)
                                     .Replace("+", Constant.SaltCharacterReplace1)
                                     .Replace("/", Constant.SaltCharacterReplace2).Replace(@"\", Constant.SaltCharacterReplace3);
        }

        public async Task<bool> SendVerificationEmail(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMember(memberId);
            if (memberStore.IsValidated)
                return false;

            if (string.IsNullOrEmpty(memberStore.HashedEmail))
            {
                memberStore.HashedEmail = GetHashedEmail(memberStore);
                await _context.SaveChangesAsync();
            }

            await _emailService.SendVerificationEmail(memberId, memberStore.FirstName, memberStore.Email,
                memberStore.HashedEmail);

            return true;
        }

        public async Task<bool> SendSignupAutomatedVerificationEmail(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMember(memberId);
            bool verificationAlreadySent = memberStore.SignupVerificationEmailSentStatus != (int)SignupAutomatedVerificationEmailStatus.NotSent;
            if (memberStore.IsValidated || verificationAlreadySent)
                return false;

            memberStore.SignupVerificationEmailSentStatus = (int)SignupAutomatedVerificationEmailStatus.Sending;
            await _context.SaveChangesAsync();

            if (string.IsNullOrEmpty(memberStore.HashedEmail))
            {
                memberStore.HashedEmail = GetHashedEmail(memberStore);
            }

            try
            {
                await _emailService.SendVerificationEmail(memberId, memberStore.FirstName, memberStore.Email, memberStore.HashedEmail);
                memberStore.SignupVerificationEmailSentStatus = (int)SignupAutomatedVerificationEmailStatus.Sent;
            }
            catch
            {
                memberStore.SignupVerificationEmailSentStatus = (int)SignupAutomatedVerificationEmailStatus.NotSent;
            }
            await _context.SaveChangesAsync();

            return memberStore.SignupVerificationEmailSentStatus == (int)SignupAutomatedVerificationEmailStatus.Sent;
        }

        public async Task SendUpdateMobileLink(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMember(memberId);
            if (string.IsNullOrWhiteSpace(memberStore.Mobile))
            {
                await _emailService.SendOrphanMobileEmail(memberId, memberStore.FirstName, memberStore.Email, memberStore.Mobile);
            }
            else
            {
                await _emailService.SendContactMSEmail(memberStore.FirstName, memberStore.Email);
            }
        }

        public async Task UpdateMobileWithCode(string code, string mobile)
        {
            var codeChunk = code.Split("$");

            var fromCurrentTimestamp = long.Parse(codeChunk[1]);
            var currentTimestamp = _timeService.GetCurrentTimestamp();
            if ((currentTimestamp - fromCurrentTimestamp) > Constant.TokenTimeToLive)
                throw new TokenExpiredException();

            //Validate mobile
            _validationService.ValidatePhone(mobile);

            //Check match code
            var fromMemberId = int.Parse(codeChunk[0]);
            var memberStore = await ValidateAndGetCurrentMember(fromMemberId);

            var data = new StringBuilder($"{fromMemberId}|{memberStore.Mobile}|{fromCurrentTimestamp}").ToString();
            var fromHashCode = _encryptionService.EncryptWithSalt(data, _settings.Value.SaltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2)
                .Replace(@"\", Constant.SaltCharacterReplace3);

            var hashCode = codeChunk[2];
            if (!fromHashCode.Equals(hashCode))
                throw new UnauthorizedException();

            // Sanitize mobile phone to make sure that db will have phone number +## #########
            mobile = Util.SanitizeMobilePhone(mobile);

            //Check mobile use by other member
            await CheckPhoneNumberUseByOtherMember(fromMemberId, mobile);

            var oldMember = new Member
            {
                Email = memberStore.Email,
                FacebookUsername = memberStore.FacebookUsername,
                Mobile = memberStore.Mobile
            };

            //If mobile existed send sns and return
            if (mobile.Equals(memberStore.Mobile))
            {
                return;
            }

            //Update new mobile
            var hashedMobile = _encryptionService.EncryptWithSalt(mobile, memberStore.SaltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2).Replace(@"\", Constant.SaltCharacterReplace3);

            var members = await GetMembers(null, fromMemberId);
            foreach (var member in members)
            {
                member.MobileSha256 = _encryptionService.ComputeSha256Hash(mobile);
                member.HashedMobile = hashedMobile;
                member.Mobile = mobile;
            }

            await _context.SaveChangesAsync();
            //Send sns
            await SendMemberUpdatedEvent(memberStore, oldMember);
            //Send sqs
            await SendQueueCognito(memberStore, string.Empty);

            //Log audit fields 
            var fieldAudit = _fieldAuditService.GetUpdateMobileFieldAudit(mobile, oldMember.Mobile);
            if (fieldAudit != null)
            await _entityAuditService.LogEntityAudit(fromMemberId, "Member",
                AuditActionType.SettingsUpdated,
                new List<FieldAudit> { fieldAudit });
        }

        public async Task CheckMobileLinkWithCode(string code)
        {
            _validationService.ValidateCheckMobileLinkCode(code);
            string[] codeChunk = code.Split("$");

            //Check match code first
            int fromMemberId = int.Parse(codeChunk[0]);
            long fromCurrentTimestamp = long.Parse(codeChunk[1]);
            // check the timestamp
            long currentTimestamp = _timeService.GetCurrentTimestamp();
            if ((currentTimestamp - fromCurrentTimestamp) > Constant.TokenTimeToLive)
                throw new TokenExpiredException();

            Member memberStore = await ValidateAndGetCurrentMemberReadOnly(fromMemberId);

            string data = $"{fromMemberId}|{memberStore.Mobile}|{fromCurrentTimestamp}";
            string fromHashCode = _encryptionService.EncryptWithSalt(data, _settings.Value.SaltKey)
                .Replace("+", Constant.SaltCharacterReplace1)
                .Replace("/", Constant.SaltCharacterReplace2)
                .Replace(@"\", Constant.SaltCharacterReplace3);

            string hashCode = codeChunk[2];
            if (!fromHashCode.Equals(hashCode))
                throw new BadRequestException("Bad hashcode");
        }

        public async Task UpdateInstallNotifier(InstallNotifierModel model)
        {
            var members = await GetMembers(model.PersonId, model.MemberId);
            var member = members.FirstOrDefault(m => m.MemberId == model.MemberId);
            if (member == null)
                throw new MemberNotFoundException();

            var oldMember = new Member
            {
                Email = member.Email,
                FacebookUsername = member.FacebookUsername
            };
            //If not change status send sns and return
            if (member.InstallNotifier == model.Status)
            {
                return;
            }

            members.ToList().ForEach(m => m.InstallNotifier = model.Status);
            await _context.SaveChangesAsync();

            //Send sns
            await SendMemberUpdatedEvent(member, oldMember);
            //Send sqs
            await SendQueueCognito(member, string.Empty);
        }

        public async Task FeedBack(int memberId, string feedback, string appVersion, string deviceModel,
            string operatingSystem, string buildNumber)
        {
            //Validate
            _validationService.ValidateFeedback(feedback);
            _validationService.ValidateAppVersion(appVersion);
            _validationService.ValidateDeviceModel(deviceModel);
            _validationService.ValidateOperatingSystem(operatingSystem);
            _validationService.ValidateBuildNumber(buildNumber);

            var memberStore = await ValidateAndGetCurrentMemberReadOnly(memberId);
            var emailStore = memberStore.Email;
            var firstName = memberStore.FirstName;
            var lastName = memberStore.LastName;

            //Send to member email
            await _emailService.SendFeedbackToCustomerEmail(emailStore, firstName, feedback);
            //Send to feedback
            await _emailService.SendFeedbackToCashrewards(memberId, firstName, lastName, emailStore, feedback,
                appVersion, deviceModel, operatingSystem, buildNumber);
        }

        public virtual IEnumerable<WelcomeBonusTransaction> GetMemberWelcomeBonus(int memberId)
        {
            const int crMerchantId = 1001211;
            var trans = (from tran in _readOnlyContext.Transaction
                         join tranTier in _readOnlyContext.TransactionTier on tran.TransactionId equals tranTier.TransactionId
                         join merchantTier in _readOnlyContext.MerchantTier on tranTier.MerchantTierId equals merchantTier.MerchantTierId
                         where tran.MemberId == memberId && tran.MerchantId == crMerchantId &&
                               merchantTier.TierName.StartsWith(Constant.WelcomeBonusStringPrefix) &&
                               merchantTier.TierReference.StartsWith(Constant.WelcomeBonusStringPrefix)
                         select new WelcomeBonusTransaction
                         {
                             TransactionId = tran.TransactionId,
                             Amount = tranTier.MemberCommissionValueAud,
                             TransactionStatus = tran.TransactionStatusId
                         }).ToList();

            return trans;
        }

        public async Task<Member> ValidateAndGetCurrentMember(int memberId)
        {
            var memberStore = await _context.Member
                .Where(member => member.MemberId == memberId && (member.Status == (int)StatusType.Active || member.Status == (int)StatusType.NotAssigned))
                .FirstOrDefaultAsync();

            if (memberStore == null)
                throw new MemberNotFoundException();

            return memberStore;
        }
        public async Task<Member> ValidateAndGetCurrentMemberReadOnly(int memberId)
        {
            var memberStore = await _readOnlyContext.Member
                .Where(member => member.MemberId == memberId && (member.Status == (int)StatusType.Active || member.Status == (int)StatusType.NotAssigned))
                .FirstOrDefaultAsync();

            if (memberStore == null)
                throw new MemberNotFoundException();

            return memberStore;
        }

        /// <summary>
        /// Gets the members by Person id
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Member>> MembersByPersonId(int personId)
        {
            var memberStore = await _context.Member
                .Where(member => member.PersonId == personId &&
                                (member.Status == (int)StatusType.Active ||
                                 member.Status == (int)StatusType.NotAssigned))
                .ToListAsync();

            if (!memberStore.Any())
                throw new MemberNotFoundException();

            return memberStore;
        }

        private async Task<(IEnumerable<Member> members, Member cRMember)> GetMembersByPersonId(int personId)
        {
            var members = await _context.Member
                .Where(member => member.PersonId == personId &&
                                (member.Status == StatusType.Active.GetHashCode() ||
                                 member.Status == StatusType.NotAssigned.GetHashCode()))
                .ToListAsync();

            if (!members.Any())
                throw new MemberNotFoundException();

            var CRMember = members.Where(m => m.ClientId == Constant.Clients.CashRewards).FirstOrDefault();
            if (CRMember == null)
                throw new MemberNotFoundException();

            return (members, CRMember);
        }

        private async Task<IEnumerable<Member>> GetMembers(int? personId, int memberId)
        {
            if (personId.HasValue)
                return await GetAllMembersByPersonId(personId.Value);

            return await GetMembersById(memberId);
        }
        private async Task<IEnumerable<Member>> GetAllMembersByPersonId(int personId)
        {
            var members = await _context.Member
                .Where(member => member.PersonId == personId &&
                                (member.Status == StatusType.Active.GetHashCode() ||
                                 member.Status == StatusType.NotAssigned.GetHashCode()))
                .ToListAsync();
            return members;
        }

        private async Task<IEnumerable<Member>> GetMembersById(int memberId)
        {
            IEnumerable<Member> members;
            var cognitoId = await GetCognitoIdForMember(memberId);

            if (!string.IsNullOrEmpty(cognitoId))
            {
                members = await _context.CognitoMember
                            .Join(_context.Member,
                                    cm => cm.MemberId,
                                    m => m.MemberId,
                                    (cm, m) => new { Memer = m, CognitoId = cm.CognitoId })
                            .Where(m => m.CognitoId == cognitoId)
                            .Select(m => m.Memer)
                            .ToListAsync();
            }
            else
            {
                members = await _context.Member
               .Where(member => member.MemberId == memberId &&
                               (member.Status == StatusType.Active.GetHashCode() ||
                                member.Status == StatusType.NotAssigned.GetHashCode())).ToListAsync();
            }

            if (members == null)
                throw new MemberNotFoundException();

            return members;
        }

        private async Task<string> GetCognitoIdForMember(int memberId)
        {
            var cognitoMembers = await _context.CognitoMember
                .FirstOrDefaultAsync(cm => cm.MemberId == memberId);

            return cognitoMembers?.CognitoId;
        }

        private async Task<Person> GetPerson(int? personId)
        {
            if (!personId.HasValue)
            {
                return null;
            }

            return await _readOnlyContext.Person
                .Where(person => person.PersonId == personId)
                .FirstOrDefaultAsync();
        }

        private async Task CheckPhoneNumberUseByOtherMember(int memberId, string mobile, int? personId = null)
        {
            IQueryable<Member> query = _context.Member;

            if (personId.HasValue)
                query = query.Where(member => member.PersonId != personId);
            else
                query = query.Where(member => member.MemberId != memberId);

            query = query.Where(member => member.Mobile == mobile);
            var memberStore = await query.FirstOrDefaultAsync();
            if (memberStore != null)
                throw new DuplicateMobileNumberException();
        }

        private async Task CheckEmailUseByOtherMember(MemberModel model)
        {
            IQueryable<Member> query = _context.Member;

            if (model.PersonId > 0)
                query = query.Where(member => member.PersonId != model.PersonId);
            else
                query = query.Where(member => member.MemberId != model.MemberId);

            query = query.Where(member => member.Email == model.Email);

            var memberStore = await query.FirstOrDefaultAsync();
            if (memberStore != null)
                throw new DuplicateEmailException();
        }

        private async Task SendMemberUpdatedEvent(Member newMember, Member oldMember)
        {
            var fbUserId = oldMember.FacebookUsername;
            if (string.IsNullOrWhiteSpace(fbUserId))
            {
                var memberUpdatedEvent = new MemberUpdatedEvent
                {
                    MemberId = newMember.MemberId,
                    Old_Email = oldMember.Email,
                    Email = newMember.Email,
                    FirstName = newMember.FirstName,
                    LastName = newMember.LastName,
                    ReceiveNewsLetter = newMember.ReceiveNewsLetter,
                    Status = newMember.Status,
                    IsValidated = newMember.IsValidated,
                    ExternalMemberId = newMember.MemberNewId.ToString(),
                    MobileNumber = newMember.Mobile,
                    ReferralSource = !string.IsNullOrEmpty(newMember.AccessCode) ? newMember.AccessCode : "Direct",
                    PlatformJoined = GetPlatformDetails(newMember.Source),
                    ClientId = newMember.ClientId,
                    AppNotificationConsent = newMember.AppNotificationConsent,
                    SmsConsent = newMember.SmsConsent
                };
                await _awsService.SendSnsMessage(memberUpdatedEvent);
                return;
            }

            //Send if member has fbUserId
            var memberUpdatedEventFb = new MemberUpdatedEventFb
            {
                MemberId = newMember.MemberId,
                Old_Email = oldMember.Email,
                Email = newMember.Email,
                FirstName = newMember.FirstName,
                LastName = newMember.LastName,
                ReceiveNewsLetter = newMember.ReceiveNewsLetter,
                Status = newMember.Status,
                IsValidated = newMember.IsValidated,
                ExternalMemberId = newMember.MemberNewId.ToString(),
                MobileNumber = newMember.Mobile,
                ReferralSource = newMember.AccessCode ?? "Direct",
                FBUserId = fbUserId,
                PlatformJoined = GetPlatformDetails(newMember.Source),
                ClientId = newMember.ClientId
            };
            await _awsService.SendSnsMessage(memberUpdatedEventFb);
        }

        private async Task SendQueueCognito(Member member, string plainPassword)
        {
            var cognitoMember = await ValidateAndGetCognitoMember(member.MemberId, _readOnlyContext);
            var messageContent = new CognitoMemberUpdateEvent
            {
                Email = member.Email,
                Password = plainPassword,
                PhoneNumber = (member.Mobile ?? string.Empty)?.Replace(" ", ""),
                FirstName = member.FirstName,
                LastName = member.LastName,
                PostCode = member.PostCode,
                AccessCode = member.AccessCode ?? string.Empty,
                MemberId = member.MemberId.ToString(),
                MemberNewId = member.MemberNewId.ToString(),
                Status = member.Status,
                CognitoId = (cognitoMember?.CognitoId) ?? string.Empty
            };
            await _awsService.SendSqsMessage(messageContent, null);
        }

        private static string GetPlatformDetails(int? source)
        {
            return source == 2 ? "app" : "web";
        }

        private static async Task<CognitoMember> ValidateAndGetCognitoMember(int memberId, ReadOnlyShopGoContext context)
        {
            if (memberId <= 0) throw new MemberNotFoundException();
            var data = await context.CognitoMember
                .Where(cognitoMember => cognitoMember.MemberId == memberId)
                .FirstOrDefaultAsync();

            return data;
        }

        private async Task<bool> ShouldShowCommunicationsPrompt(Member member)
        {
            var promptShownInLast24Hours = await _redisDb.StringGetAsync($"comms_prompt_shown:{member.MemberId}", CommandFlags.None);
            var cutoffJoinDate = new DateTime(2021, 5, 30);

            if (promptShownInLast24Hours.HasValue || member.DateJoined > cutoffJoinDate)
            {
                return false;
            }

            return member.CommsPromptShownCount < 2;
        }

        public async Task<MembershipDetail> GetMembershipInfo(int memberId)
        {
            var membershipCache = await _redisDb.StringGetAsync($"membership_CR_ANZ:{memberId}");

            if (membershipCache.HasValue)
            {
                return JsonConvert.DeserializeObject<MembershipDetail>(membershipCache);
            }

            var membership = await GetMembershipInfoByMemberId(memberId);
            var membershipCacheTime = TimeSpan.FromMinutes(10);
            await _redisDb.StringSetAsync($"membership_CR_ANZ:{memberId}", JsonConvert.SerializeObject(membership), membershipCacheTime);

            return membership;
        }

        private async Task<MembershipDetail> GetMembershipInfoByMemberId(int memberId)
        {
            var cognitoMember = await _readOnlyContext.CognitoMember.FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (cognitoMember == null)
            {
                return new MembershipDetail
                {
                    Items = new List<MemberShipItem> {
                        new MemberShipItem{ClientId= Constant.Clients.CashRewards, MemberId= memberId }
                        }
                };
            }

            var person = await GetPerson(cognitoMember.PersonId);
            var premiumStatus = person?.PremiumStatus ?? Constant.PremiumStatus.NotEnrolled;

            var membershipItems = await (from cogMem in _readOnlyContext.CognitoMember
                                         join mem in _readOnlyContext.Member on cogMem.MemberId equals mem.MemberId
                                         where cogMem.CognitoId == cognitoMember.CognitoId && Constant.ClientGroup.ANZPremium.Contains(mem.ClientId)
                                         select new MemberShipItem
                                         {
                                             MemberId = mem.MemberId,
                                             ClientId = mem.ClientId,
                                             PersonId = cogMem.PersonId,
                                             PremiumStatus = premiumStatus,
                                             DateJoined = mem.DateJoined
                                         }).ToListAsync();
            return new MembershipDetail { Items = membershipItems };
        }

        public async Task<string> GetMaskedMobileNumber(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMemberReadOnly(memberId);            
            return memberStore.Mobile != null ? Util.ToMaskedMobileNumber(memberStore.Mobile) : null;
        }

        public async Task<string> GetHashedSurveyEmail(int memberId)
        {
            var memberStore = await ValidateAndGetCurrentMemberReadOnly(memberId);
            return memberStore.Email != null ? Util.ToHashedSurveyEmail(memberStore.Email, _settings.Value.AskNicelySecret) : null;
        }

    }
}
