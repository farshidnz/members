using SettingsAPI.Extensions;
using SettingsAPI.Model.Enum;
using System.Text;

namespace SettingsAPI.Helper
{
    public static class CommissionHelper
    {
        public static string GetCommissionString(int clientProgramTypeId, int tierCommTypeId, decimal clientCommission, decimal rate, bool? isFlatRate, int tierTypeId, string rewardName)
        {
            if (clientProgramTypeId != (int)ClientProgramTypeEnum.PointsProgram)
            {
                return GetCashrewardsCommissionString(tierCommTypeId, clientCommission, tierTypeId, isFlatRate, rewardName);
            }

            return GetPointsCommissionString(tierCommTypeId, clientCommission, rate, isFlatRate, rewardName);
        }

        private static string GetPointsCommissionString(int tierCommTypeId, decimal clientCommission, decimal rate, bool? isFlatRate, string rewardName)
        {
            var sb = new StringBuilder();

            var isDollarType = tierCommTypeId == (int)TierCommTypeEnum.Dollar;
            var commissionPts = isDollarType ? clientCommission * rate : clientCommission / 100 * rate;
            commissionPts = commissionPts.RoundToTwoDecimalPlaces();

            if (isFlatRate ?? true)
            {
                sb.Append($"{commissionPts.ToString("G29")} {rewardName}");
            }
            else
            {
                sb.Append($"Up to {commissionPts.ToString("G29")} {rewardName}");
            }

            if (!isDollarType)
            {
                sb.Append("/$");
            }

            return sb.ToString();
        }

        private static string GetCashrewardsCommissionString(int tierCommTypeId, decimal clientCommission, int tierTypeId, bool? isFlatRate, string rewardName)
        {
            var sb = new StringBuilder();
            BuildCashrewardsCommisionString_Pre(sb, tierTypeId, isFlatRate);

            if (tierCommTypeId == (int)TierCommTypeEnum.Dollar)
            {
                sb.Append($"${clientCommission.RoundToTwoDecimalPlaces().ToString("G29")}");
            }
            else
            {
                sb.Append($"{clientCommission.RoundToTwoDecimalPlaces().ToString("G29")}%");
            }

            BuildCashrewardsCommisionString_Post(sb, tierTypeId, rewardName);

            return sb.ToString();
        }

        private static void BuildCashrewardsCommisionString_Pre(StringBuilder sb, int tierTypeId, bool? isFlatRate)
        {
            if (tierTypeId == (int)TierTypeEnum.Discount)
            {
                sb.Append("Save ");
                return;
            }

            if (tierTypeId == (int)TierTypeEnum.MaxDiscount)
            {
                sb.Append("Save up to ");
                return;
            }

            if (isFlatRate ?? false)
            {
                sb.Append(string.Empty);
            }
            else
            {
                sb.Append("Up to ");
            }
        }

        private static void BuildCashrewardsCommisionString_Post(StringBuilder sb, int tierTypeId, string rewardName)
        {
            if (tierTypeId == (int)TierTypeEnum.Discount)
            {
                return;
            }
            if (tierTypeId == (int)TierTypeEnum.MaxDiscount)
            {
                return;
            }

            sb.Append($" {rewardName}");
        }
    }
}