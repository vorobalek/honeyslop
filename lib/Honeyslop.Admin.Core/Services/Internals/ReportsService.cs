using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class ReportsService(
    IAbuseReportRepository abuseReportRepository,
    ICountryLookup countryLookup,
    IOptions<AbuseReportOptions> reportOptions,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IReportsService
{
    public async Task<ReportsPageDto> GetPageAsync(ReportsQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var total = await abuseReportRepository.CountAsync(cancellationToken);
        var (order, direction) = query.Sort.ToOrder();
        var reports = await abuseReportRepository.GetPageAsync(order, direction, page, IReportsService.PageSize, cancellationToken);
        return new ReportsPageDto(
            new PagedDto<ReportDto>([.. reports.Select(x => x.ToRow(access.Viewer))], total, page, IReportsService.PageSize),
            countryLookup.FindAll(reports.Select(x => x.ClientIp)),
            asOf);
    }

    public async Task<ReportDetailsDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var report = await abuseReportRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (report is null)
            return null;
        var from = access.Viewer.CanOperate
            ? report.SentFrom ?? reportOptions.Value.ExpectedFrom(report.SenderName) ?? report.SenderName ?? "the address only"
            : null;
        return new ReportDetailsDto(
            report.ToRow(access.Viewer),
            countryLookup.Find(report.ClientIp),
            report.Body,
            report.MessageId,
            from,
            access.Viewer.CanOperate && report.SentFrom is null);
    }
}
