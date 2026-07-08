using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.DTOs.ValidateDtos;

public class CreateCompanyAndUserDto
{
    public CompanyDto Company { get; set; } = new CompanyDto();
    public CreateUserDto User { get; set; } = new CreateUserDto();

    /// <summary>
    /// When true, modules also seed sample/demo data (orders, invoices, contacts, items…).
    /// When false (default) only master/config data is seeded (chart of accounts, templates,
    /// price lists, tax, POS store/terminals/cashiers…).
    /// </summary>
    public bool IncludeSampleData { get; set; }
}
