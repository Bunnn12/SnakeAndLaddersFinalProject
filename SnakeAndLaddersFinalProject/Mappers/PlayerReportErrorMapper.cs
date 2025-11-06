using System;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Mappers
{
    public static class PlayerReportErrorMapper
    {
        public static string GetMessageForCode(string faultCode)
        {
            if (string.IsNullOrWhiteSpace(faultCode))
            {
                return Lang.ReportGenericErrorMessage;
            }

            if (string.Equals(faultCode, "REPORT_DUPLICATE", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorDuplicate;
            }

            if (string.Equals(faultCode, "REPORT_INVALID_USER", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorInvalidUser;
            }

            if (string.Equals(faultCode, "REPORT_INVALID_REQUEST", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorInvalidRequest;
            }

            if (string.Equals(faultCode, "REPORT_INTERNAL_ERROR", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportGenericErrorMessage;
            }

            return Lang.ReportGenericErrorMessage;
        }
    }
}
