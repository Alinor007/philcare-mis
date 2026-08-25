using System.Globalization;
using System.Net;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Infrastructure.Email;

namespace philcare.Api.Features.Finance.Donations.Emails;

/// <summary>
/// Builds the subject/HTML/text bodies for donation confirmation and void-notice emails. Kept
/// separate from the outbox/dispatcher so the actual delivery mechanism can be swapped without
/// touching content, and so content can be unit-tested without a DbContext or IResend.
///
/// Content mirrors the client-side PDF receipt (frontend/src/features/finance/donations/receipt.ts)
/// so a donor who gets the email and later requests a PDF sees the same document: PhilCare green
/// (#0f4b3a), "Official Donation Receipt", donor/amount/channel/purpose field order.
///
/// Every free-text value (donor name, purpose, program/project, notes) is officer- or donor-entered
/// and gets HTML-encoded before being interpolated into the body — this is an email sent to an
/// external inbox, so unencoded input would be an injection vector.
/// </summary>
public static class DonationEmailComposer
{
    private const string BrandColor = "#0f4b3a";
    private const string BrandTint = "#ecfdf5";
    private const string AmberAccent = "#d2201e";
    private const string AmberTint = "#fffbeb";

    public static (string Subject, string Html, string Text) ComposeConfirmation(
        Donation donation, Donor donor, Fund fund, EmailOptions options)
    {
        var receiptLabel = donation.ReceiptNo ?? $"#{donation.Id}";
        var subject = $"Thank you for your donation — Receipt {receiptLabel}";

        var whatItSupports = !string.IsNullOrWhiteSpace(donation.ProgramOrProject)
            ? donation.ProgramOrProject
            : !string.IsNullOrWhiteSpace(donation.Purpose)
                ? donation.Purpose
                : fund.Name;

        var primaryAmount = FormatMoney(donation.AmountOriginal, donation.Currency);
        var isForeignCurrency = !string.Equals(donation.Currency, "PHP", StringComparison.OrdinalIgnoreCase);

        var rows = new List<(string Label, string? Value)>
        {
            ("Receipt No.", donation.ReceiptNo),
            ("Date received", donation.DateReceived.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)),
        };

        if (isForeignCurrency)
        {
            rows.Add(("Equivalent in PHP", FormatMoney(donation.AmountPhp, "PHP")));
        }

        rows.Add(("Payment channel", donation.Channel));

        if (!string.IsNullOrWhiteSpace(donation.TransactionRef))
        {
            rows.Add(("Payment reference", donation.TransactionRef));
        }

        rows.Add(("Supports", whatItSupports ?? fund.Name));

        var html = BuildHtml(
            preheader: $"Receipt {receiptLabel} — {primaryAmount} received. Thank you for supporting {options.OrganizationName}.",
            badge: "Official Receipt",
            badgeIcon: "&#10003;",
            greeting: $"Dear {Enc(donor.Name)},",
            intro: $"Thank you for your generous donation to {Enc(options.OrganizationName)}. " +
                   "We're pleased to confirm it has been received and recorded — this email serves as your official receipt.",
            heroLabel: "Donation received",
            heroValue: Enc(primaryAmount),
            heroSub: $"Receipt {Enc(receiptLabel)}",
            rows: rows,
            calloutTitle: "What happens next",
            calloutBody: $"Your gift is directed to <strong>{Enc(whatItSupports ?? fund.Name)}</strong>, in line with " +
                         "the purpose you specified. Our team allocates and reports on funds through our standard " +
                         "governance process, and this record is kept on file for our annual reporting.",
            accent: BrandColor,
            accentTint: BrandTint,
            options: options);

