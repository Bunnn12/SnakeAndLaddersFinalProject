using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Mappers
{
    public static class PlayerReportErrorMapper
    {
        public static string GetMessageForCode(string faultCode)
        {
            if (string.IsNullOrWhiteSpace(faultCode))
            {
                return "Properties.Langs.Lang.ReportGenericErrorMessage";
            }

            if (string.Equals(faultCode, "REPORT_DUPLICATE", StringComparison.OrdinalIgnoreCase))
            {
                return "Properties.Langs.Lang.ReportErrorDuplicate";
            }

            if (string.Equals(faultCode, "REPORT_INVALID_USER", StringComparison.OrdinalIgnoreCase))
            {
                return "Properties.Langs.Lang.ReportErrorInvalidUser";
            }

            if (string.Equals(faultCode, "REPORT_INVALID_REQUEST", StringComparison.OrdinalIgnoreCase))
            {
                return "Properties.Langs.Lang.ReportErrorInvalidRequest";
            }

            if (string.Equals(faultCode, "REPORT_INTERNAL_ERROR", StringComparison.OrdinalIgnoreCase))
            {
                return "Properties.Langs.Lang.ReportGenericErrorMessage";
            }

            return "Properties.Langs.Lang.ReportGenericErrorMessage";
        }
    }
}
