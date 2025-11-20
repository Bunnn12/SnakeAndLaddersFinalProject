using System;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Mappers
{
    public static class PlayerReportErrorMapper
    {
        private const string ERROR_REPORT_DUPLICATE = "REPORT_DUPLICATE";
        private const string ERROR_REPORT_INVALID_USER = "REPORT_INVALID_USER";
        private const string ERROR_REPORT_INVALID_REQUEST = "REPORT_INVALID_REQUEST";
        private const string ERROR_REPORT_INTERNAL = "REPORT_INTERNAL_ERROR";

        public static string GetMessageForCode(string faultCode)
        {
            if (string.IsNullOrWhiteSpace(faultCode))
            {
                return Lang.ReportGenericErrorMessage;
            }

            if (string.Equals(faultCode, ERROR_REPORT_DUPLICATE, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorDuplicate;
            }

            if (string.Equals(faultCode, ERROR_REPORT_INVALID_USER, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorInvalidUser;
            }

            if (string.Equals(faultCode, ERROR_REPORT_INVALID_REQUEST, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportErrorInvalidRequest;
            }

            if (string.Equals(faultCode, ERROR_REPORT_INTERNAL, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.ReportGenericErrorMessage;
            }

            return Lang.ReportGenericErrorMessage;
        }
    }
}