        var text = BuildText(
            greeting: $"Dear {donor.Name},",
            intro: $"Thank you for your generous donation to {options.OrganizationName}. " +
                   "We're pleased to confirm it has been received and recorded — this email serves as your official receipt.",
            heroLabel: "DONATION RECEIVED",
            heroValue: primaryAmount,
            rows: rows,
            calloutTitle: "WHAT HAPPENS NEXT",
            calloutBody: $"Your gift is directed to {whatItSupports ?? fund.Name}, in line with the purpose you " +
                         "specified when giving.",
            options: options);

        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) ComposeVoided(Donation donation, Donor donor, EmailOptions options)
    {
        var receiptLabel = donation.ReceiptNo ?? $"#{donation.Id}";
        var subject = $"Update on your donation — Receipt {receiptLabel}";

        var primaryAmount = FormatMoney(donation.AmountOriginal, donation.Currency);

        var rows = new List<(string Label, string? Value)>
        {
            ("Receipt No.", donation.ReceiptNo),
            ("Date received", donation.DateReceived.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)),
            ("Original amount", primaryAmount),
        };

        var html = BuildHtml(
            preheader: $"Receipt {receiptLabel} has been voided in {options.OrganizationName}'s records.",
            badge: "Record Correction",
            badgeIcon: "&#9998;",
            greeting: $"Dear {Enc(donor.Name)},",
            intro: $"We're writing to let you know that the donation below has been voided in " +
                   $"{Enc(options.OrganizationName)}'s records — typically because it was recorded in error " +
                   "or needed correction.",
            heroLabel: "Voided donation",
            heroValue: Enc(primaryAmount),
            heroSub: $"Receipt {Enc(receiptLabel)}",
            rows: rows,
            calloutTitle: "Think this is a mistake?",
            calloutBody: "If you believe this donation should still stand, or if you have any questions, " +
                         $"contact us at <a href=\"mailto:{Enc(options.SupportEmail)}\" style=\"color:{AmberAccent};font-weight:600;\">{Enc(options.SupportEmail)}</a> " +
                         "and we'll help sort it out.",
            accent: AmberAccent,
            accentTint: AmberTint,
            options: options);

        var text = BuildText(
            greeting: $"Dear {donor.Name},",
            intro: $"We're writing to let you know that the donation below has been voided in " +
                   $"{options.OrganizationName}'s records — typically because it was recorded in error " +
                   "or needed correction.",
            heroLabel: "VOIDED DONATION",
            heroValue: primaryAmount,
            rows: rows,
            calloutTitle: "THINK THIS IS A MISTAKE?",
            calloutBody: $"If you believe this should still stand, contact us at {options.SupportEmail}.",
            options: options);

        return (subject, html, text);
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        var culture = CultureInfo.GetCultureInfo("en-PH");
        return string.Equals(currency, "PHP", StringComparison.OrdinalIgnoreCase)
            ? amount.ToString("C2", culture)
            : $"{currency} {amount.ToString("N2", culture)}";
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>
    /// Table-based, inline-styled layout. Email clients (Outlook especially) don't support flexbox,
    /// grid, or embedded stylesheets, so structure is nested tables and every rule is inline.
    /// </summary>
    private static string BuildHtml(
        string preheader,
        string badge,
        string badgeIcon,
        string greeting,
        string intro,
        string heroLabel,
        string heroValue,
        string heroSub,
        List<(string Label, string? Value)> rows,
        string calloutTitle,
        string calloutBody,
        string accent,
        string accentTint,
        EmailOptions options)
    {
        var visibleRows = rows.Where(r => !string.IsNullOrWhiteSpace(r.Value)).ToList();

        var rowsHtml = string.Join("\n", visibleRows.Select((r, i) => $"""
            <tr>
              <td style="padding:11px 16px;color:#6b7280;font-size:13px;line-height:1.4;white-space:nowrap;vertical-align:top;{BorderTop(i)}">{Enc(r.Label)}</td>
              <td align="right" style="padding:11px 16px;font-size:14px;line-height:1.4;font-weight:600;color:#111827;vertical-align:top;{BorderTop(i)}">{Enc(r.Value)}</td>
            </tr>
            """));

        var supportLine = string.IsNullOrWhiteSpace(options.SupportPhone)
            ? $"""<a href="mailto:{Enc(options.SupportEmail)}" style="color:{accent};font-weight:600;text-decoration:none;">{Enc(options.SupportEmail)}</a>"""
            : $"""<a href="mailto:{Enc(options.SupportEmail)}" style="color:{accent};font-weight:600;text-decoration:none;">{Enc(options.SupportEmail)}</a> &nbsp;·&nbsp; {Enc(options.SupportPhone)}""";

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>{Enc(badge)}</title>
            </head>
            <body style="margin:0;padding:0;background:#f1f5f4;-webkit-font-smoothing:antialiased;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;height:0;width:0;">{Enc(preheader)}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f1f5f4;">
                <tr><td align="center" style="padding:32px 12px;">

                  <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 1px 3px rgba(16,24,40,0.08);">

                    <tr><td style="background:{accent};padding:20px 28px;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr>
                        <td style="vertical-align:middle;">
                          <div style="color:#ffffff;font-size:17px;font-weight:700;letter-spacing:-0.2px;line-height:1.25;">{Enc(options.OrganizationName)}</div>
                          <div style="color:#ffffff;font-size:10.5px;font-weight:500;letter-spacing:0.3px;line-height:1.4;opacity:0.7;">Philippines Humanitarian Care</div>
                        </td>
                        <td align="right" style="vertical-align:middle;">
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="background:rgba(255,255,255,0.16);border-radius:999px;border:1px solid rgba(255,255,255,0.4);"><tr>
                            <td style="padding:7px 14px;color:#ffffff;font-size:10px;font-weight:700;letter-spacing:0.9px;text-transform:uppercase;white-space:nowrap;">{badgeIcon}&nbsp; {Enc(badge)}</td>
                          </tr></table>
                        </td>
                      </tr></table>
                    </td></tr>

                    <tr><td style="padding:32px 32px 0;">
                      <p style="margin:0 0 14px;font-size:16px;font-weight:600;color:#111827;">{greeting}</p>
                      <p style="margin:0 0 26px;font-size:14px;line-height:1.65;color:#4b5563;">{intro}</p>
                    </td></tr>

                    <tr><td style="padding:0 32px;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{accentTint};border-radius:12px;">
                        <tr><td align="center" style="padding:26px 20px;">
                          <div style="font-size:11px;font-weight:600;letter-spacing:1px;text-transform:uppercase;color:{accent};opacity:0.75;">{Enc(heroLabel)}</div>
                          <div style="font-size:34px;line-height:1.15;font-weight:700;color:{accent};padding:8px 0 6px;letter-spacing:-0.5px;">{heroValue}</div>
                          <div style="font-size:12px;color:#6b7280;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;">{heroSub}</div>
                        </td></tr>
                      </table>
                    </td></tr>

                    <tr><td style="padding:26px 32px 0;">
                      <div style="font-size:11px;font-weight:600;letter-spacing:0.8px;text-transform:uppercase;color:#9ca3af;padding-bottom:10px;">Donation details</div>
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border:1px solid #e5e7eb;border-radius:10px;">
                        {rowsHtml}
                      </table>
                    </td></tr>

                    <tr><td style="padding:24px 32px 0;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f9fafb;border-left:3px solid {accent};border-radius:0 8px 8px 0;">
                        <tr><td style="padding:16px 18px;">
                          <div style="font-size:13px;font-weight:700;color:#111827;padding-bottom:5px;">{Enc(calloutTitle)}</div>
                          <div style="font-size:13px;line-height:1.65;color:#4b5563;">{calloutBody}</div>
                        </td></tr>
                      </table>
                    </td></tr>

                    <tr><td style="padding:24px 32px 32px;">
                      <div style="font-size:13px;line-height:1.65;color:#6b7280;">
                        Questions about this donation? Reach us at<br>{supportLine}
                      </div>
                    </td></tr>

                    <tr><td style="padding:18px 32px;background:#f9fafb;border-top:1px solid #eef0f2;">
                      <div style="font-size:11px;line-height:1.6;color:#9ca3af;">
                        This is an automated message from {Enc(options.OrganizationName)} MIS. Please keep this email for your records.
                      </div>
                    </td></tr>

                  </table>

                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BorderTop(int index) => index == 0 ? "" : "border-top:1px solid #f0f1f3;";

    private static string BuildText(
        string greeting,
        string intro,
        string heroLabel,
        string heroValue,
        List<(string Label, string? Value)> rows,
        string calloutTitle,
        string calloutBody,
        EmailOptions options)
    {
        var visibleRows = rows.Where(r => !string.IsNullOrWhiteSpace(r.Value)).ToList();
        var width = visibleRows.Count == 0 ? 0 : visibleRows.Max(r => r.Label.Length);
        var lines = visibleRows.Select(r => $"  {r.Label.PadRight(width)}   {r.Value}");

        return $"""
            {greeting}

            {StripHtml(intro)}

            {heroLabel}
            {heroValue}

            DONATION DETAILS
            {string.Join("\n", lines)}

            {calloutTitle}
            {StripHtml(calloutBody)}

            Questions? Contact {options.SupportEmail}{(string.IsNullOrWhiteSpace(options.SupportPhone) ? "" : $" or {options.SupportPhone}")}.

            — {options.OrganizationName}
            This is an automated message from {options.OrganizationName} MIS. Please keep it for your records.
            """;
    }

    private static string StripHtml(string value) => System.Text.RegularExpressions.Regex
        .Replace(value.Replace("<br/>", "\n").Replace("<br>", "\n"), "<.*?>", string.Empty);
}
