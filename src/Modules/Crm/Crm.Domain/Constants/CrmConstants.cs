namespace Crm.Domain.Constants;

/// <summary>
/// Lead status options - used in Lead.Status
/// </summary>
public static class LeadStatusConstants
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Nurtured = "Nurtured";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";

    public static readonly string[] All = [New, Contacted, Nurtured, Qualified, Unqualified];
}

/// <summary>
/// Salutation options - used in Contact.Salutation and Lead.Salutation
/// </summary>
public static class SalutationConstants
{
    public const string Mr = "Mr";
    public const string Mrs = "Mrs";
    public const string Ms = "Ms";
    public const string Dr = "Dr";
    public const string Prof = "Prof";
    public const string Mx = "Mx";

    public static readonly string[] All = [Mr, Mrs, Ms, Dr, Prof, Mx];
}

/// <summary>
/// Lead source options - used in Lead.Source
/// </summary>
public static class LeadSourceConstants
{
    public const string Ads = "Ads";
    public const string Web = "Web";
    public const string WordOfMouth = "WordOfMouth";
    public const string Email = "Email";
    public const string Phone = "Phone";
    public const string SocialMedia = "SocialMedia";
    public const string Referral = "Referral";
    public const string Webinar = "Webinar";
    public const string Event = "Event";
    public const string Other = "Other";

    public static readonly string[] All = [Ads, Web, WordOfMouth, Email, Phone, SocialMedia, Referral, Webinar, Event, Other];
}

/// <summary>
/// Account type options - used in Account.Type
/// </summary>
public static class AccountTypeConstants
{
    public const string Analyst = "Analyst";
    public const string Competitor = "Competitor";
    public const string Customer = "Customer";
    public const string Integrator = "Integrator";
    public const string Investor = "Investor";
    public const string Partner = "Partner";
    public const string Press = "Press";
    public const string Prospect = "Prospect";
    public const string Reseller = "Reseller";
    public const string Other = "Other";

    public static readonly string[] All = [Analyst, Competitor, Customer, Integrator, Investor, Partner, Press, Prospect, Reseller, Other];
}

/// <summary>
/// Deal stage options - used in Deal.Stage
/// </summary>
public static class DealStageConstants
{
    public const string Qualify = "Qualify";
    public const string MeetAndPresent = "MeetAndPresent";
    public const string Propose = "Propose";
    public const string Negotiate = "Negotiate";
    public const string ClosedWon = "ClosedWon";
    public const string ClosedLost = "ClosedLost";

    public static readonly string[] All = [Qualify, MeetAndPresent, Propose, Negotiate, ClosedWon, ClosedLost];
}

/// <summary>
/// Forecast category options - used in Forecast.Category
/// </summary>
public static class ForecastCategoryConstants
{
    public const string Omitted = "Omitted";
    public const string Pipeline = "Pipeline";
    public const string BestCase = "BestCase";
    public const string Commit = "Commit";
    public const string Closed = "Closed";

    public static readonly string[] All = [Omitted, Pipeline, BestCase, Commit, Closed];
}

/// <summary>
/// Case status options - used in Case.Status
/// </summary>
public static class CaseStatusConstants
{
    public const string New = "New";
    public const string Working = "Working";
    public const string WaitingOnCustomer = "WaitingOnCustomer";
    public const string Escalated = "Escalated";
    public const string Closed = "Closed";

    public static readonly string[] All = [New, Working, WaitingOnCustomer, Escalated, Closed];
}

/// <summary>
/// Case origin options - used in Case.Origin
/// </summary>
public static class CaseOriginConstants
{
    public const string Email = "Email";
    public const string Phone = "Phone";
    public const string Web = "Web";

    public static readonly string[] All = [Email, Phone, Web];
}

/// <summary>
/// Case priority options - used in Case.Priority
/// </summary>
public static class CasePriorityConstants
{
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";

    public static readonly string[] All = [High, Medium, Low];
}

