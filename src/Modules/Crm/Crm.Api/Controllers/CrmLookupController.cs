using Crm.Application.DTOs;
using Const = Crm.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Crm.Api.Controllers;

/// <summary>
/// Returns all CRM constant/lookup lists so the frontend can populate
/// dropdowns without embedding magic strings in the UI.
/// </summary>
[ApiController]
[Route("api/crm-lookup")]
[Authorize]
public class CrmLookupController : ControllerBase
{
    /// <summary>
    /// Get all CRM lookup lists in a single request.
    /// Ideal for bootstrapping a form that uses multiple dropdowns.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CrmLookupsDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var dto = new CrmLookupsDto
        {
            LeadStatuses = ToLookup(Const.LeadStatusConstants.All, FormatLabel),
            Salutations = ToLookup(Const.SalutationConstants.All, FormatLabel),
            LeadSources = ToLookup(Const.LeadSourceConstants.All, FormatLeadSource),
            Industries = ToLookup(Const.IndustryConstants.All, FormatLabel),
            Countries = ToLookup(Const.CountryConstants.All, FormatLabel),
            StatesProvinces = ToLookup(Const.StateProvinceConstants.All, FormatLabel),
            AccountTypes = ToLookup(Const.AccountTypeConstants.All, FormatLabel),
            DealStages = ToLookup(Const.DealStageConstants.All, FormatDealStage),
            ForecastCategories = ToLookup(Const.ForecastCategoryConstants.All, FormatForecastCategory),
            CaseStatuses = ToLookup(Const.CaseStatusConstants.All, FormatCaseStatus),
            CaseOrigins = ToLookup(Const.CaseOriginConstants.All, FormatLabel),
            CasePriorities = ToLookup(Const.CasePriorityConstants.All, FormatLabel),
        };

        return Ok(new ApiResponse<CrmLookupsDto>
        {
            Success = true,
            Data = dto,
            Message = "CRM lookups retrieved successfully"
        });
    }

    // ?? Individual endpoints (useful when only one list is needed) ????????????

    [HttpGet("lead-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLeadStatuses() =>
        OkList(Const.LeadStatusConstants.All, FormatLabel);

    [HttpGet("salutations")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSalutations() =>
        OkList(Const.SalutationConstants.All, FormatLabel);

    [HttpGet("lead-sources")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetLeadSources() =>
        OkList(Const.LeadSourceConstants.All, FormatLeadSource);

    [HttpGet("industries")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetIndustries() =>
        OkList(Const.IndustryConstants.All, FormatLabel);

    [HttpGet("countries")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCountries() =>
        OkList(Const.CountryConstants.All, FormatLabel);

    [HttpGet("states-provinces")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetStatesProvinces() =>
        OkList(Const.StateProvinceConstants.All, FormatLabel);

    [HttpGet("account-types")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetAccountTypes() =>
        OkList(Const.AccountTypeConstants.All, FormatLabel);

    [HttpGet("deal-stages")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetDealStages() =>
        OkList(Const.DealStageConstants.All, FormatDealStage);

    [HttpGet("forecast-categories")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetForecastCategories() =>
        OkList(Const.ForecastCategoryConstants.All, FormatForecastCategory);

    [HttpGet("case-statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCaseStatuses() =>
        OkList(Const.CaseStatusConstants.All, FormatCaseStatus);

    [HttpGet("case-origins")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCaseOrigins() =>
        OkList(Const.CaseOriginConstants.All, FormatLabel);

    [HttpGet("case-priorities")]
    [ProducesResponseType(typeof(ApiResponse<List<CrmLookupItemDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCasePriorities() =>
        OkList(Const.CasePriorityConstants.All, FormatLabel);

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private IActionResult OkList(string[] values, Func<string, string> labelFn) =>
        Ok(new ApiResponse<List<CrmLookupItemDto>>
        {
            Success = true,
            Data = ToLookup(values, labelFn),
            Message = "Lookup values retrieved successfully"
        });

    private static List<CrmLookupItemDto> ToLookup(string[] values, Func<string, string> labelFn) =>
        values.Select(v => new CrmLookupItemDto { Value = v, Label = labelFn(v) }).ToList();

    private static string FormatLabel(string v) =>
        System.Text.RegularExpressions.Regex.Replace(v, "([a-z])([A-Z])", "$1 $2");

    private static string FormatLeadSource(string v) => v switch
    {
        Const.LeadSourceConstants.Ads => "Ads",
        Const.LeadSourceConstants.Web => "Web",
        Const.LeadSourceConstants.WordOfMouth => "Word of Mouth",
        Const.LeadSourceConstants.Email => "Email",
        Const.LeadSourceConstants.Phone => "Phone",
        Const.LeadSourceConstants.SocialMedia => "Social Media",
        Const.LeadSourceConstants.Referral => "Referral",
        Const.LeadSourceConstants.Webinar => "Webinar",
        Const.LeadSourceConstants.Event => "Event",
        Const.LeadSourceConstants.Other => "Other",
        _ => v
    };

    private static string FormatDealStage(string v) => v switch
    {
        Const.DealStageConstants.Qualify => "Qualify",
        Const.DealStageConstants.MeetAndPresent => "Meet and Present",
        Const.DealStageConstants.Propose => "Propose",
        Const.DealStageConstants.Negotiate => "Negotiate",
        Const.DealStageConstants.ClosedWon => "Closed Won",
        Const.DealStageConstants.ClosedLost => "Closed Lost",
        _ => v
    };

    private static string FormatForecastCategory(string v) => v switch
    {
        Const.ForecastCategoryConstants.Omitted => "Omitted",
        Const.ForecastCategoryConstants.Pipeline => "Pipeline",
        Const.ForecastCategoryConstants.BestCase => "Best Case",
        Const.ForecastCategoryConstants.Commit => "Commit",
        Const.ForecastCategoryConstants.Closed => "Closed",
        _ => v
    };

    private static string FormatCaseStatus(string v) => v switch
    {
        Const.CaseStatusConstants.New => "New",
        Const.CaseStatusConstants.Working => "Working",
        Const.CaseStatusConstants.WaitingOnCustomer => "Waiting on Customer",
        Const.CaseStatusConstants.Escalated => "Escalated",
        Const.CaseStatusConstants.Closed => "Closed",
        _ => v
    };
}