/// <summary>
/// Industry options - used in Account.Industry and Lead.Industry
/// </summary>
public static class IndustryConstants
{
    public const string Agriculture = "Agriculture";
    public const string Apparel = "Apparel";
    public const string Banking = "Banking";
    public const string Biotechnology = "Biotechnology";
    public const string Chemicals = "Chemicals";
    public const string Communications = "Communications";
    public const string Construction = "Construction";
    public const string Consulting = "Consulting";
    public const string Education = "Education";
    public const string Electronics = "Electronics";
    public const string Energy = "Energy";
    public const string Engineering = "Engineering";
    public const string Entertainment = "Entertainment";
    public const string Environmental = "Environmental";
    public const string Finance = "Finance";
    public const string FoodBeverage = "FoodBeverage";
    public const string Government = "Government";
    public const string Healthcare = "Healthcare";
    public const string Hospitality = "Hospitality";
    public const string Insurance = "Insurance";
    public const string Machinery = "Machinery";
    public const string Manufacturing = "Manufacturing";
    public const string Media = "Media";
    public const string NotForProfit = "NotForProfit";
    public const string Recreation = "Recreation";
    public const string Retail = "Retail";
    public const string Shipping = "Shipping";
    public const string Technology = "Technology";
    public const string Telecommunications = "Telecommunications";
    public const string Transportation = "Transportation";
    public const string Utilities = "Utilities";
    public const string Other = "Other";

    public static readonly string[] All =
    [
        Agriculture, Apparel, Banking, Biotechnology, Chemicals, Communications, Construction,
        Consulting, Education, Electronics, Energy, Engineering, Entertainment, Environmental,
        Finance, FoodBeverage, Government, Healthcare, Hospitality, Insurance, Machinery,
        Manufacturing, Media, NotForProfit, Recreation, Retail, Shipping, Technology,
        Telecommunications, Transportation, Utilities, Other
    ];
}

/// <summary>
/// Country options - used in Address.Country and Lead.Country
/// </summary>
public static class CountryConstants
{
    public const string Afghanistan = "Afghanistan";
    public const string Albania = "Albania";
    public const string Algeria = "Algeria";
    public const string Argentina = "Argentina";
    public const string Australia = "Australia";
    public const string Austria = "Austria";
    public const string Bangladesh = "Bangladesh";
    public const string Belgium = "Belgium";
    public const string Brazil = "Brazil";
    public const string Canada = "Canada";
    public const string Chile = "Chile";
    public const string China = "China";
    public const string Colombia = "Colombia";
    public const string Denmark = "Denmark";
    public const string Egypt = "Egypt";
    public const string Finland = "Finland";
    public const string France = "France";
    public const string Germany = "Germany";
    public const string Ghana = "Ghana";
    public const string Greece = "Greece";
    public const string India = "India";
    public const string Indonesia = "Indonesia";
    public const string Iran = "Iran";
    public const string Iraq = "Iraq";
    public const string Ireland = "Ireland";
    public const string Israel = "Israel";
    public const string Italy = "Italy";
    public const string Japan = "Japan";
    public const string Jordan = "Jordan";
    public const string Kenya = "Kenya";
    public const string Kuwait = "Kuwait";
    public const string Lebanon = "Lebanon";
    public const string Malaysia = "Malaysia";
    public const string Mexico = "Mexico";
    public const string Morocco = "Morocco";
    public const string Netherlands = "Netherlands";
    public const string NewZealand = "NewZealand";
    public const string Nigeria = "Nigeria";
    public const string Norway = "Norway";
    public const string Pakistan = "Pakistan";
    public const string Philippines = "Philippines";
    public const string Poland = "Poland";
    public const string Portugal = "Portugal";
    public const string Qatar = "Qatar";
    public const string Russia = "Russia";
    public const string SaudiArabia = "SaudiArabia";
    public const string Singapore = "Singapore";
    public const string SouthAfrica = "SouthAfrica";
    public const string SouthKorea = "SouthKorea";
    public const string Spain = "Spain";
    public const string Sweden = "Sweden";
    public const string Switzerland = "Switzerland";
    public const string Thailand = "Thailand";
    public const string Turkey = "Turkey";
    public const string UnitedArabEmirates = "UnitedArabEmirates";
    public const string UnitedKingdom = "UnitedKingdom";
    public const string UnitedStates = "UnitedStates";
    public const string Vietnam = "Vietnam";

    public static readonly string[] All =
    [
        Afghanistan, Albania, Algeria, Argentina, Australia, Austria, Bangladesh, Belgium, Brazil,
        Canada, Chile, China, Colombia, Denmark, Egypt, Finland, France, Germany, Ghana, Greece,
        India, Indonesia, Iran, Iraq, Ireland, Israel, Italy, Japan, Jordan, Kenya, Kuwait, Lebanon,
        Malaysia, Mexico, Morocco, Netherlands, NewZealand, Nigeria, Norway, Pakistan, Philippines,
        Poland, Portugal, Qatar, Russia, SaudiArabia, Singapore, SouthAfrica, SouthKorea, Spain,
        Sweden, Switzerland, Thailand, Turkey, UnitedArabEmirates, UnitedKingdom, UnitedStates, Vietnam
    ];
}

/// <summary>
/// State/Province options - used in Address.State and Lead.State
/// </summary>
public static class StateProvinceConstants
{
    // United States
    public const string Alabama = "Alabama";
    public const string Alaska = "Alaska";
    public const string Arizona = "Arizona";
    public const string Arkansas = "Arkansas";
    public const string California = "California";
    public const string Colorado = "Colorado";
    public const string Connecticut = "Connecticut";
    public const string Delaware = "Delaware";
    public const string Florida = "Florida";
    public const string Georgia = "Georgia";
    public const string Hawaii = "Hawaii";
    public const string Idaho = "Idaho";
    public const string Illinois = "Illinois";
    public const string Indiana = "Indiana";
    public const string Iowa = "Iowa";
    public const string Kansas = "Kansas";
    public const string Kentucky = "Kentucky";
    public const string Louisiana = "Louisiana";
    public const string Maine = "Maine";
    public const string Maryland = "Maryland";
    public const string Massachusetts = "Massachusetts";
    public const string Michigan = "Michigan";
    public const string Minnesota = "Minnesota";
    public const string Mississippi = "Mississippi";
    public const string Missouri = "Missouri";
    public const string Montana = "Montana";
    public const string Nebraska = "Nebraska";
    public const string Nevada = "Nevada";
    public const string NewHampshire = "NewHampshire";
    public const string NewJersey = "NewJersey";
    public const string NewMexico = "NewMexico";
    public const string NewYork = "NewYork";
    public const string NorthCarolina = "NorthCarolina";
    public const string NorthDakota = "NorthDakota";
    public const string Ohio = "Ohio";
    public const string Oklahoma = "Oklahoma";
    public const string Oregon = "Oregon";
    public const string Pennsylvania = "Pennsylvania";
    public const string RhodeIsland = "RhodeIsland";
    public const string SouthCarolina = "SouthCarolina";
    public const string SouthDakota = "SouthDakota";
    public const string Tennessee = "Tennessee";
    public const string Texas = "Texas";
    public const string Utah = "Utah";
    public const string Vermont = "Vermont";
    public const string Virginia = "Virginia";
    public const string Washington = "Washington";
    public const string WestVirginia = "WestVirginia";
    public const string Wisconsin = "Wisconsin";
    public const string Wyoming = "Wyoming";

    // Canada
    public const string Alberta = "Alberta";
    public const string BritishColumbia = "BritishColumbia";
    public const string Manitoba = "Manitoba";
    public const string NewBrunswick = "NewBrunswick";
    public const string NovaScotia = "NovaScotia";
    public const string Ontario = "Ontario";
    public const string PrinceEdwardIsland = "PrinceEdwardIsland";
    public const string Quebec = "Quebec";
    public const string Saskatchewan = "Saskatchewan";

    // Pakistan
    public const string AzadKashmirAndGilgitBaltistan = "AzadKashmirAndGilgitBaltistan";
    public const string Balochistan = "Balochistan";
    public const string FederallyAdministeredTribalAreas = "FederallyAdministeredTribalAreas";
    public const string IslamabadCapitalTerritory = "IslamabadCapitalTerritory";
    public const string KhyberPakhtunkhwa = "KhyberPakhtunkhwa";
    public const string PunjabPk = "PunjabPk";
    public const string Sindh = "Sindh";

    // United Kingdom
    public const string England = "England";
    public const string Scotland = "Scotland";
    public const string Wales = "Wales";
    public const string NorthernIreland = "NorthernIreland";

    // Australia
    public const string AustralianCapitalTerritory = "AustralianCapitalTerritory";
    public const string NewSouthWales = "NewSouthWales";
    public const string NorthernTerritory = "NorthernTerritory";
    public const string Queensland = "Queensland";
    public const string SouthAustralia = "SouthAustralia";
    public const string Tasmania = "Tasmania";
    public const string Victoria = "Victoria";
    public const string WesternAustralia = "WesternAustralia";

    // India
    public const string AndamanAndNicobarIslands = "AndamanAndNicobarIslands";
    public const string AndhraPradesh = "AndhraPradesh";
    public const string ArunachalPradesh = "ArunachalPradesh";
    public const string Assam = "Assam";
    public const string Bihar = "Bihar";
    public const string Chhattisgarh = "Chhattisgarh";
    public const string Dadra = "Dadra";
    public const string Daman = "Daman";
    public const string Diu = "Diu";
    public const string Delhi = "Delhi";
    public const string Goa = "Goa";
    public const string Gujarat = "Gujarat";
    public const string Haryana = "Haryana";
    public const string HimachalPradesh = "HimachalPradesh";
    public const string JammuAndKashmir = "JammuAndKashmir";
    public const string Jharkhand = "Jharkhand";
    public const string Karnataka = "Karnataka";
    public const string Kerala = "Kerala";
    public const string Lakshadweep = "Lakshadweep";
    public const string MadhyaPradesh = "MadhyaPradesh";
    public const string Maharashtra = "Maharashtra";
    public const string Manipur = "Manipur";
    public const string Meghalaya = "Meghalaya";
    public const string Mizoram = "Mizoram";
    public const string Nagaland = "Nagaland";
    public const string Odisha = "Odisha";
    public const string Puducherry = "Puducherry";
    public const string Punjab = "Punjab";
    public const string Rajasthan = "Rajasthan";
    public const string Sikkim = "Sikkim";
    public const string TamilNadu = "TamilNadu";
    public const string Telangana = "Telangana";
    public const string Tripura = "Tripura";
    public const string UttarPradesh = "UttarPradesh";
    public const string Uttarakhand = "Uttarakhand";
    public const string WestBengal = "WestBengal";

    public static readonly string[] All =
    [
        // United States
        Alabama, Alaska, Arizona, Arkansas, California, Colorado, Connecticut, Delaware, Florida,
        Georgia, Hawaii, Idaho, Illinois, Indiana, Iowa, Kansas, Kentucky, Louisiana, Maine, Maryland,
        Massachusetts, Michigan, Minnesota, Mississippi, Missouri, Montana, Nebraska, Nevada,
        NewHampshire, NewJersey, NewMexico, NewYork, NorthCarolina, NorthDakota, Ohio, Oklahoma,
        Oregon, Pennsylvania, RhodeIsland, SouthCarolina, SouthDakota, Tennessee, Texas, Utah,
        Vermont, Virginia, Washington, WestVirginia, Wisconsin, Wyoming,
        // Canada
        Alberta, BritishColumbia, Manitoba, NewBrunswick, NovaScotia, Ontario,
        PrinceEdwardIsland, Quebec, Saskatchewan,
        // Pakistan
        AzadKashmirAndGilgitBaltistan, Balochistan, FederallyAdministeredTribalAreas,
        IslamabadCapitalTerritory, KhyberPakhtunkhwa, PunjabPk, Sindh,
        // United Kingdom
        England, Scotland, Wales, NorthernIreland,
        // Australia
        AustralianCapitalTerritory, NewSouthWales, NorthernTerritory, Queensland,
        SouthAustralia, Tasmania, Victoria, WesternAustralia,
        // India
        AndamanAndNicobarIslands, AndhraPradesh, ArunachalPradesh, Assam, Bihar, Chhattisgarh,
        Dadra, Daman, Diu, Delhi, Goa, Gujarat, Haryana, HimachalPradesh, JammuAndKashmir,
        Jharkhand, Karnataka, Kerala, Lakshadweep, MadhyaPradesh, Maharashtra, Manipur, Meghalaya,
        Mizoram, Nagaland, Odisha, Puducherry, Punjab, Rajasthan, Sikkim, TamilNadu, Telangana,
        Tripura, UttarPradesh, Uttarakhand, WestBengal
    ];
}
